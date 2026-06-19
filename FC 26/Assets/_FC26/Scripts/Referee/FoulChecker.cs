//=============================================================================
// 文件名：FoulChecker.cs
// 所属模块：Referee
// 命名空间：FC26.Referee
// 作用：犯规判定器。根据铲球者与被铲者位置、铲球力量、是否在禁区内，
//       判定是否犯规、严重程度、是否点球，并计算任意球位置。
//       严重度分级：0=无犯规，1=轻微(任意球)，2=中等(黄牌)，3=严重(红牌)。
// 备注：本类为普通 C# 类（非 MonoBehaviour），由 RefereeManager 持有实例。
//       判定逻辑为基于距离与力量的启发式规则，不依赖物理引擎。
//=============================================================================
using UnityEngine;
using FC26.Core;

namespace FC26.Referee
{
    /// <summary>
    /// 犯规判定结果结构体。
    /// </summary>
    public struct FoulResult
    {
        /// <summary>是否犯规</summary>
        public bool IsFoul;

        /// <summary>
        /// 严重程度：0=无犯规，1=轻微(任意球)，2=中等(黄牌)，3=严重(红牌)。
        /// 仅当 IsFoul=true 时有效。
        /// </summary>
        public int Severity;

        /// <summary>是否点球（禁区内犯规）</summary>
        public bool IsPenalty;

        /// <summary>任意球/点球执行位置</summary>
        public Vector3 FreeKickPosition;
    }

    /// <summary>
    /// 犯规判定器：基于距离、力量与位置判定犯规。
    /// </summary>
    public class FoulChecker
    {
        // ===== 判定参数 =====
        // 铲球有效距离（米）：铲球者与被铲者距离小于此值才可能发生犯规
        private const float TackleRange = 2.0f;

        // 力量阈值：低于此值不犯规（合理铲球）
        private const float CleanTackleThreshold = 0.3f;

        // 轻微犯规力量阈值：[CleanTackleThreshold, MinorThreshold) → 严重度 1
        private const float MinorFoulThreshold = 0.55f;

        // 中等犯规力量阈值：[MinorThreshold, MediumThreshold) → 严重度 2
        private const float MediumFoulThreshold = 0.8f;

        // 严重度 3：力量 >= MediumFoulThreshold

        // 点球点距底线距离（米）
        private const float PenaltySpotDist = 11f;

        // 半场长度（米）
        private const float HalfLength = 52.5f;

        /// <summary>
        /// 犯规判定核心方法。
        /// 根据铲球者与被铲者的距离、铲球力量、是否在禁区内，判定犯规结果。
        /// </summary>
        /// <param name="tacklerPos">铲球者位置</param>
        /// <param name="victimPos">被铲者位置</param>
        /// <param name="tacklePower">铲球力量（0~1）</param>
        /// <param name="inPenaltyArea">是否在防守方禁区内</param>
        /// <returns>犯规判定结果</returns>
        public FoulResult CheckFoul(Vector3 tacklerPos, Vector3 victimPos, float tacklePower, bool inPenaltyArea)
        {
            FoulResult result = new FoulResult
            {
                IsFoul = false,
                Severity = 0,
                IsPenalty = false,
                FreeKickPosition = Vector3.zero
            };

            // 1. 检查距离：铲球者与被铲者距离必须在有效范围内
            float distance = Vector3.Distance(tacklerPos, victimPos);
            if (distance > TackleRange)
            {
                // 距离过远，未发生接触，不犯规
                return result;
            }

            // 2. 力量归一化
            float power = Mathf.Clamp01(tacklePower);

            // 3. 力量低于阈值 → 合理铲球，不犯规
            if (power < CleanTackleThreshold)
            {
                return result;
            }

            // 4. 判定为犯规，确定严重程度
            result.IsFoul = true;

            if (power < MinorFoulThreshold)
            {
                // 轻微犯规：任意球
                result.Severity = 1;
            }
            else if (power < MediumFoulThreshold)
            {
                // 中等犯规：黄牌
                result.Severity = 2;
            }
            else
            {
                // 严重犯规：红牌
                result.Severity = 3;
            }

            // 5. 判定点球与执行位置
            if (inPenaltyArea)
            {
                // 禁区内犯规 → 点球
                result.IsPenalty = true;

                // 点球位置：根据被铲者位置推断进攻方向，计算点球点
                // 进攻 +z 方向：点球点在 z = -HalfLength + PenaltySpotDist = -41.5
                // 进攻 -z 方向：点球点在 z = HalfLength - PenaltySpotDist = 41.5
                bool attackPlusZ = victimPos.z > tacklerPos.z;
                float penaltyZ = attackPlusZ
                    ? (HalfLength - PenaltySpotDist)   // 进攻 +z，点球点在 z+ 端
                    : (-HalfLength + PenaltySpotDist); // 进攻 -z，点球点在 z- 端
                result.FreeKickPosition = new Vector3(0f, 0f, penaltyZ);
            }
            else
            {
                // 禁区外犯规 → 任意球，位置为犯规发生地（被铲者位置）
                result.IsPenalty = false;
                result.FreeKickPosition = victimPos;
            }

            return result;
        }

        /// <summary>
        /// 检查指定位置是否在禁区范围内。
        /// 禁区：宽 40.32（x 轴 ±20.16），深 16.5（从底线向场内）。
        /// </summary>
        /// <param name="position">待检查位置</param>
        /// <param name="defendingPlusZ">防守方球门是否在 +z 端（true=守 +z 端，false=守 -z 端）</param>
        /// <returns>true=在禁区内，false=不在禁区内</returns>
        public static bool IsInPenaltyArea(Vector3 position, bool defendingPlusZ)
        {
            const float halfPenaltyWidth = 20.16f; // 40.32 / 2
            const float penaltyDepth = 16.5f;

            // x 方向：|x| <= 20.16
            if (Mathf.Abs(position.x) > halfPenaltyWidth)
            {
                return false;
            }

            // z 方向：根据防守方向判定
            if (defendingPlusZ)
            {
                // 防守 +z 端：禁区 z 范围 [HalfLength - penaltyDepth, HalfLength] = [36, 52.5]
                return position.z >= (HalfLength - penaltyDepth) && position.z <= HalfLength;
            }
            else
            {
                // 防守 -z 端：禁区 z 范围 [-HalfLength, -HalfLength + penaltyDepth] = [-52.5, -36]
                return position.z >= -HalfLength && position.z <= (-HalfLength + penaltyDepth);
            }
        }
    }
}

//=============================================================================
// 以下为犯规事件结构体定义，置于 FC26.Core 命名空间以保持事件一致性。
//=============================================================================
namespace FC26.Core
{
    /// <summary>
    /// 犯规事件。当裁判判定犯规时由 RefereeManager 发布。
    /// </summary>
    public struct FoulCommittedEvent
    {
        /// <summary>犯规球员 ID</summary>
        public int FoulingPlayerId;

        /// <summary>被犯规球员 ID</summary>
        public int VictimPlayerId;

        /// <summary>严重程度：1=轻微，2=中等，3=严重</summary>
        public int Severity;

        /// <summary>是否点球</summary>
        public bool IsPenalty;

        /// <summary>犯规位置（任意球/点球执行位置）</summary>
        public Vector3 Position;
    }
}
