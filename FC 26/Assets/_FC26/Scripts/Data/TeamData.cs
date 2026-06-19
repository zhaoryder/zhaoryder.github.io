using UnityEngine;

namespace FC26.Data
{
    /// <summary>
    /// 联赛枚举。
    /// </summary>
    public enum League
    {
        CSL, // 中超 Chinese Super League
        EPL  // 英超 English Premier League
    }

    /// <summary>
    /// 阵型枚举。坐标定义见 Formations.cs。
    /// </summary>
    public enum FormationType
    {
        F442,  // 4-4-2
        F433,  // 4-3-3
        F4231  // 4-2-3-1
    }

    /// <summary>
    /// 球队数据。继承 ScriptableObject 以支持资源化，
    /// 同时保留默认构造以便 PlayerDatabase 在运行时用 new 实例化。
    /// 包含队名、联赛、球衣颜色、阵型、首发 11 人、替补 7 人与战术风格。
    /// </summary>
    [CreateAssetMenu(fileName = "TeamData", menuName = "FC26/TeamData", order = 2)]
    public class TeamData : ScriptableObject
    {
        [Header("基础")]
        [Tooltip("球队名称")] public string TeamName;
        [Tooltip("所属联赛")] public League League;
        [Tooltip("球队 ID（唯一标识，与 PlayerData.TeamID 对应）")] public string TeamID;

        [Header("球衣颜色")]
        [Tooltip("主球衣颜色")] public Color HomeColor;
        [Tooltip("客球衣颜色")] public Color AwayColor;
        [Tooltip("门将球衣颜色")] public Color GKColor;

        [Header("阵型与战术")]
        [Tooltip("默认阵型")] public FormationType Formation;
        [Tooltip("默认战术风格")] public TacticStyle Tactic;

        [Header("阵容")]
        [Tooltip("首发 11 人，顺序需与 Formations 坐标顺序一致")] public PlayerData[] Starters;
        [Tooltip("替补 7 人")] public PlayerData[] Substitutes;
    }
}
