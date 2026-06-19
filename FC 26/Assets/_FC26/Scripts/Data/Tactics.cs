using UnityEngine;

namespace FC26.Data
{
    /// <summary>
    /// 战术风格枚举。每种风格对应一组默认战术参数（见 Tactics.GetParams）。
    /// </summary>
    public enum TacticStyle
    {
        Possession,       // 控球：注重控球率与短传渗透
        Counter,          // 反击：注重防守反击与快速推进
        HighPress,        // 高位压迫：前场积极上抢、防线前压
        DefensiveCounter  // 防守反击：低位防守、抢断后快速长传反击
    }

    /// <summary>
    /// 战术参数结构体。所有 float 字段范围 0-1，用于驱动 AI 决策与球员跑位。
    /// </summary>
    [System.Serializable]
    public struct TacticParams
    {
        [Tooltip("进攻倾向 0-1，越高越倾向压上进攻")] public float AttackTendency;
        [Tooltip("压迫强度 0-1，越高越积极上抢")] public float PressIntensity;
        [Tooltip("防线高度 0-1，越高防线越靠上")] public float DefensiveLineHeight;
        [Tooltip("传球冒险度 0-1，越高越倾向直塞/长传")] public float PassRisk;
        [Tooltip("宽度利用 0-1，越高越拉开边路")] public float WidthUsage;
        [Tooltip("节奏 0-1，越高攻防转换越快")] public float Tempo;

        /// <summary>构造战术参数</summary>
        public TacticParams(float atk, float press, float line, float risk, float width, float tempo)
        {
            AttackTendency = atk;
            PressIntensity = press;
            DefensiveLineHeight = line;
            PassRisk = risk;
            WidthUsage = width;
            Tempo = tempo;
        }
    }

    /// <summary>
    /// 战术参数表。为每种战术风格提供默认参数，AI 与布阵系统据此调整行为。
    /// </summary>
    public static class Tactics
    {
        /// <summary>控球：中高进攻倾向、中压迫、中防线、低冒险传球、高宽度、中节奏</summary>
        public static readonly TacticParams PossessionParams =
            new TacticParams(0.65f, 0.45f, 0.55f, 0.35f, 0.70f, 0.55f);

        /// <summary>反击：中低进攻倾向、低压迫、低防线、高冒险传球、中宽度、高节奏</summary>
        public static readonly TacticParams CounterParams =
            new TacticParams(0.45f, 0.35f, 0.30f, 0.65f, 0.60f, 0.75f);

        /// <summary>高位压迫：高进攻倾向、极高压迫、高防线、中冒险传球、中宽度、高节奏</summary>
        public static readonly TacticParams HighPressParams =
            new TacticParams(0.70f, 0.85f, 0.80f, 0.55f, 0.65f, 0.85f);

        /// <summary>防守反击：低进攻倾向、中压迫、极低防线、高冒险传球、中宽度、中高节奏</summary>
        public static readonly TacticParams DefensiveCounterParams =
            new TacticParams(0.35f, 0.40f, 0.25f, 0.70f, 0.55f, 0.70f);

        /// <summary>
        /// 根据战术风格返回默认参数。
        /// 若传入未知枚举值，默认回退到控球参数。
        /// </summary>
        public static TacticParams GetParams(TacticStyle style)
        {
            switch (style)
            {
                case TacticStyle.Possession: return PossessionParams;
                case TacticStyle.Counter: return CounterParams;
                case TacticStyle.HighPress: return HighPressParams;
                case TacticStyle.DefensiveCounter: return DefensiveCounterParams;
                default: return PossessionParams;
            }
        }
    }
}
