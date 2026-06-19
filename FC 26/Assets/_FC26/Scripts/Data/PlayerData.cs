using UnityEngine;

namespace FC26.Data
{
    /// <summary>
    /// 球员位置枚举：门将 / 后卫 / 中场 / 前锋。
    /// 用于标识球员在球场上的大类位置，配合阵型坐标表完成布阵。
    /// </summary>
    public enum PlayerPosition
    {
        GK, // 门将 Goalkeeper
        DF, // 后卫 Defender
        MF, // 中场 Midfielder
        FW  // 前锋 Forward
    }

    /// <summary>
    /// 球员能力值结构体，所有字段范围 0-99。
    /// 五维能力：速度、传球、射门、防守、体能。
    /// </summary>
    [System.Serializable]
    public struct Ability
    {
        [Tooltip("速度：影响跑动最高速度与加速")] public int Speed;
        [Tooltip("传球：影响传球精度与球速")] public int Passing;
        [Tooltip("射门：影响射门精度与力量")] public int Shooting;
        [Tooltip("防守：影响拦截、铲断成功率")] public int Defending;
        [Tooltip("体能：影响体能消耗速率与续航")] public int Stamina;

        /// <summary>构造能力值</summary>
        public Ability(int speed, int passing, int shooting, int defending, int stamina)
        {
            Speed = speed;
            Passing = passing;
            Shooting = shooting;
            Defending = defending;
            Stamina = stamina;
        }
    }

    /// <summary>
    /// 球员数据。继承 ScriptableObject 以支持资源化（可在 Unity 编辑器创建 .asset），
    /// 同时保留默认构造以便 PlayerDatabase 在运行时用 new 实例化纯数据对象。
    /// 字段全部公开，便于数据层与比赛层直接读取。
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerData", menuName = "FC26/PlayerData", order = 1)]
    public class PlayerData : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("球员姓名")] public string Name;
        [Tooltip("球衣号码")] public int Number;
        [Tooltip("场上位置 GK/DF/MF/FW")] public PlayerPosition Position;
        [Tooltip("所属球队 ID（与 TeamData.TeamID 对应）")] public string TeamID;

        [Header("能力值 0-99")]
        [Tooltip("五维能力值")] public Ability Stats;

        /// <summary>
        /// 综合评分（按位置加权计算，用于排序与 UI 显示）。
        /// GK 偏重防守，DF 偏重防守与速度，MF 偏重传球与体能，FW 偏重射门与速度。
        /// </summary>
        public int Overall
        {
            get
            {
                switch (Position)
                {
                    case PlayerPosition.GK:
                        return (Stats.Defending * 3 + Stats.Stamina + Stats.Speed) / 5;
                    case PlayerPosition.DF:
                        return (Stats.Defending * 3 + Stats.Speed + Stats.Stamina + Stats.Passing) / 6;
                    case PlayerPosition.MF:
                        return (Stats.Passing * 2 + Stats.Stamina + Stats.Speed + Stats.Shooting + Stats.Defending) / 6;
                    case PlayerPosition.FW:
                        return (Stats.Shooting * 3 + Stats.Speed + Stats.Passing + Stats.Stamina) / 6;
                    default:
                        return (Stats.Speed + Stats.Passing + Stats.Shooting + Stats.Defending + Stats.Stamina) / 5;
                }
            }
        }
    }
}
