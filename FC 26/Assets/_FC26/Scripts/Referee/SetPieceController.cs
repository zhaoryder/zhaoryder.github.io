//=============================================================================
// 文件名：SetPieceController.cs
// 所属模块：Referee
// 命名空间：FC26.Referee
// 作用：定位球控制单例。继承 MonoSingleton<SetPieceController>，全局唯一。
//       管理任意球、点球、角球、界外球的流程：重置球位置、重置球员位置、
//       广播 SetPieceEvent 通知其他模块。
//       球场尺寸：长 105 米（z 轴 -52.5~52.5），宽 68 米（x 轴 -34~34）。
//       点球点距底线 11 米，角球弧半径 1 米。
// 备注：本脚本需挂载在场景中的 GameObject 上。
//       依赖 BallManager（重置球位置）与 PlayerFactory（重置球员位置、获取进攻方向）。
//=============================================================================
using UnityEngine;
using FC26.Core;
using FC26.Ball;
using FC26.Player;

namespace FC26.Referee
{
    /// <summary>
    /// 定位球控制单例：管理任意球、点球、角球、界外球流程。
    /// </summary>
    public class SetPieceController : MonoSingleton<SetPieceController>
    {
        // ===== 球场尺寸常量 =====
        private const float HalfWidth = 34f;              // 半场宽度
        private const float HalfLength = 52.5f;           // 半场长度
        private const float PenaltySpotDist = 11f;        // 点球点距底线距离
        private const float BallRadius = 0.11f;           // 足球半径（地面放置高度）

        // ===== 定位球类型枚举 =====
        // 0=任意球, 1=点球, 2=角球, 3=界外球
        public const int TypeFreeKick = 0;
        public const int TypePenalty = 1;
        public const int TypeCorner = 2;
        public const int TypeThrowIn = 3;

        /// <summary>
        /// 设置任意球。
        /// 将球放置到指定位置，重置两队球员到阵型位置，广播 SetPieceEvent。
        /// </summary>
        /// <param name="position">任意球执行位置</param>
        /// <param name="teamId">执行任意球的队伍 ID</param>
        public void SetupFreeKick(Vector3 position, int teamId)
        {
            // 确保位置在球场内且在地面
            Vector3 ballPos = ClampToPitch(position);

            // 重置球到任意球位置
            BallManager.Instance?.ResetBallToPosition(ballPos);

            // 重置两队球员到阵型位置
            PlayerFactory.Instance?.ResetAllTeamsToFormation();

            // 广播定位球事件
            EventBus.Publish(new SetPieceEvent
            {
                Type = TypeFreeKick,
                Position = ballPos,
                TeamId = teamId
            });

            Debug.Log($"[SetPieceController] 设置任意球：位置={ballPos}，队伍={teamId}");
        }

        /// <summary>
        /// 设置点球。
        /// 将球放置到点球点，重置两队球员，广播 SetPieceEvent。
        /// 点球点位置根据进攻方向确定：进攻 +z → z=+41.5，进攻 -z → z=-41.5。
        /// </summary>
        /// <param name="teamId">执行点球的队伍 ID</param>
        public void SetupPenalty(int teamId)
        {
            // 获取进攻方向
            int attackDir = GetAttackDirection(teamId);

            // 计算点球点位置
            // 进攻 +z：点球点在 z = HalfLength - PenaltySpotDist = 41.5
            // 进攻 -z：点球点在 z = -HalfLength + PenaltySpotDist = -41.5
            float penaltyZ = (HalfLength - PenaltySpotDist) * attackDir;
            Vector3 ballPos = new Vector3(0f, BallRadius, penaltyZ);

            // 重置球到点球点
            BallManager.Instance?.ResetBallToPosition(ballPos);

            // 重置两队球员到阵型位置
            PlayerFactory.Instance?.ResetAllTeamsToFormation();

            // 广播定位球事件
            EventBus.Publish(new SetPieceEvent
            {
                Type = TypePenalty,
                Position = ballPos,
                TeamId = teamId
            });

            Debug.Log($"[SetPieceController] 设置点球：位置={ballPos}，队伍={teamId}");
        }

        /// <summary>
        /// 设置角球。
        /// 将球放置到角球弧位置，重置两队球员，广播 SetPieceEvent。
        /// 角球位置根据进攻方向与左右侧确定。
        /// </summary>
        /// <param name="teamId">执行角球的队伍 ID</param>
        /// <param name="isLeft">是否左侧角球（从进攻方视角）</param>
        public void SetupCorner(int teamId, bool isLeft)
        {
            // 获取进攻方向
            int attackDir = GetAttackDirection(teamId);

            // 计算角球位置
            // 进攻 +z 时，角球在 z=+52.5 端；进攻 -z 时，角球在 z=-52.5 端
            float cornerZ = HalfLength * attackDir;

            // 左右侧（从进攻方视角）：
            // 进攻 +z 时，进攻方面向 +z，左侧为 -x
            // 进攻 -z 时，进攻方面向 -z，左侧为 +x（镜像）
            float cornerX = isLeft ? (-HalfWidth * attackDir) : (HalfWidth * attackDir);

            Vector3 ballPos = new Vector3(cornerX, BallRadius, cornerZ);

            // 重置球到角球位置
            BallManager.Instance?.ResetBallToPosition(ballPos);

            // 重置两队球员到阵型位置
            PlayerFactory.Instance?.ResetAllTeamsToFormation();

            // 广播定位球事件
            EventBus.Publish(new SetPieceEvent
            {
                Type = TypeCorner,
                Position = ballPos,
                TeamId = teamId
            });

            Debug.Log($"[SetPieceController] 设置角球：位置={ballPos}，队伍={teamId}，左侧={isLeft}");
        }

        /// <summary>
        /// 设置界外球。
        /// 将球放置到出界位置（边线内侧），重置球员，广播 SetPieceEvent。
        /// </summary>
        /// <param name="position">出界位置（球将放置在最近的边线内侧）</param>
        /// <param name="teamId">执行界外球的队伍 ID</param>
        public void SetupThrowIn(Vector3 position, int teamId)
        {
            // 将球位置限制到边线内侧
            Vector3 ballPos = position;

            // 确保球在边线内侧（|x| <= HalfWidth - 边距）
            const float margin = 0.5f; // 边线内侧 0.5 米
            ballPos.x = Mathf.Clamp(ballPos.x, -HalfWidth + margin, HalfWidth - margin);

            // 确保在地面
            ballPos.y = BallRadius;

            // 重置球到界外球位置
            BallManager.Instance?.ResetBallToPosition(ballPos);

            // 广播定位球事件
            EventBus.Publish(new SetPieceEvent
            {
                Type = TypeThrowIn,
                Position = ballPos,
                TeamId = teamId
            });

            Debug.Log($"[SetPieceController] 设置界外球：位置={ballPos}，队伍={teamId}");
        }

        // ====================================================================
        #region 内部工具

        /// <summary>
        /// 获取指定队伍的进攻方向。
        /// 优先从 PlayerFactory 获取，若不可用则根据 teamId 默认推断
        /// （主队进攻 +z，客队进攻 -z）。
        /// </summary>
        /// <param name="teamId">队伍 ID</param>
        /// <returns>+1=进攻 +z，-1=进攻 -z</returns>
        private int GetAttackDirection(int teamId)
        {
            if (PlayerFactory.Instance != null)
            {
                return PlayerFactory.Instance.GetTeamAttackDirection(teamId);
            }

            // 默认：主队进攻 +z，客队进攻 -z
            return (teamId == 0) ? 1 : -1;
        }

        /// <summary>
        /// 将位置限制在球场范围内，并确保在地面。
        /// </summary>
        /// <param name="position">原始位置</param>
        /// <returns>限制后的位置</returns>
        private Vector3 ClampToPitch(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, -HalfWidth, HalfWidth);
            position.z = Mathf.Clamp(position.z, -HalfLength, HalfLength);
            position.y = BallRadius;
            return position;
        }

        #endregion
    }
}

//=============================================================================
// 以下为定位球事件结构体定义，置于 FC26.Core 命名空间以保持事件一致性。
//=============================================================================
namespace FC26.Core
{
    /// <summary>
    /// 定位球事件。当设置任意球/点球/角球/界外球时由 SetPieceController 发布。
    /// </summary>
    public struct SetPieceEvent
    {
        /// <summary>
        /// 定位球类型：0=任意球, 1=点球, 2=角球, 3=界外球。
        /// 对应 SetPieceController.TypeFreeKick / TypePenalty / TypeCorner / TypeThrowIn。
        /// </summary>
        public int Type;

        /// <summary>定位球执行位置</summary>
        public Vector3 Position;

        /// <summary>执行定位球的队伍 ID</summary>
        public int TeamId;
    }
}
