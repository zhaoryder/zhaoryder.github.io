//=============================================================================
// 文件名：RefereeManager.cs
// 所属模块：Referee
// 命名空间：FC26.Referee
// 作用：裁判管理器单例。继承 MonoSingleton<RefereeManager>，全局唯一。
//       总调度越位/犯规/出界判定，协调 OffsideChecker、FoulChecker、
//       DisciplineSystem、SetPieceController 四个子系统。
//       监听 BallOutOfPlayEvent 处理出界后的定位球流程。
//       提供公开方法供 Player/AI 模块调用以触发犯规与越位判定。
// 备注：本脚本需挂载在场景中的 GameObject 上。
//       依赖 BallManager、PlayerFactory、MatchManager、DisciplineSystem、SetPieceController。
//=============================================================================
using UnityEngine;
using FC26.Core;
using FC26.Ball;
using FC26.Player;
using FC26.Match;

namespace FC26.Referee
{
    /// <summary>
    /// 裁判管理器单例：总调度越位、犯规、出界判定与定位球流程。
    /// </summary>
    public class RefereeManager : MonoSingleton<RefereeManager>
    {
        // ===== 球场尺寸常量 =====
        private const float HalfWidth = 34f;        // 半场宽度
        private const float HalfLength = 52.5f;     // 半场长度
        private const float GoalAreaDepth = 5.5f;   // 小禁区深度

        // ===== 子系统引用 =====
        // 越位判定器（普通类实例，由本类创建）
        private OffsideChecker _offsideChecker;

        // 犯规判定器（普通类实例，由本类创建）
        private FoulChecker _foulChecker;

        /// <summary>
        /// Awake：注册单例（基类完成），创建判定器实例。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            _offsideChecker = new OffsideChecker();
            _foulChecker = new FoulChecker();
        }

        /// <summary>
        /// OnEnable：订阅出界事件。
        /// </summary>
        private void OnEnable()
        {
            EventBus.Subscribe<BallOutOfPlayEvent>(OnBallOutOfPlay);
        }

        /// <summary>
        /// OnDisable：取消订阅。
        /// </summary>
        private void OnDisable()
        {
            EventBus.Unsubscribe<BallOutOfPlayEvent>(OnBallOutOfPlay);
        }

        /// <summary>
        /// Update：周期性规则检查。
        /// 当前为轻量占位实现，后续可扩展实时越位监控、犯规检测等。
        /// </summary>
        private void Update()
        {
            // 仅在比赛进行时检查
            if (MatchManager.Instance == null || !MatchManager.Instance.IsInProgress)
            {
                return;
            }

            // 此处可扩展实时规则检查，例如：
            // - 监控球员位置是否越位（基于传球快照）
            // - 检测球员间碰撞是否构成犯规
            // - 检查球员是否停留在禁区内过久
            //
            // 当前版本由 Player/AI 模块通过调用 CheckFoul / SnapshotPass / CheckOffsideWithSnapshot
            // 主动触发判定，Update 仅作占位。
        }

        // ====================================================================
        #region 犯规判定

        /// <summary>
        /// 犯规判定入口。由 Player/AI 模块在铲球/拼抢时调用。
        /// 判定流程：
        ///   1. 调用 FoulChecker.CheckFoul 判定是否犯规
        ///   2. 若犯规：发布 FoulCommittedEvent
        ///   3. 根据严重程度调用 DisciplineSystem 出牌
        ///   4. 调用 SetPieceController 设置任意球/点球
        ///   5. 记录犯规统计
        /// </summary>
        /// <param name="tacklerPos">铲球者位置</param>
        /// <param name="victimPos">被铲者位置</param>
        /// <param name="tacklePower">铲球力量（0~1）</param>
        /// <param name="inPenaltyArea">是否在防守方禁区内</param>
        /// <param name="tacklerPlayerId">铲球者球员 ID</param>
        /// <param name="victimPlayerId">被铲者球员 ID</param>
        /// <param name="tacklerTeamId">铲球者队伍 ID</param>
        /// <returns>犯规判定结果</returns>
        public FoulResult CheckFoul(Vector3 tacklerPos, Vector3 victimPos, float tacklePower,
                                    bool inPenaltyArea, int tacklerPlayerId, int victimPlayerId,
                                    int tacklerTeamId)
        {
            // 1. 调用犯规判定器
            FoulResult result = _foulChecker.CheckFoul(tacklerPos, victimPos, tacklePower, inPenaltyArea);

            if (!result.IsFoul)
            {
                return result;
            }

            // 2. 发布犯规事件
            EventBus.Publish(new FoulCommittedEvent
            {
                FoulingPlayerId = tacklerPlayerId,
                VictimPlayerId = victimPlayerId,
                Severity = result.Severity,
                IsPenalty = result.IsPenalty,
                Position = result.FreeKickPosition
            });

            // 3. 根据严重程度出牌
            if (DisciplineSystem.Instance != null)
            {
                switch (result.Severity)
                {
                    case 2:
                        // 中等犯规 → 黄牌
                        DisciplineSystem.Instance.ShowYellowCard(tacklerPlayerId);
                        break;
                    case 3:
                        // 严重犯规 → 红牌
                        DisciplineSystem.Instance.ShowRedCard(tacklerPlayerId);
                        break;
                    // 严重度 1 不出牌，仅任意球
                }
            }

            // 4. 设置定位球
            // 被犯规方（对方）获得任意球/点球
            int restartTeamId = (tacklerTeamId == 0) ? 1 : 0;

            if (SetPieceController.Instance != null)
            {
                if (result.IsPenalty)
                {
                    SetPieceController.Instance.SetupPenalty(restartTeamId);
                }
                else
                {
                    SetPieceController.Instance.SetupFreeKick(result.FreeKickPosition, restartTeamId);
                }
            }

            // 5. 记录犯规统计
            MatchManager.Instance?.Statistics?.RecordFoul(tacklerTeamId);

            // 6. 记录犯规用于伤停补时计算
            MatchTimer.Instance?.RecordFoulForStoppage();

            Debug.Log($"[RefereeManager] 犯规判定：铲球者={tacklerPlayerId}，被铲者={victimPlayerId}，" +
                      $"严重度={result.Severity}，点球={result.IsPenalty}，位置={result.FreeKickPosition}");

            return result;
        }

        /// <summary>
        /// 静态方法：检查指定位置是否在禁区范围内。
        /// 供 Player/AI 模块在铲球前调用以判断是否在禁区内。
        /// </summary>
        /// <param name="position">待检查位置</param>
        /// <param name="defendingPlusZ">防守方球门是否在 +z 端</param>
        /// <returns>true=在禁区内</returns>
        public static bool IsInPenaltyArea(Vector3 position, bool defendingPlusZ)
        {
            return FoulChecker.IsInPenaltyArea(position, defendingPlusZ);
        }

        #endregion

        // ====================================================================
        #region 越位判定

        /// <summary>
        /// 传球瞬间快照。由 Player/AI 模块在传球时调用。
        /// 记录传球瞬间的球位置、传球者位置、防守球员位置。
        /// </summary>
        /// <param name="ballPos">传球瞬间球位置</param>
        /// <param name="passerPos">传球者位置</param>
        /// <param name="defenders">防守球员位置数组</param>
        public void SnapshotPass(Vector3 ballPos, Vector3 passerPos, Vector3[] defenders)
        {
            _offsideChecker.SnapshotPass(ballPos, passerPos, defenders);
        }

        /// <summary>
        /// 使用快照判定越位。由 Player/AI 模块在接球时调用。
        /// 若越位：发布 OffsideEvent，设置间接任意球。
        /// </summary>
        /// <param name="attackerPos">接球进攻球员位置</param>
        /// <param name="attackerId">接球球员 ID</param>
        /// <param name="attackerTeamId">接球球员队伍 ID</param>
        /// <returns>true=越位，false=未越位</returns>
        public bool CheckOffsideWithSnapshot(Vector3 attackerPos, int attackerId, int attackerTeamId)
        {
            bool isOffside = _offsideChecker.CheckOffsideWithSnapshot(attackerPos);

            if (!isOffside)
            {
                return false;
            }

            // 发布越位事件
            EventBus.Publish(new OffsideEvent
            {
                PlayerId = attackerId,
                Position = attackerPos
            });

            // 越位 → 对方获得间接任意球
            int restartTeamId = (attackerTeamId == 0) ? 1 : 0;
            SetPieceController.Instance?.SetupFreeKick(attackerPos, restartTeamId);

            // 清除快照
            _offsideChecker.ClearSnapshot();

            Debug.Log($"[RefereeManager] 越位判定：球员={attackerId}，位置={attackerPos}，" +
                      $"对方获任意球，队伍={restartTeamId}");

            return true;
        }

        /// <summary>
        /// 直接调用越位判定（不使用快照）。
        /// 传入所有参数进行即时判定。
        /// </summary>
        /// <param name="attackerPos">进攻球员位置</param>
        /// <param name="passerPos">传球者位置</param>
        /// <param name="defenders">防守球员位置数组</param>
        /// <param name="ballPosAtPass">传球瞬间球位置</param>
        /// <returns>true=越位</returns>
        public bool CheckOffside(Vector3 attackerPos, Vector3 passerPos, Vector3[] defenders,
                                 Vector3 ballPosAtPass)
        {
            return _offsideChecker.CheckOffside(attackerPos, passerPos, defenders, ballPosAtPass);
        }

        /// <summary>
        /// 清除越位快照。
        /// </summary>
        public void ClearOffsideSnapshot()
        {
            _offsideChecker.ClearSnapshot();
        }

        #endregion

        // ====================================================================
        #region 出界处理

        /// <summary>
        /// 出界事件回调。根据出界类型设置对应的定位球。
        /// </summary>
        /// <param name="evt">出界事件</param>
        private void OnBallOutOfPlay(BallOutOfPlayEvent evt)
        {
            if (SetPieceController.Instance == null)
            {
                return;
            }

            // 仅在比赛进行时处理
            if (MatchManager.Instance == null || !MatchManager.Instance.IsInProgress)
            {
                return;
            }

            switch (evt.Type)
            {
                case 0:
                    // 界外球
                    SetPieceController.Instance.SetupThrowIn(evt.Position, evt.RestartTeamId);
                    Debug.Log($"[RefereeManager] 出界→界外球：位置={evt.Position}，队伍={evt.RestartTeamId}");
                    break;

                case 1:
                    // 角球
                    HandleCorner(evt.Position, evt.RestartTeamId);
                    break;

                case 2:
                    // 球门球
                    HandleGoalKick(evt.Position, evt.RestartTeamId);
                    break;

                default:
                    Debug.LogWarning($"[RefereeManager] 未知出界类型：{evt.Type}");
                    break;
            }
        }

        /// <summary>
        /// 处理角球。根据出界位置确定左/右侧角球。
        /// </summary>
        /// <param name="outPosition">出界位置</param>
        /// <param name="attackingTeamId">进攻方（执行角球的）队伍 ID</param>
        private void HandleCorner(Vector3 outPosition, int attackingTeamId)
        {
            // 获取进攻方向
            int attackDir = GetAttackDirection(attackingTeamId);

            // 判断左/右侧（从进攻方视角）
            // isLeft = (出界位置.x * attackDir < 0)
            bool isLeft = (outPosition.x * attackDir) < 0f;

            SetPieceController.Instance.SetupCorner(attackingTeamId, isLeft);

            // 记录角球统计
            MatchManager.Instance?.Statistics?.RecordCorner(attackingTeamId);

            Debug.Log($"[RefereeManager] 出界→角球：队伍={attackingTeamId}，左侧={isLeft}");
        }

        /// <summary>
        /// 处理球门球。将球放置到小禁区前沿。
        /// </summary>
        /// <param name="outPosition">出界位置</param>
        /// <param name="defendingTeamId">防守方（执行球门球的）队伍 ID</param>
        private void HandleGoalKick(Vector3 outPosition, int defendingTeamId)
        {
            // 球门球位置：小禁区前沿中心
            // 出界在 z+ 端 → 球门球在 z = HalfLength - GoalAreaDepth = 47
            // 出界在 z- 端 → 球门球在 z = -HalfLength + GoalAreaDepth = -47
            float goalKickZ = (outPosition.z > 0f)
                ? (HalfLength - GoalAreaDepth)
                : (-HalfLength + GoalAreaDepth);

            Vector3 goalKickPos = new Vector3(0f, 0.11f, goalKickZ);

            SetPieceController.Instance.SetupFreeKick(goalKickPos, defendingTeamId);

            Debug.Log($"[RefereeManager] 出界→球门球：位置={goalKickPos}，队伍={defendingTeamId}");
        }

        #endregion

        // ====================================================================
        #region 内部工具

        /// <summary>
        /// 获取指定队伍的进攻方向。
        /// 优先从 PlayerFactory 获取，若不可用则默认（主队 +z，客队 -z）。
        /// </summary>
        /// <param name="teamId">队伍 ID</param>
        /// <returns>+1=进攻 +z，-1=进攻 -z</returns>
        private int GetAttackDirection(int teamId)
        {
            if (PlayerFactory.Instance != null)
            {
                return PlayerFactory.Instance.GetTeamAttackDirection(teamId);
            }
            return (teamId == 0) ? 1 : -1;
        }

        #endregion
    }
}
