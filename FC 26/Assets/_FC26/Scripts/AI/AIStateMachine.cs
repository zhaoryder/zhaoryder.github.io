//=============================================================================
// 文件名：AIStateMachine.cs
// 所属模块：AI
// 命名空间：FC26.AI
// 作用：AI 行为状态机。每个 AI 球员持有一个 AIStateMachine 实例，负责：
//   1. 根据球权与球员位置切换状态（Attack/Defend/Support/Mark/Goalkeep）；
//   2. 按反应时间节流调用 AIDecisionCore.MakeDecision；
//   3. 在 Update 中执行状态对应行为（移动 / 触球），通过 PlayerController 驱动球员。
// 状态切换原则：
//   - 门将恒为 Goalkeep；
//   - 本队控球时，持球者与前锋/前压中场为 Attack，其余为 Support；
//   - 对方控球时，距球最近者为 Mark（压迫），其余为 Defend；
//   - 无人控球时，距球最近者为 Attack（抢球），其余为 Support。
// 备注：本类为普通 C# 类（非 MonoBehaviour），由 AIController 每帧调用 Update。
//       反应时间通过难度参数 ReactionTime 控制，避免每帧重算决策造成抖动。
//=============================================================================
using UnityEngine;
using FC26.Core;
using FC26.Data;
using FC26.Ball;
using FC26.Player;

namespace FC26.AI
{
    /// <summary>
    /// AI 行为状态枚举。
    /// </summary>
    public enum AIState
    {
        /// <summary>进攻：持球突破/射门，或无球前压跑位</summary>
        Attack,
        /// <summary>防守：回到防线位置</summary>
        Defend,
        /// <summary>支援：为持球者提供接应点</summary>
        Support,
        /// <summary>盯人：逼近对方持球者施加压迫</summary>
        Mark,
        /// <summary>守门：门将专属，沿球门线跟随球</summary>
        Goalkeep
    }

    /// <summary>
    /// AI 行为状态机。每球员一个实例，管理状态切换与决策执行。
    /// </summary>
    public class AIStateMachine
    {
        // 关联的球员实体与控制器
        private readonly PlayerEntity _player;
        private readonly PlayerController _controller;

        // 当前状态
        private AIState _currentState = AIState.Support;

        // 决策节流计时器（剩余秒数）
        private float _decisionTimer;

        // 最近一次决策结果（在反应时间间隔内持续执行）
        private AIDecision _lastDecision;

        // 标记是否已执行过持球动作（Pass/Shoot/ThroughBall 只执行一次，执行后球已离开）
        private bool _ballActionExecuted;

        /// <summary>当前状态。</summary>
        public AIState CurrentState => _currentState;

        /// <summary>关联球员。</summary>
        public PlayerEntity Player => _player;

        /// <summary>
        /// 构造状态机。
        /// </summary>
        /// <param name="player">球员实体</param>
        /// <param name="controller">球员控制器</param>
        public AIStateMachine(PlayerEntity player, PlayerController controller)
        {
            _player = player;
            _controller = controller;
            // 门将初始为守门状态
            if (player != null && player.IsGoalkeeper)
            {
                _currentState = AIState.Goalkeep;
            }
        }

        /// <summary>
        /// 每帧更新：切换状态 → 节流决策 → 执行行为。
        /// 由 AIController 在 Update 中调用。
        /// </summary>
        /// <param name="ball">足球实体</param>
        /// <param name="context">比赛上下文</param>
        public void Update(BallEntity ball, MatchContext context)
        {
            if (_player == null || _controller == null || context == null)
            {
                return;
            }

            // 1. 切换状态
            UpdateState(context);

            // 2. 决策节流：到达反应时间才重新决策
            AIDifficultyParams diff = context.Difficulty;
            _decisionTimer -= Time.deltaTime;
            bool needNewDecision = _decisionTimer <= 0f;

            // 持球动作已执行且仍处于同一持球回合时，若球已离开则重置标记
            if (_ballActionExecuted && !IsBallOwner(context))
            {
                _ballActionExecuted = false;
            }

            if (needNewDecision)
            {
                // 写入当前状态供决策核心分支
                context.RequestingPlayerState = _currentState;

                AIDecisionCore core = AIDecisionCore.Instance;
                if (core != null)
                {
                    _lastDecision = core.MakeDecision(_player, ball, context);
                }
                _decisionTimer = diff.ReactionTime;
                _ballActionExecuted = false;
            }

            // 3. 执行决策
            ExecuteDecision(diff);
        }

        /// <summary>当前球员是否为控球者。</summary>
        private bool IsBallOwner(MatchContext context)
        {
            return context.PossessionPlayerId == _player.PlayerId
                   && context.PossessionTeamId == _player.TeamId;
        }

        // ====================================================================
        #region 状态切换

        /// <summary>
        /// 根据球权与球员位置切换状态。
        /// </summary>
        private void UpdateState(MatchContext context)
        {
            // 门将恒为守门
            if (_player.IsGoalkeeper)
            {
                _currentState = AIState.Goalkeep;
                return;
            }

            int possTeam = context.PossessionTeamId;
            bool ourTeamHasBall = possTeam == _player.TeamId;
            bool opponentHasBall = possTeam >= 0 && possTeam != _player.TeamId;

            if (ourTeamHasBall)
            {
                // 持球者本人 → Attack
                if (context.PossessionPlayerId == _player.PlayerId)
                {
                    _currentState = AIState.Attack;
                    return;
                }

                // 前锋与前压中场 → Attack，其余 → Support
                TacticParams tactic = context.Tactic;
                bool isAttacker = _player.PositionType == PlayerPosition.FW;
                if (_player.PositionType == PlayerPosition.MF && tactic.AttackTendency > 0.6f)
                {
                    isAttacker = true;
                }
                _currentState = isAttacker ? AIState.Attack : AIState.Support;
            }
            else if (opponentHasBall)
            {
                // 距球权持有者最近的本队球员 → Mark（压迫），其余 → Defend
                PlayerEntity carrier = FindOpponentCarrier(context);
                Vector3 refPos = carrier != null ? carrier.Position : context.BallPosition;

                bool isNearest = IsNearestTeammateTo(refPos, context);
                TacticParams tactic = context.Tactic;

                if (isNearest && tactic.PressIntensity > 0.45f)
                {
                    _currentState = AIState.Mark;
                }
                else
                {
                    _currentState = AIState.Defend;
                }
            }
            else
            {
                // 无人控球（loose ball）：距球最近者 → Attack（抢球），其余 → Support
                bool isNearest = IsNearestTeammateTo(context.BallPosition, context);
                _currentState = isNearest ? AIState.Attack : AIState.Support;
            }
        }

        /// <summary>查找对方控球球员。</summary>
        private PlayerEntity FindOpponentCarrier(MatchContext context)
        {
            PlayerEntity[] opponents = _player.TeamId == 0 ? context.AwayPlayers : context.HomePlayers;
            if (opponents == null) return null;
            int targetId = context.PossessionPlayerId;
            for (int i = 0; i < opponents.Length; i++)
            {
                if (opponents[i] != null && opponents[i].PlayerId == targetId)
                {
                    return opponents[i];
                }
            }
            return null;
        }

        /// <summary>判断本球员是否为距指定位置最近的同队球员（含小容差）。</summary>
        private bool IsNearestTeammateTo(Vector3 pos, MatchContext context)
        {
            PlayerEntity[] teammates = _player.TeamId == 0 ? context.HomePlayers : context.AwayPlayers;
            if (teammates == null) return true;

            float myDist = HorizontalDist(_player.Position, pos);
            float minDist = float.MaxValue;

            for (int i = 0; i < teammates.Length; i++)
            {
                PlayerEntity t = teammates[i];
                if (t == null || t == _player || t.IsGoalkeeper) continue;
                float d = HorizontalDist(t.Position, pos);
                if (d < minDist) minDist = d;
            }
            // 容差 0.8 米：距离接近时也视为最近，避免频繁切换
            return myDist <= minDist + 0.8f;
        }

        /// <summary>水平面距离。</summary>
        private float HorizontalDist(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        #endregion

        // ====================================================================
        #region 决策执行

        /// <summary>
        /// 执行最近一次决策。持球动作（Pass/Shoot/ThroughBall）只执行一次，
        /// 移动类动作（Move/Dribble）每帧持续执行直到下次决策。
        /// 持球动作执行时通过 EventBus 发布 AIActionEvent，供 UI/音频/回放订阅。
        /// 注意：PlayerController.Pass/Shoot/ThroughBall 接收的是“方向”（世界空间），
        ///   Move/Dribble 接收的是“相对摄像机的二维输入”（Vector2），
        ///   故此处需将决策结果中的 Direction/TargetPosition 转换为对应格式。
        /// </summary>
        private void ExecuteDecision(AIDifficultyParams diff)
        {
            switch (_lastDecision.ActionType)
            {
                case AIActionType.Pass:
                    if (!_ballActionExecuted)
                    {
                        float power = _lastDecision.Power * diff.SkillMultiplier;
                        // Pass 接收世界方向（已归一化），使用 Direction 而非 TargetPosition
                        _controller.Pass(_lastDecision.Direction, power);
                        PublishAction(AIActionType.Pass, power);
                        _ballActionExecuted = true;
                    }
                    break;

                case AIActionType.Shoot:
                    if (!_ballActionExecuted)
                    {
                        float power = _lastDecision.Power * diff.SkillMultiplier;
                        // Shoot 接收世界方向
                        _controller.Shoot(_lastDecision.Direction, power);
                        PublishAction(AIActionType.Shoot, power);
                        _ballActionExecuted = true;
                    }
                    break;

                case AIActionType.ThroughBall:
                    if (!_ballActionExecuted)
                    {
                        float power = _lastDecision.Power * diff.SkillMultiplier;
                        // ThroughBall 接收世界方向
                        _controller.ThroughBall(_lastDecision.Direction, power);
                        PublishAction(AIActionType.ThroughBall, power);
                        _ballActionExecuted = true;
                    }
                    break;

                case AIActionType.Dribble:
                    // 带球：将世界方向转为相对摄像机的二维输入，调用 Dribble
                    {
                        Vector2 input = WorldDirectionToCameraInput(_lastDecision.Direction);
                        _controller.Dribble(input);
                    }
                    break;

                case AIActionType.Move:
                default:
                    // 无球跑位：计算朝目标的世界方向，转为相对摄像机的二维输入，调用 Move
                    {
                        Vector3 toTarget = _lastDecision.TargetPosition - _player.Position;
                        Vector2 input = WorldDirectionToCameraInput(toTarget);
                        _controller.Move(input);
                    }
                    break;
            }
        }

        /// <summary>
        /// 将世界空间水平方向转换为相对摄像机的二维输入（Vector2）。
        /// PlayerController.Move/Dribble 接收相对摄像机的二维输入（x=左右, y=前后），
        /// CameraUtility.GetMoveDirectionRelativeToCamera 的逆变换为：
        ///   input.y = dot(worldDir, camForward)
        ///   input.x = dot(worldDir, camRight)
        /// 若主摄像机不可用，回退到世界方向直接映射（z→y, x→x）。
        /// </summary>
        /// <param name="worldDir">世界空间方向（水平面，y 分量忽略）</param>
        /// <returns>相对摄像机的二维输入（幅度不超过 1）</returns>
        private Vector2 WorldDirectionToCameraInput(Vector3 worldDir)
        {
            // 清除 y 分量，仅保留水平方向
            worldDir.y = 0f;
            if (worldDir.sqrMagnitude < 1e-6f)
            {
                return Vector2.zero;
            }
            worldDir.Normalize();

            UnityEngine.Camera cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                // 摄像机不可用时回退：世界 z→输入 y，世界 x→输入 x
                return new Vector2(worldDir.x, worldDir.z);
            }

            // 取摄像机前向与右向，投影到水平面并归一化
            Vector3 camForward = cam.transform.forward;
            Vector3 camRight = cam.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;

            // 摄像机近乎垂直俯视时回退到 +z / +x
            if (camForward.sqrMagnitude < 1e-6f) camForward = Vector3.forward;
            if (camRight.sqrMagnitude < 1e-6f) camRight = Vector3.right;
            camForward.Normalize();
            camRight.Normalize();

            // 逆变换：求 worldDir 在 camForward 与 camRight 上的投影分量
            float inputY = Vector3.Dot(worldDir, camForward);
            float inputX = Vector3.Dot(worldDir, camRight);
            Vector2 input = new Vector2(inputX, inputY);

            // 限制幅度不超过 1（对角线移动时归一化）
            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }
            return input;
        }

        /// <summary>
        /// 通过 EventBus 发布 AIActionEvent。
        /// </summary>
        /// <param name="actionType">动作类型</param>
        /// <param name="power">实际执行力量（已乘技能倍率）</param>
        private void PublishAction(AIActionType actionType, float power)
        {
            EventBus.Publish(new AIActionEvent
            {
                PlayerId = _player.PlayerId,
                ActionType = (int)actionType,
                Target = _lastDecision.TargetPosition,
                Power = power
            });
        }

        #endregion
    }
}
