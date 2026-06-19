//=============================================================================
// 文件名：MatchStatistics.cs
// 所属模块：Match
// 命名空间：FC26.Match
// 作用：比赛统计数据。记录两队控球率、射门、射正、角球、犯规、黄红牌、传球、抢断。
//       由 MatchManager 持有实例，各模块通过 MatchManager.Statistics 访问。
// 备注：本类为普通 C# 类（非 MonoBehaviour），由 MatchManager 创建并驱动。
//       控球率通过 UpdatePossession 累计控球时间计算。
//=============================================================================
using System;

namespace FC26.Match
{
    /// <summary>
    /// 单队比赛统计数据结构体。记录该队在一场比赛中的各项统计。
    /// </summary>
    [Serializable]
    public struct TeamMatchStats
    {
        /// <summary>控球时间（秒），用于计算控球率</summary>
        public float PossessionTime;

        /// <summary>射门数</summary>
        public int Shots;

        /// <summary>射正数</summary>
        public int ShotsOnTarget;

        /// <summary>角球数</summary>
        public int Corners;

        /// <summary>犯规数</summary>
        public int Fouls;

        /// <summary>黄牌数</summary>
        public int YellowCards;

        /// <summary>红牌数</summary>
        public int RedCards;

        /// <summary>传球数</summary>
        public int Passes;

        /// <summary>抢断数</summary>
        public int Tackles;
    }

    /// <summary>
    /// 比赛统计快照结构体。包含两队统计数据，供 UI 与赛后面板读取。
    /// </summary>
    [Serializable]
    public struct MatchStatsSnapshot
    {
        /// <summary>主队统计（TeamId=0）</summary>
        public TeamMatchStats Home;

        /// <summary>客队统计（TeamId=1）</summary>
        public TeamMatchStats Away;

        /// <summary>主队控球率（0~1）</summary>
        public float HomePossessionRate;

        /// <summary>客队控球率（0~1）</summary>
        public float AwayPossessionRate;
    }

    /// <summary>
    /// 比赛统计类：记录两队比赛数据，提供记录方法与快照查询。
    /// </summary>
    public class MatchStatistics
    {
        // 主队统计（TeamId=0）
        private TeamMatchStats _homeStats;

        // 客队统计（TeamId=1）
        private TeamMatchStats _awayStats;

        /// <summary>
        /// 构造函数：初始化统计数据为零。
        /// </summary>
        public MatchStatistics()
        {
            _homeStats = new TeamMatchStats();
            _awayStats = new TeamMatchStats();
        }

        /// <summary>
        /// 重置所有统计数据为零。
        /// </summary>
        public void Reset()
        {
            _homeStats = new TeamMatchStats();
            _awayStats = new TeamMatchStats();
        }

        /// <summary>
        /// 记录一次射门。
        /// </summary>
        /// <param name="teamId">队伍 ID（0=主队，1=客队）</param>
        /// <param name="onTarget">是否射正</param>
        public void RecordShot(int teamId, bool onTarget)
        {
            if (teamId == 0)
            {
                _homeStats.Shots++;
                if (onTarget) _homeStats.ShotsOnTarget++;
            }
            else
            {
                _awayStats.Shots++;
                if (onTarget) _awayStats.ShotsOnTarget++;
            }
        }

        /// <summary>
        /// 记录一次角球。
        /// </summary>
        /// <param name="teamId">队伍 ID</param>
        public void RecordCorner(int teamId)
        {
            if (teamId == 0)
            {
                _homeStats.Corners++;
            }
            else
            {
                _awayStats.Corners++;
            }
        }

        /// <summary>
        /// 记录一次犯规。
        /// </summary>
        /// <param name="teamId">队伍 ID</param>
        public void RecordFoul(int teamId)
        {
            if (teamId == 0)
            {
                _homeStats.Fouls++;
            }
            else
            {
                _awayStats.Fouls++;
            }
        }

        /// <summary>
        /// 记录一张牌。
        /// </summary>
        /// <param name="teamId">队伍 ID</param>
        /// <param name="isRed">是否红牌（false=黄牌）</param>
        public void RecordCard(int teamId, bool isRed)
        {
            if (teamId == 0)
            {
                if (isRed) _homeStats.RedCards++;
                else _homeStats.YellowCards++;
            }
            else
            {
                if (isRed) _awayStats.RedCards++;
                else _awayStats.YellowCards++;
            }
        }

        /// <summary>
        /// 更新控球时间。由 MatchManager 在 Update 中调用，
        /// 传入当前控球队伍 ID 与本帧时间增量。
        /// </summary>
        /// <param name="teamId">当前控球队伍 ID（-1=无人控球）</param>
        /// <param name="deltaTime">时间增量（秒）</param>
        public void UpdatePossession(int teamId, float deltaTime)
        {
            if (teamId == 0)
            {
                _homeStats.PossessionTime += deltaTime;
            }
            else if (teamId == 1)
            {
                _awayStats.PossessionTime += deltaTime;
            }
            // teamId == -1 时不计入任何一方
        }

        /// <summary>
        /// 记录一次传球。
        /// </summary>
        /// <param name="teamId">队伍 ID</param>
        public void RecordPass(int teamId)
        {
            if (teamId == 0)
            {
                _homeStats.Passes++;
            }
            else
            {
                _awayStats.Passes++;
            }
        }

        /// <summary>
        /// 记录一次抢断。
        /// </summary>
        /// <param name="teamId">队伍 ID</param>
        public void RecordTackle(int teamId)
        {
            if (teamId == 0)
            {
                _homeStats.Tackles++;
            }
            else
            {
                _awayStats.Tackles++;
            }
        }

        /// <summary>
        /// 获取当前统计快照。
        /// 控球率 = 该队控球时间 / 两队总控球时间。若总控球时间为零，则各 0.5。
        /// </summary>
        /// <returns>统计快照结构体</returns>
        public MatchStatsSnapshot GetStats()
        {
            float totalPossession = _homeStats.PossessionTime + _awayStats.PossessionTime;
            float homeRate = 0.5f;
            float awayRate = 0.5f;

            if (totalPossession > 0.0001f)
            {
                homeRate = _homeStats.PossessionTime / totalPossession;
                awayRate = _awayStats.PossessionTime / totalPossession;
            }

            return new MatchStatsSnapshot
            {
                Home = _homeStats,
                Away = _awayStats,
                HomePossessionRate = homeRate,
                AwayPossessionRate = awayRate
            };
        }
    }
}
