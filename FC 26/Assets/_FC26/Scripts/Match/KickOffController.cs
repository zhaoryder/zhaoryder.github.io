//=============================================================================
// 文件名：KickOffController.cs
// 所属模块：Match
// 命名空间：FC26.Match
// 作用：开球控制单例。继承 MonoSingleton<KickOffController>，全局唯一。
//       负责执行开球流程：重置两队球员到阵型初始位置、重置球到中圈。
//       监听 GoalScoredEvent 自动执行开球（失球方开球）。
// 备注：本脚本需挂载在场景中的 GameObject 上。
//       依赖 BallManager（重置球位置）与 PlayerFactory（重置球员位置）。
//=============================================================================
using UnityEngine;
using FC26.Core;
using FC26.Ball;
using FC26.Player;

namespace FC26.Match
{
    /// <summary>
    /// 开球控制单例：管理开球流程与球员位置重置。
    /// </summary>
    public class KickOffController : MonoSingleton<KickOffController>
    {
        /// <summary>
        /// OnEnable：订阅进球事件。
        /// </summary>
        private void OnEnable()
        {
            EventBus.Subscribe<GoalScoredEvent>(OnGoalScored);
        }

        /// <summary>
        /// OnDisable：取消订阅进球事件。
        /// </summary>
        private void OnDisable()
        {
            EventBus.Unsubscribe<GoalScoredEvent>(OnGoalScored);
        }

        /// <summary>
        /// 执行开球。
        /// 重置两队球员到阵型初始位置，重置球到中圈中心。
        /// </summary>
        /// <param name="teamId">开球队伍 ID（失球方开球）</param>
        public void PerformKickOff(int teamId)
        {
            // 重置两队球员到阵型初始位置
            ResetPositions();

            // 重置球到中圈中心
            BallManager.Instance?.ResetBallToCenter();

            // 清除控球归属
            BallManager.Instance?.ClearPossession();

            Debug.Log($"[KickOffController] 执行开球：开球队伍={teamId}");
        }

        /// <summary>
        /// 重置两队球员到阵型初始位置。
        /// </summary>
        public void ResetPositions()
        {
            PlayerFactory.Instance?.ResetAllTeamsToFormation();
        }

        /// <summary>
        /// 重置指定队伍球员到阵型初始位置。
        /// </summary>
        /// <param name="teamId">队伍 ID</param>
        public void ResetTeamPositions(int teamId)
        {
            PlayerFactory.Instance?.ResetTeamToFormation(teamId);
        }

        /// <summary>
        /// 进球事件回调：失球方开球。
        /// GoalScoredEvent.TeamId 是得分方，开球方为对方。
        /// </summary>
        /// <param name="evt">进球事件</param>
        private void OnGoalScored(GoalScoredEvent evt)
        {
            // 失球方开球：得分方为 evt.TeamId，开球方为对方
            int kickOffTeamId = (evt.TeamId == 0) ? 1 : 0;

            // 延迟一帧执行开球，确保进球处理（比分更新等）先完成
            // 此处直接调用，MatchManager 也会监听进球事件更新比分
            PerformKickOff(kickOffTeamId);
        }
    }
}
