//=============================================================================
// 文件名：AIDifficulty.cs
// 所属模块：AI
// 命名空间：FC26.AI
// 作用：定义 AI 难度等级与对应参数。难度参数驱动 AI 决策核心与状态机的行为：
//   - ReactionTime：反应时间（秒），AI 两次决策之间的最小间隔，越低越灵敏。
//   - DecisionAccuracy：决策精度（0-1），越高越倾向选择最优动作，低难度会随机选次优。
//   - SkillMultiplier：技能倍率（0.8-1.2），影响传球/射门力量与跑动速度加成。
// 备注：纯静态数据表，无 MonoBehaviour 依赖，任意模块可通过 AIDifficulty.GetParams 读取。
//=============================================================================
using UnityEngine;

namespace FC26.AI
{
    /// <summary>
    /// AI 难度等级枚举。
    /// </summary>
    public enum AILevel
    {
        /// <summary>简单：反应慢、决策易失误、技能略弱</summary>
        Easy,
        /// <summary>普通：反应与决策适中</summary>
        Normal,
        /// <summary>困难：反应快、决策精准、技能略强</summary>
        Hard
    }

    /// <summary>
    /// AI 难度参数结构体。
    /// </summary>
    [System.Serializable]
    public struct AIDifficultyParams
    {
        [Tooltip("反应时间（秒）：AI 两次决策的最小间隔，越低越灵敏")]
        public float ReactionTime;

        [Tooltip("决策精度（0-1）：越高越倾向选择最优动作")]
        [Range(0f, 1f)] public float DecisionAccuracy;

        [Tooltip("技能倍率（0.8-1.2）：影响传球/射门力量与跑动加成")]
        [Range(0.8f, 1.2f)] public float SkillMultiplier;

        /// <summary>构造难度参数。</summary>
        public AIDifficultyParams(float reactionTime, float decisionAccuracy, float skillMultiplier)
        {
            ReactionTime = reactionTime;
            DecisionAccuracy = Mathf.Clamp01(decisionAccuracy);
            SkillMultiplier = Mathf.Clamp(skillMultiplier, 0.8f, 1.2f);
        }
    }

    /// <summary>
    /// AI 难度参数表。为每个难度等级提供默认参数。
    /// </summary>
    public static class AIDifficulty
    {
        /// <summary>简单：反应 0.6s、精度 0.55、技能 0.85</summary>
        public static readonly AIDifficultyParams EasyParams =
            new AIDifficultyParams(0.60f, 0.55f, 0.85f);

        /// <summary>普通：反应 0.35s、精度 0.78、技能 1.00</summary>
        public static readonly AIDifficultyParams NormalParams =
            new AIDifficultyParams(0.35f, 0.78f, 1.00f);

        /// <summary>困难：反应 0.18s、精度 0.92、技能 1.12</summary>
        public static readonly AIDifficultyParams HardParams =
            new AIDifficultyParams(0.18f, 0.92f, 1.12f);

        /// <summary>
        /// 根据难度等级返回对应参数。
        /// 若传入未知枚举值，默认回退到普通难度。
        /// </summary>
        /// <param name="level">难度等级</param>
        /// <returns>难度参数</returns>
        public static AIDifficultyParams GetParams(AILevel level)
        {
            switch (level)
            {
                case AILevel.Easy: return EasyParams;
                case AILevel.Normal: return NormalParams;
                case AILevel.Hard: return HardParams;
                default: return NormalParams;
            }
        }
    }
}
