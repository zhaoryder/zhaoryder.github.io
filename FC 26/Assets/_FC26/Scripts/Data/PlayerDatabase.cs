using System.Collections.Generic;
using UnityEngine;

namespace FC26.Data
{
    /// <summary>
    /// 球员与球队数据库主入口（partial class）。
    /// <para>本文件为主分部，负责：</para>
    /// <list type="number">
    ///   <item>维护全局球队字典 _teams（TeamId -> TeamData）与列表 _allTeams；</item>
    ///   <item>在静态构造函数中调用各分文件的初始化方法（InitCSLeague_Part1 等）；</item>
    ///   <item>对外提供查询接口（GetAllTeams / GetTeam / GetTeamsByLeague 等）；</item>
    ///   <item>对内提供辅助工厂方法（CreateTeam / CreatePlayer / SetLineup）供各分文件调用。</item>
    /// </list>
    ///
    /// <para>TeamId 规则：</para>
    /// <list type="bullet">
    ///   <item>中超 CSL：1 - 16</item>
    ///   <item>英超 EPL：101 - 120</item>
    /// </list>
    ///
    /// <para>全部 36 队名单（由各分文件实现）：</para>
    /// <para>中超 CSL（16 队，id 1-16，2026 赛季最新阵容）：</para>
    /// <list type="bullet">
    ///   <item>1.上海海港  2.上海申花  3.山东泰山  4.北京国安</item>
    ///   <item>5.成都蓉城  6.武汉三镇  7.浙江队    8.天津津门虎</item>
    ///   <item>9.河南队    10.青岛海牛 11.深圳新鹏城 12.大连英博海发</item>
    ///   <item>13.青岛西海岸 14.辽宁铁人 15.重庆铜梁龙 16.云南玉昆</item>
    /// </list>
    /// <para>英超 EPL（20 队，id 101-120）：</para>
    /// <list type="bullet">
    ///   <item>101.曼城      102.阿森纳    103.利物浦    104.曼联      105.切尔西</item>
    ///   <item>106.热刺      107.纽卡斯尔  108.阿斯顿维拉 109.布莱顿    110.西汉姆</item>
    ///   <item>111.埃弗顿    112.富勒姆    113.水晶宫    114.狼队      115.伯恩茅斯</item>
    ///   <item>116.布伦特福德 117.诺丁汉森林 118.莱斯特城 119.伊普斯维奇 120.南安普顿</item>
    /// </list>
    /// </summary>
    public static partial class PlayerDatabase
    {
        /// <summary>球队字典：TeamId -> TeamData，便于按 ID 快速查找。</summary>
        private static Dictionary<int, TeamData> _teams;

        /// <summary>全部球队列表，保持注册顺序，便于遍历与随机抽取。</summary>
        private static List<TeamData> _allTeams;

        /// <summary>
        /// 静态构造函数：初始化集合并调用各分文件的初始化方法。
        /// <para>分文件（PlayerDatabase_CS_Part1 等）实现对应的 partial void 方法，
        /// 在此统一调度，完成全部 36 队数据填充。
        /// 未实现的 partial 方法会被编译器自动移除调用，不影响运行。</para>
        /// </summary>
        static PlayerDatabase()
        {
            _teams = new Dictionary<int, TeamData>();
            _allTeams = new List<TeamData>();

            // 中超 16 队（分两个分文件，各 8 队）
            InitCSLeague_Part1(); // 中超前 8 队（id 1-8）
            InitCSLeague_Part2(); // 中超后 8 队（id 9-16）

            // 英超 20 队（分两个分文件，各 10 队）
            InitEPLLeague_Part1(); // 英超前 10 队（id 101-110）
            InitEPLLeague_Part2(); // 英超后 10 队（id 111-120）
        }

        /// <summary>中超球队总数。</summary>
        public static int CSLTeamCount => 16;

        /// <summary>英超球队总数。</summary>
        public static int EPLTeamCount => 20;

        /// <summary>返回全部球队列表（按注册顺序）。</summary>
        public static List<TeamData> GetAllTeams()
        {
            return _allTeams;
        }

        /// <summary>按球队 ID 查找，未找到返回 null。</summary>
        public static TeamData GetTeam(int teamId)
        {
            return _teams.TryGetValue(teamId, out var team) ? team : null;
        }

        /// <summary>按球队名称查找（精确匹配），未找到返回 null。</summary>
        public static TeamData GetTeam(string name)
        {
            for (int i = 0; i < _allTeams.Count; i++)
            {
                if (_allTeams[i].TeamName == name)
                    return _allTeams[i];
            }
            return null;
        }

        /// <summary>返回指定联赛下的全部球队。</summary>
        public static List<TeamData> GetTeamsByLeague(League league)
        {
            var list = new List<TeamData>();
            for (int i = 0; i < _allTeams.Count; i++)
            {
                if (_allTeams[i].League == league)
                    list.Add(_allTeams[i]);
            }
            return list;
        }

        // ===== partial 初始化方法（各分文件实现）=====

        /// <summary>中超前 8 队初始化（id 1-8），由 PlayerDatabase_CS_Part1.cs 实现。</summary>
        static partial void InitCSLeague_Part1();

        /// <summary>中超后 8 队初始化（id 9-16），由 PlayerDatabase_CS_Part2.cs 实现。</summary>
        static partial void InitCSLeague_Part2();

        /// <summary>英超前 10 队初始化（id 101-110），由 PlayerDatabase_EPL_Part1.cs 实现。</summary>
        static partial void InitEPLLeague_Part1();

        /// <summary>英超后 10 队初始化（id 111-120），由 PlayerDatabase_EPL_Part2.cs 实现。</summary>
        static partial void InitEPLLeague_Part2();

        // ===== 辅助工厂方法（供各分文件调用）=====
        // 注：静态类不支持 protected 修饰符，故采用 private static；
        //     partial 分文件同属一个类，可直接访问这些私有方法。

        /// <summary>
        /// 创建球队并注册到全局字典与列表。
        /// <para>参数说明：</para>
        /// <param name="id">球队 ID（中超 1-16，英超 101-120）</param>
        /// <param name="name">球队名称</param>
        /// <param name="league">所属联赛</param>
        /// <param name="home">主球衣颜色</param>
        /// <param name="away">客球衣颜色</param>
        /// <param name="keeper">门将球衣颜色</param>
        /// <param name="formation">默认阵型</param>
        /// <param name="tactic">默认战术风格</param>
        /// </summary>
        private static TeamData CreateTeam(int id, string name, League league, Color home, Color away, Color keeper, FormationType formation, TacticStyle tactic)
        {
            var team = ScriptableObject.CreateInstance<TeamData>();
            team.TeamName = name;
            team.League = league;
            team.TeamID = id.ToString();
            team.HomeColor = home;
            team.AwayColor = away;
            team.GKColor = keeper;
            team.Formation = formation;
            team.Tactic = tactic;

            _teams[id] = team;
            _allTeams.Add(team);
            return team;
        }

        /// <summary>
        /// 创建球员。能力值五维范围 0-99。
        /// <para>参数顺序：速度、传球、射门、防守、体能。</para>
        /// <param name="name">球员姓名</param>
        /// <param name="number">球衣号码</param>
        /// <param name="pos">场上位置 GK/DF/MF/FW</param>
        /// <param name="speed">速度 0-99</param>
        /// <param name="passing">传球 0-99</param>
        /// <param name="shooting">射门 0-99</param>
        /// <param name="defense">防守 0-99（对应 Ability.Defending）</param>
        /// <param name="stamina">体能 0-99</param>
        /// <param name="teamId">所属球队 ID</param>
        /// </summary>
        private static PlayerData CreatePlayer(string name, int number, PlayerPosition pos, int speed, int passing, int shooting, int defense, int stamina, int teamId)
        {
            var p = ScriptableObject.CreateInstance<PlayerData>();
            p.Name = name;
            p.Number = number;
            p.Position = pos;
            p.TeamID = teamId.ToString();
            p.Stats = new Ability(speed, passing, shooting, defense, stamina);
            return p;
        }

        /// <summary>
        /// 设置球队首发与替补阵容。
        /// <para>首发 11 人顺序需与 Formations 坐标顺序一致：</para>
        /// <list type="bullet">
        ///   <item>433: GK, LB, LCB, RCB, RB, LCM, CM, RCM, LW, ST, RW</item>
        ///   <item>442: GK, LB, LCB, RCB, RB, LM, LCM, RCM, RM, LST, RST</item>
        ///   <item>4231: GK, LB, LCB, RCB, RB, LDM, RDM, LAM, CAM, RAM, ST</item>
        /// </list>
        /// </summary>
        private static void SetLineup(TeamData team, PlayerData[] starting, PlayerData[] subs)
        {
            team.Starters = starting;
            team.Substitutes = subs;
        }
    }
}
