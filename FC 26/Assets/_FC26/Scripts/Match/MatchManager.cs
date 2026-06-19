//=============================================================================
// 文件名：MatchManager.cs
// 所属模块：Match
// 命名空间：FC26.Match
// 作用：比赛管理器单例。继承 MonoSingleton<MatchManager>，全局唯一。
//       总调度比赛流程：上下半场、伤停补时、开球、暂停、结束。
//       持有 MatchTimer、MatchStatistics、KickOffController 引用。
//       监听 GoalScoredEvent 更新比分；半场切换时主客队交换场地。
// 备注：本脚本需挂载在场景中的 GameObject 上。
//       依赖 BallManager、PlayerFactory、MatchTimer、KickOffController、DisciplineSystem。
//=============================================================================
using UnityEngine;
using FC26.Core;
using FC26.Ball;
using FC26.Player;
using FC26.Data;
using FC26.Referee;

namespace FC26.Match
{
    /// <summary>
    /// 比赛阶段枚举。
    /// </summary>
    public enum MatchPhase
    {
        /// <summary>未开始</summary>
        NotStarted,
        /// <summary>上半场</summary>
        FirstHalf,
        /// <summary>中场休息</summary>
        HalfTime,
        /// <summary>下半场</summary>
        SecondHalf,
        /// <summary>全场结束</summary>
        FullTime
    }

    /// <summary>
    /// 比赛管理器单例：调度比赛全流程。
    /// </summary>
    public class MatchManager : MonoSingleton<MatchManager>
    {
        // ===== 比赛配置 =====
        [Header("比赛配置")]
        [Tooltip("中场休息时长（现实秒）")]
        [SerializeField] private float _halfTimeDuration = 15f;

        // ===== 队伍数据 =====
        // 主队数据（TeamId=0）
        private TeamData _homeTeam;

        // 客队数据（TeamId=1）
        private TeamData _awayTeam;

        // ===== 比分 =====
        private int _homeScore = 0;
        private int _awayScore = 0;

        // ===== 比赛阶段 =====
        private MatchPhase _phase = MatchPhase.NotStarted;

        // 中场休息倒计时
        private float _halfTimeCountdown = 0f;

        // 上半场先开球的队伍（下半场由对方开球）
        private int _firstKickOffTeam = 0;

        // ===== 子系统引用 =====
        // 比赛统计（普通类实例，由本类创建）
        private MatchStatistics _statistics;

        /// <summary>
        /// Awake：注册单例（基类完成），创建统计实例，订阅事件。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            _statistics = new MatchStatistics();
        }

        /// <summary>
        /// OnEnable：订阅进球事件与半场结束回调。
        /// </summary>
        private void OnEnable()
        {
            EventBus.Subscribe<GoalScoredEvent>(OnGoalScored);
        }

        /// <summary>
        /// OnDisable：取消订阅。
        /// </summary>
        private void OnDisable()
        {
            EventBus.Unsubscribe<GoalScoredEvent>(OnGoalScored);

            // 取消半场结束回调
            if (MatchTimer.Instance != null)
            {
                MatchTimer.Instance.OnHalfEnded -= OnHalfEnded;
            }
        }

        // ===== 公开属性 =====

        /// <summary>主队数据。</summary>
        public TeamData HomeTeam => _homeTeam;

        /// <summary>客队数据。</summary>
        public TeamData AwayTeam => _awayTeam;

        /// <summary>主队比分。</summary>
        public int HomeScore => _homeScore;

        /// <summary>客队比分。</summary>
        public int AwayScore => _awayScore;

        /// <summary>当前比赛阶段。</summary>
        public MatchPhase Phase => _phase;

        /// <summary>比赛统计实例。</summary>
        public MatchStatistics Statistics => _statistics;

        /// <summary>是否正在比赛中（上半场或下半场进行中）。</summary>
        public bool IsInProgress => _phase == MatchPhase.FirstHalf || _phase == MatchPhase.SecondHalf;

        // ===== 比赛流程控制 =====

        /// <summary>
        /// 开始比赛。
        /// 创建两队球员、重置统计与计时器、执行开球。
        /// 主队（TeamId=0）上半场进攻 +z，客队（TeamId=1）进攻 -z。
        /// </summary>
        /// <param name="home">主队数据</param>
        /// <param name="away">客队数据</param>
        public void StartMatch(TeamData home, TeamData away)
        {
            if (home == null || away == null)
            {
                Debug.LogError("[MatchManager] 开始比赛失败：队伍数据为空。");
                return;
            }

            // 存储队伍数据
            _homeTeam = home;
            _awayTeam = away;

            // 重置比分与统计
            _homeScore = 0;
            _awayScore = 0;
            _statistics.Reset();

            // 重置纪律系统
            DisciplineSystem.Instance?.Reset();

            // 创建两队球员
            // 主队（TeamId=0）上半场进攻 +z
            // 客队（TeamId=1）上半场进攻 -z
            if (PlayerFactory.Instance != null)
            {
                PlayerFactory.Instance.ClearAllTeams();
                PlayerFactory.Instance.CreateTeamOnField(_homeTeam, 0, true);   // 主队进攻 +z
                PlayerFactory.Instance.CreateTeamOnField(_awayTeam, 1, false);  // 客队进攻 -z

                // 注册球员到纪律系统（使用 teamId * 100 + index 作为球员 ID）
                RegisterPlayersWithDiscipline(0);
                RegisterPlayersWithDiscipline(1);
            }
            else
            {
                Debug.LogWarning("[MatchManager] PlayerFactory 未找到，跳过球员创建。");
            }

            // 订阅半场结束回调
            if (MatchTimer.Instance != null)
            {
                MatchTimer.Instance.OnHalfEnded -= OnHalfEnded;
                MatchTimer.Instance.OnHalfEnded += OnHalfEnded;

                // 重置并启动计时器
                MatchTimer.Instance.Reset();
                MatchTimer.Instance.Start();
            }
            else
            {
                Debug.LogWarning("[MatchManager] MatchTimer 未找到，跳过计时。");
            }

            // 设置阶段为上半场
            _phase = MatchPhase.FirstHalf;

            // 主队先开球
            _firstKickOffTeam = 0;
            KickOffController.Instance?.PerformKickOff(_firstKickOffTeam);

            // 广播比赛开始事件
            EventBus.Publish(new MatchStartedEvent());

            Debug.Log($"[MatchManager] 比赛开始：{_homeTeam.TeamName} vs {_awayTeam.TeamName}");
        }

        /// <summary>
        /// 暂停比赛。
        /// 暂停计时器并广播暂停事件。
        /// </summary>
        public void PauseMatch()
        {
            if (!IsInProgress)
            {
                return;
            }

            MatchTimer.Instance?.Pause();
            EventBus.Publish(new PauseToggledEvent { Paused = true });
            Debug.Log("[MatchManager] 比赛暂停");
        }

        /// <summary>
        /// 恢复比赛。
        /// 恢复计时器并广播恢复事件。
        /// </summary>
        public void ResumeMatch()
        {
            if (!IsInProgress)
            {
                return;
            }

            MatchTimer.Instance?.Resume();
            EventBus.Publish(new PauseToggledEvent { Paused = false });
            Debug.Log("[MatchManager] 比赛恢复");
        }

        /// <summary>
        /// 结束比赛。
        /// 停止计时器，设置阶段为全场结束，广播比赛结束事件。
        /// </summary>
        public void EndMatch()
        {
            MatchTimer.Instance?.Pause();
            _phase = MatchPhase.FullTime;

            // 取消半场结束回调
            if (MatchTimer.Instance != null)
            {
                MatchTimer.Instance.OnHalfEnded -= OnHalfEnded;
            }

            EventBus.Publish(new MatchEndedEvent());

            var stats = _statistics.GetStats();
            Debug.Log($"[MatchManager] 比赛结束：{_homeTeam?.TeamName} {_homeScore} - {_awayScore} {_awayTeam?.TeamName}");
            Debug.Log($"[MatchManager] 统计 - 射门 {stats.Home.Shots}-{stats.Away.Shots}, " +
                      $"犯规 {stats.Home.Fouls}-{stats.Away.Fouls}, " +
                      $"黄牌 {stats.Home.YellowCards}-{stats.Away.YellowCards}, " +
                      $"红牌 {stats.Home.RedCards}-{stats.Away.RedCards}");
        }

        // ===== Update：驱动统计与阶段转换 =====

        /// <summary>
        /// Update：更新控球统计，处理中场休息倒计时。
        /// </summary>
        private void Update()
        {
            if (_phase == MatchPhase.NotStarted || _phase == MatchPhase.FullTime)
            {
                return;
            }

            // 更新控球统计
            if (IsInProgress && BallManager.Instance != null)
            {
                int possessionTeam = BallManager.Instance.PossessionTeamId;
                _statistics.UpdatePossession(possessionTeam, Time.deltaTime);
            }

            // 中场休息倒计时
            if (_phase == MatchPhase.HalfTime)
            {
                _halfTimeCountdown -= Time.deltaTime;
                if (_halfTimeCountdown <= 0f)
                {
                    StartSecondHalf();
                }
            }
        }

        // ===== 事件回调 =====

        /// <summary>
        /// 进球事件回调：更新比分、记录统计、记录伤停补时。
        /// </summary>
        /// <param name="evt">进球事件</param>
        private void OnGoalScored(GoalScoredEvent evt)
        {
            // 更新比分
            if (evt.TeamId == 0)
            {
                _homeScore++;
            }
            else
            {
                _awayScore++;
            }

            // 记录射门统计（进球必然射正）
            _statistics.RecordShot(evt.TeamId, true);

            // 记录进球用于伤停补时计算
            MatchTimer.Instance?.RecordGoalForStoppage();

            Debug.Log($"[MatchManager] 进球！队伍={evt.TeamId}，比分 {_homeScore}-{_awayScore}，" +
                      $"比赛时间={evt.MatchTime}");
        }

        /// <summary>
        /// 半场结束回调：由 MatchTimer 在补时结束时触发。
        /// </summary>
        /// <param name="half">结束的半场号（1=上半场，2=下半场）</param>
        private void OnHalfEnded(int half)
        {
            if (half == 1)
            {
                // 上半场结束 → 进入中场休息
                EnterHalfTime();
            }
            else if (half == 2)
            {
                // 下半场结束 → 全场结束
                EndMatch();
            }
        }

        // ===== 内部流程 =====

        /// <summary>
        /// 进入中场休息。
        /// </summary>
        private void EnterHalfTime()
        {
            _phase = MatchPhase.HalfTime;
            _halfTimeCountdown = _halfTimeDuration;

            Debug.Log("[MatchManager] 进入中场休息");
        }

        /// <summary>
        /// 开始下半场。
        /// 交换场地（主队进攻 -z，客队进攻 +z），重置计时器，执行开球。
        /// </summary>
        private void StartSecondHalf()
        {
            // 交换场地
            if (PlayerFactory.Instance != null)
            {
                // 主队改为进攻 -z
                PlayerFactory.Instance.SetTeamAttackDirection(0, false);
                // 客队改为进攻 +z
                PlayerFactory.Instance.SetTeamAttackDirection(1, true);
            }

            // 计时器进入下半场
            MatchTimer.Instance?.NextHalf();

            // 设置阶段为下半场
            _phase = MatchPhase.SecondHalf;

            // 下半场由上半场未开球的队伍开球
            int secondKickOffTeam = (_firstKickOffTeam == 0) ? 1 : 0;
            KickOffController.Instance?.PerformKickOff(secondKickOffTeam);

            Debug.Log("[MatchManager] 下半场开始");
        }

        /// <summary>
        /// 注册指定队伍的球员到纪律系统。
        /// 球员 ID 约定：teamId * 100 + playerIndex。
        /// </summary>
        /// <param name="teamId">队伍 ID</param>
        private void RegisterPlayersWithDiscipline(int teamId)
        {
            if (DisciplineSystem.Instance == null || PlayerFactory.Instance == null)
            {
                return;
            }

            Transform[] players = PlayerFactory.Instance.GetTeamPlayers(teamId);
            for (int i = 0; i < players.Length; i++)
            {
                int playerId = teamId * 100 + i;
                DisciplineSystem.Instance.RegisterPlayer(playerId, teamId);
            }
        }
    }
}
