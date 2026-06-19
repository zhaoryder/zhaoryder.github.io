//=============================================================================
// 文件名：OffsideChecker.cs
// 所属模块：Referee
// 命名空间：FC26.Referee
// 作用：越位判定器。提供传球瞬间的越位位置判定。
//       越位规则：进攻球员在传球瞬间，若比倒数第二防守球员更靠近对方底线，
//       且比球更靠近对方底线，则处于越位位置。
//       本类实现传球瞬间快照机制：在传球发生时快照所有相关位置，
//       后续判定使用快照而非实时位置。
// 备注：本类为普通 C# 类（非 MonoBehaviour），由 RefereeManager 持有实例。
//       进攻方向通过防守球员平均位置推断：防守球员集中在哪一侧，对方球门就在哪一侧。
//=============================================================================
using UnityEngine;
using FC26.Core;

namespace FC26.Referee
{
    /// <summary>
    /// 越位判定器：基于传球瞬间快照判定越位位置。
    /// </summary>
    public class OffsideChecker
    {
        // ===== 快照数据 =====
        // 传球瞬间的球位置
        private Vector3 _snapshotBallPos;

        // 传球瞬间的传球者位置
        private Vector3 _snapshotPasserPos;

        // 传球瞬间的防守球员位置数组
        private Vector3[] _snapshotDefenders;

        // 是否已有快照
        private bool _hasSnapshot = false;

        // ===== 判定参数 =====
        // 越位容差（米）：考虑到位置误差，进攻球员与倒数第二防守球员的距离差小于此值时不判越位
        private const float OffsideTolerance = 0.3f;

        /// <summary>
        /// 在传球瞬间快照所有相关位置。
        /// 应在传球发生的同一帧调用，以捕获传球瞬间的准确位置。
        /// </summary>
        /// <param name="ballPos">传球瞬间的球位置</param>
        /// <param name="passerPos">传球者位置</param>
        /// <param name="defenders">防守球员位置数组</param>
        public void SnapshotPass(Vector3 ballPos, Vector3 passerPos, Vector3[] defenders)
        {
            _snapshotBallPos = ballPos;
            _snapshotPasserPos = passerPos;

            // 深拷贝防守球员位置数组，避免外部修改影响快照
            if (defenders != null)
            {
                _snapshotDefenders = new Vector3[defenders.Length];
                System.Array.Copy(defenders, _snapshotDefenders, defenders.Length);
            }
            else
            {
                _snapshotDefenders = null;
            }

            _hasSnapshot = true;
        }

        /// <summary>
        /// 清除快照。在越位判定完成或传球被取消后调用。
        /// </summary>
        public void ClearSnapshot()
        {
            _hasSnapshot = false;
            _snapshotDefenders = null;
        }

        /// <summary>
        /// 使用快照数据判定指定进攻球员是否越位。
        /// </summary>
        /// <param name="attackerPos">进攻球员位置（当前帧）</param>
        /// <returns>true=越位，false=未越位或无快照</returns>
        public bool CheckOffsideWithSnapshot(Vector3 attackerPos)
        {
            if (!_hasSnapshot || _snapshotDefenders == null || _snapshotDefenders.Length < 2)
            {
                return false;
            }

            return CheckOffside(attackerPos, _snapshotPasserPos, _snapshotDefenders, _snapshotBallPos);
        }

        /// <summary>
        /// 越位判定核心方法。
        /// 判定逻辑：
        ///   1. 推断进攻方向（通过防守球员平均位置）
        ///   2. 找到倒数第二防守球员（沿进攻方向最深的第二名防守球员）
        ///   3. 比较进攻球员与倒数第二防守球员沿进攻方向的深度
        ///   4. 进攻球员比倒数第二防守球员更靠近对方底线 → 越位
        ///   5. 进攻球员不能比球更靠近对方底线（否则不越位，因为球在前方）
        /// </summary>
        /// <param name="attackerPos">进攻球员位置</param>
        /// <param name="passerPos">传球者位置</param>
        /// <param name="defenders">防守球员位置数组</param>
        /// <param name="ballPosAtPass">传球瞬间的球位置</param>
        /// <returns>true=越位，false=未越位</returns>
        public bool CheckOffside(Vector3 attackerPos, Vector3 passerPos, Vector3[] defenders, Vector3 ballPosAtPass)
        {
            // 防守球员不足 2 人无法判定（至少需要门将 + 1 名后卫）
            if (defenders == null || defenders.Length < 2)
            {
                return false;
            }

            // 1. 推断进攻方向
            // 防守球员平均位置在哪一侧，对方球门就在哪一侧
            // 进攻方向 = 从防守球员平均位置指向对方球门
            float defenderAvgZ = 0f;
            for (int i = 0; i < defenders.Length; i++)
            {
                defenderAvgZ += defenders[i].z;
            }
            defenderAvgZ /= defenders.Length;

            // 进攻方向：+1 表示进攻 +z（防守方在 +z 侧），-1 表示进攻 -z
            int attackDir = (defenderAvgZ > attackerPos.z) ? 1 : -1;

            // 2. 找到倒数第二防守球员
            // 沿进攻方向最深的防守球员是"最后一名"（通常是门将）
            // 第二深的则是"倒数第二名"
            float deepestZ = float.MinValue;
            float secondDeepestZ = float.MinValue;

            for (int i = 0; i < defenders.Length; i++)
            {
                // 沿进攻方向的深度：进攻 +z 时用 z 值，进攻 -z 时用 -z 值
                float depth = defenders[i].z * attackDir;

                if (depth > deepestZ)
                {
                    secondDeepestZ = deepestZ;
                    deepestZ = depth;
                }
                else if (depth > secondDeepestZ)
                {
                    secondDeepestZ = depth;
                }
            }

            // 3. 比较进攻球员与倒数第二防守球员的深度
            float attackerDepth = attackerPos.z * attackDir;

            // 进攻球员必须比倒数第二防守球员更靠近对方底线（深度更大）
            // 加上容差，避免边缘情况误判
            if (attackerDepth <= secondDeepestZ + OffsideTolerance)
            {
                return false; // 进攻球员未越过倒数第二防守球员，不越位
            }

            // 4. 进攻球员不能比球更靠近对方底线（球在前方则不越位）
            float ballDepth = ballPosAtPass.z * attackDir;
            if (attackerDepth <= ballDepth + OffsideTolerance)
            {
                return false; // 进攻球员未越过球，不越位
            }

            // 5. 进攻球员不能在本方半场（本方半场不越位）
            // 进攻 +z 时，本方半场为 z < 0；进攻 -z 时，本方半场为 z > 0
            if (attackDir == 1 && attackerPos.z < 0f)
            {
                return false;
            }
            if (attackDir == -1 && attackerPos.z > 0f)
            {
                return false;
            }

            // 所有条件满足 → 越位
            return true;
        }

        /// <summary>
        /// 获取快照中传球瞬间的球位置。
        /// </summary>
        public Vector3 SnapshotBallPos => _snapshotBallPos;

        /// <summary>
        /// 是否已有传球快照。
        /// </summary>
        public bool HasSnapshot => _hasSnapshot;
    }
}

//=============================================================================
// 以下为越位事件结构体定义，置于 FC26.Core 命名空间以保持事件一致性。
//=============================================================================
namespace FC26.Core
{
    /// <summary>
    /// 越位事件。当裁判判定越位时由 RefereeManager 发布。
    /// </summary>
    public struct OffsideEvent
    {
        /// <summary>越位球员 ID</summary>
        public int PlayerId;

        /// <summary>越位位置</summary>
        public Vector3 Position;
    }
}
