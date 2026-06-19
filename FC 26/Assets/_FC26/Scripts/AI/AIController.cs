//=============================================================================
// 文件名：AIController.cs
// 所属模块：AI
// 命名空间：FC26.AI
// 作用：AI 球队总控。继承 MonoSingleton<AIController>，全局唯一。
//       控制 AI 球队（客队，TeamId=1）所有球员：
//   1. 收集 AI 球员并为每人创建 AIStateMachine；
//   2. 每帧更新 MatchContext（球权、球位置、比分、双方球员坐标）；
//   3. 每帧为每个 AI 球员驱动状态机（状态切换 → 决策 → 执行）；
//   4. 执行决策时通过 EventBus 发布 AIActionEvent，供 UI/音频/回放订阅。
//   5. 持有难度（AILevel）与战术风格（TacticStyle），可在 Inspector 配置。
// 备注：本脚本需挂载在场景中的 AIController GameObject 上。
//       依赖 BallManager（球权与球位置）、AIDecisionCore（决策）。
//       比赛仅在 GameManager.IsPlaying 时驱动，暂停/菜单状态停止 AI。
//=============================================================================
using System.Collections.Generic;
using UnityEngine;
using FC26.Core;
using FC26.Data;
using FC26.Ball;
using FC26.Player;

namespace FC26.AI
{
    /// <summary>
    /// AI 球队总控单例。管理 AI 球队所有球员的决策与执行。
    /// </summary>
    public class AIController : MonoSingleton<AIController>
    {
        // ===== 配置 =====
        [Header("AI 配置")]
        [Tooltip("AI 难度等级")]
        [SerializeField] private AILevel _difficulty = AILevel.Normal;

        [Tooltip("AI 球队战术风格")]
        [SerializeField] private TacticStyle _tacticStyle = TacticStyle.Possession;

        [Tooltip("AI 球队队伍 ID（默认客队=1）")]
        [SerializeField] private int _aiTeamId = 1;

        [Header("调试")]
        [Tooltip("是否打印初始化与状态日志")]
        [SerializeField] private bool _logVerbose = false;

        // ===== 运行时数据 =====
        // AI 球员列表
        private readonly List<PlayerEntity> _aiPlayers = new List<PlayerEntity>();

        // 主队球员列表（用于上下文）
        private readonly List<PlayerEntity> _homePlayers = new List<PlayerEntity>();

        // 每个球员对应的状态机
        private readonly Dictionary<PlayerEntity, AIStateMachine> _stateMachines = new Dictionary<PlayerEntity, AIStateMachine>();

        // 比赛上下文（每帧复用，避免 GC）
        private readonly MatchContext _context = new MatchContext();

        // 缓存数组（避免每帧 ToArray 分配）
        private PlayerEntity[] _homeArray = System.Array.Empty<PlayerEntity>();
        private PlayerEntity[] _awayArray = System.Array.Empty<PlayerEntity>();

        // 缓存管理器引用
        private BallManager _ballManager;
        private GameManager _gameManager;

        /// <summary>当前 AI 难度等级。</summary>
        public AILevel Difficulty => _difficulty;

        /// <summary>当前战术风格。</summary>
        public TacticStyle TacticStyle => _tacticStyle;

        /// <summary>AI 球队队伍 ID。</summary>
        public int AITeamId => _aiTeamId;

        /// <summary>当前战术参数（依据战术风格计算）。</summary>
        public TacticParams CurrentTactic => Tactics.GetParams(_tacticStyle);

        /// <summary>当前难度参数。</summary>
        public AIDifficultyParams CurrentDifficulty => AIDifficulty.GetParams(_difficulty);

        /// <summary>Unity Awake：注册单例并确保决策核心存在。</summary>
        protected override void Awake()
        {
            base.Awake();

            // 确保 AIDecisionCore 单例存在（若场景未挂载则同物体上添加）
            if (AIDecisionCore.Instance == null)
            {
                gameObject.AddComponent<AIDecisionCore>();
            }
        }

        /// <summary>Unity Start：缓存管理器引用并收集球员。</summary>
        private void Start()
        {
            _ballManager = BallManager.Instance;
            _gameManager = GameManager.Instance;

            CollectPlayers();

            if (_logVerbose)
            {
                Debug.Log($"[AIController] 初始化完成：AI球员 {_aiPlayers.Count} 人，主队 {_homePlayers.Count} 人，难度={_difficulty}，战术={_tacticStyle}");
            }
        }

        /// <summary>
        /// 收集场景中的球员并按队伍分类，为每个 AI 球员创建状态机。
        /// 若球员在 Start 之后生成（如换人），可再次调用本方法刷新。
        /// </summary>
        public void CollectPlayers()
        {
            _aiPlayers.Clear();
            _homePlayers.Clear();
            _stateMachines.Clear();

            PlayerEntity[] all = FindObjectsOfType<PlayerEntity>();
            foreach (PlayerEntity pe in all)
            {
                if (pe == null) continue;

                if (pe.TeamId == _aiTeamId)
                {
                    _aiPlayers.Add(pe);
                    PlayerController ctrl = pe.GetComponent<PlayerController>();
                    if (ctrl == null)
                    {
                        ctrl = pe.gameObject.AddComponent<PlayerController>();
                    }
                    _stateMachines[pe] = new AIStateMachine(pe, ctrl);
                }
                else
                {
                    _homePlayers.Add(pe);
                }
            }

            // 刷新缓存数组
            _homeArray = _homePlayers.ToArray();
            _awayArray = _aiPlayers.ToArray();
        }

        /// <summary>
        /// 设置 AI 难度（运行时可调，如难度选择界面）。
        /// </summary>
        public void SetDifficulty(AILevel level)
        {
            _difficulty = level;
        }

        /// <summary>
        /// 设置 AI 战术风格（运行时可调，如战术调整界面）。
        /// </summary>
        public void SetTactic(TacticStyle style)
        {
            _tacticStyle = style;
        }

        /// <summary>Unity Update：驱动 AI 球队。仅在比赛进行时执行。</summary>
        private void Update()
        {
            // 比赛未进行（暂停/菜单）时停止 AI
            if (_gameManager != null && !_gameManager.IsPlaying)
            {
                return;
            }

            if (_aiPlayers.Count == 0)
            {
                return;
            }

            BallEntity ball = _ballManager != null ? _ballManager.GetBall() : null;

            // 1. 更新比赛上下文
            UpdateContext(ball);

            // 2. 为每个 AI 球员驱动状态机
            for (int i = 0; i < _aiPlayers.Count; i++)
            {
                PlayerEntity player = _aiPlayers[i];
                if (player == null) continue;

                if (_stateMachines.TryGetValue(player, out var sm))
                {
                    sm.Update(ball, _context);
                }
            }
        }

        /// <summary>
        /// 更新比赛上下文：球权、球位置、比分、战术、难度、双方球员坐标。
        /// </summary>
        private void UpdateContext(BallEntity ball)
        {
            // 球位置与球权
            if (ball != null)
            {
                _context.BallPosition = ball.transform.position;
            }
            if (_ballManager != null)
            {
                _context.PossessionTeamId = _ballManager.PossessionTeamId;
                _context.PossessionPlayerId = _ballManager.PossessionPlayerId;
            }

            // 比赛时间
            _context.MatchTime = Time.time;

            // 战术与难度
            _context.Tactic = CurrentTactic;
            _context.Difficulty = CurrentDifficulty;

            // 球员数组（缓存，避免每帧分配）
            _context.HomePlayers = _homeArray;
            _context.AwayPlayers = _awayArray;
        }
    }
}
