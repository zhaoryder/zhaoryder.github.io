using System.Collections.Generic;

namespace FC26.Data
{
    /// <summary>
    /// 联赛数据。
    /// League 枚举定义于 TeamData.cs：
    ///   CSL = 中超（中国足球协会超级联赛，Chinese Super League）
    ///   EPL = 英超（英格兰足球超级联赛，English Premier League）
    /// 本类为静态工具类，提供按联赛获取球队列表与联赛中文名的接口，
    /// 球队数据源统一来自 PlayerDatabase。
    /// </summary>
    public static class LeagueData
    {
        /// <summary>联赛中文名映射表</summary>
        public static readonly Dictionary<League, string> LeagueNames = new Dictionary<League, string>
        {
            { League.CSL, "中国足球协会超级联赛" },
            { League.EPL, "英格兰足球超级联赛" }
        };

        /// <summary>
        /// 获取指定联赛下的所有球队。
        /// 数据来自 PlayerDatabase.GetTeamsByLeague。
        /// </summary>
        public static List<TeamData> GetTeams(League league)
        {
            return PlayerDatabase.GetTeamsByLeague(league);
        }

        /// <summary>
        /// 获取联赛的中文名。
        /// 若未找到映射，返回枚举名字符串。
        /// </summary>
        public static string GetLeagueName(League league)
        {
            return LeagueNames.TryGetValue(league, out var name) ? name : league.ToString();
        }
    }
}
