using UnityEngine;

namespace FC26.Data
{
    /// <summary>
    /// PlayerDatabase 英超联赛分部（Part2）。
    /// 本文件实现 <c>InitEPLLeague_Part2()</c>，负责创建英超 2026-27 赛季后 10 支球队
    /// （TeamId 111 ~ 120）的完整大名单（每队 18 人：首发 11 + 替补 7），
    /// 并通过 CreateTeam 注册到 <c>_teams</c> 与 <c>_allTeams</c>。
    ///
    /// 辅助方法（由主文件 PlayerDatabase.cs 提供，本文件直接调用）：
    ///   - CreateTeam(...)      创建球队并返回 TeamData（内部已注册到 _teams / _allTeams）
    ///   - CreatePlayer(...)    创建球员并返回 PlayerData
    ///   - SetLineup(...)       设置首发与替补
    ///
    /// 大名单摘要（TeamId 111-120）：
    /// ┌─────┬──────────────┬────────┬──────────────────┬──────────────────────────────────────────┐
    /// │ ID  │ 球队         │ 阵型   │ 战术             │ 核心球员                                 │
    /// ├─────┼──────────────┼────────┼──────────────────┼──────────────────────────────────────────┤
    /// │ 111 │ 布伦特福德   │ 4-2-3-1│ HighPress        │ 姆贝乌莫(82)、维萨(79)、平诺克(78)、弗莱肯(78)│
    /// │ 112 │ 水晶宫       │ 4-3-3  │ Counter          │ 埃泽(83)、马特塔(80)、格伊(81)、亨德森(79)│
    /// │ 113 │ 富勒姆       │ 4-4-2  │ Possession       │ 伊沃比(81)、帕利尼亚(82)、莱诺(79)、希门尼斯(78)│
    /// │ 114 │ 埃弗顿       │ 4-4-2  │ DefensiveCounter │ 勒温(79)、皮克福德(82)、盖耶(78)、塔尔科夫斯基(78)│
    /// │ 115 │ 诺丁汉森林   │ 4-2-3-1│ Counter          │ 吉布斯-怀特(82)、伍德(78)、穆里略(80)、塞尔斯(77)│
    /// │ 116 │ 狼队         │ 4-3-3  │ Counter          │ 库尼亚(82)、黄喜灿(80)、勒米纳(78)、塞梅多(77)│
    /// │ 117 │ 伯恩茅斯     │ 4-3-3  │ HighPress        │ 克鲁伊维特(79)、塔维尼尔(78)、内托(78)、塞梅尼奥(77)│
    /// │ 118 │ 利兹联       │ 4-3-3  │ HighPress        │ 萨默维尔(80)、尼奥托(78)、鲁特尔(77)、梅斯利耶(77)│
    /// │ 119 │ 桑德兰       │ 4-2-3-1│ Counter          │ 克拉克(76)、罗伯茨(75)、巴拉德(74)、帕特森(73)│
    /// │ 120 │ 赫尔城       │ 4-4-2  │ DefensiveCounter │ 菲洛根(75)、特拉奥雷(74)、庞德(73)、潘托里米(72)│
    /// └─────┴──────────────┴────────┴──────────────────┴──────────────────────────────────────────┘
    ///
    /// 能力值定位：中游主力 75-83，保级队主力 72-78，升班马主力 70-76，替补 68-75。
    /// 每队 18 人配置：1 GK + 7 DF + 6 MF + 4 FW。
    /// CreatePlayer 参数顺序：name, number, pos, speed, passing, shooting, defense, stamina, teamId。
    /// 首发顺序严格对应 Formations.cs 中各阵型坐标顺序。
    ///
    /// 2026-27 赛季动态：
    ///   - 利兹联、桑德兰、赫尔城为本赛季三支升班马
    ///   - 赫尔城夺冠赔率 3001.0，为最不被看好的球队
    ///   - 诺丁汉森林中场安德森曾获曼城 1.2 亿英镑报价（未成交）
    /// </summary>
    public partial class PlayerDatabase
    {
        /// <summary>
        /// 初始化英超联赛后 10 队（TeamId 111-120）。
        /// 由主文件 PlayerDatabase 静态构造函数通过 partial 调用链触发。
        /// </summary>
        static partial void InitEPLLeague_Part2()
        {
            InitBrentford();          // 111 布伦特福德
            InitCrystalPalace();      // 112 水晶宫
            InitFulham();             // 113 富勒姆
            InitEverton();            // 114 埃弗顿
            InitNottinghamForest();   // 115 诺丁汉森林
            InitWolves();             // 116 狼队
            InitBournemouth();        // 117 伯恩茅斯
            InitLeedsUnited();        // 118 利兹联（升班马）
            InitSunderland();         // 119 桑德兰（升班马）
            InitHullCity();           // 120 赫尔城（升班马）
        }

        // ====================================================================================
        // 111. 布伦特福德（TeamId=111，4-2-3-1，高位压迫，红白条纹/蓝/橙）
        //     核心：姆贝乌莫(82)、维萨(79)、平诺克(78)、弗莱肯(78)
        //     注：红白条纹主场衣以红色近似表示
        // ====================================================================================
        private static void InitBrentford()
        {
            // 创建球队：红白条纹主场衣（红色近似）/ 蓝色客场衣 / 橙色门将衣
            TeamData team = CreateTeam(111, "布伦特福德", League.EPL,
                new Color(0.85f, 0.10f, 0.10f),  // 主场：红白条纹（以红近似）
                new Color(0.10f, 0.30f, 0.85f),  // 客场：蓝
                new Color(0.95f, 0.50f, 0.10f),  // 门将：橙
                FormationType.F4231, TacticStyle.HighPress);

            // ---------- 首发 11 人（4-2-3-1 顺序：GK, LB, LCB, RCB, RB, LDM, RDM, LAM, CAM, RAM, ST）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("弗莱肯",     1, PlayerPosition.GK, 60, 68, 25, 80, 76, 111), // GK  主力门将
                CreatePlayer("里科·亨利",  3, PlayerPosition.DF, 80, 74, 45, 78, 76, 111), // LB  左后卫
                CreatePlayer("平诺克",     5, PlayerPosition.DF, 72, 72, 42, 80, 78, 111), // LCB 左中卫（核心）
                CreatePlayer("米",        16, PlayerPosition.DF, 74, 72, 42, 78, 76, 111), // RCB 右中卫
                CreatePlayer("阿耶",       2, PlayerPosition.DF, 80, 74, 42, 76, 76, 111), // RB  右后卫
                CreatePlayer("诺尔高",     6, PlayerPosition.MF, 72, 78, 70, 78, 78, 111), // LDM 左后腰
                CreatePlayer("詹尼特",    27, PlayerPosition.MF, 74, 76, 68, 76, 76, 111), // RDM 右后腰
                CreatePlayer("姆贝乌莫",  19, PlayerPosition.MF, 86, 80, 82, 55, 80, 111), // LAM 左前腰（核心）
                CreatePlayer("达姆斯高",   7, PlayerPosition.MF, 78, 80, 76, 68, 76, 111), // CAM 中前腰
                CreatePlayer("维萨",      11, PlayerPosition.MF, 84, 78, 78, 55, 78, 111), // RAM 右前腰（核心）
                CreatePlayer("沙德",      17, PlayerPosition.FW, 80, 74, 76, 50, 76, 111)  // ST  单前锋
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("斯特拉科沙", 13, PlayerPosition.GK, 58, 65, 25, 75, 72, 111), // GK  替补门将
                CreatePlayer("本杰明",     4, PlayerPosition.DF, 72, 70, 40, 75, 72, 111), // DF  替补中卫
                CreatePlayer("希基",      15, PlayerPosition.DF, 78, 72, 42, 76, 74, 111), // DF  替补边卫
                CreatePlayer("延森",       8, PlayerPosition.MF, 74, 78, 72, 72, 74, 111), // MF  替补中场
                CreatePlayer("奥涅卡",    28, PlayerPosition.MF, 72, 74, 68, 74, 72, 111), // MF  替补后腰
                CreatePlayer("托尼",      22, PlayerPosition.FW, 78, 74, 78, 50, 76, 111), // FW  替补前锋（伊万·托尼）
                CreatePlayer("刘易斯·波特",26, PlayerPosition.FW, 78, 72, 72, 48, 72, 111)  // FW  替补边锋
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 112. 水晶宫（TeamId=112，4-3-3，反击，红蓝条纹/白/绿）
        //     核心：埃泽(83)、马特塔(80)、格伊(81)、亨德森(79)
        //     注：红蓝条纹主场衣以红色近似表示
        // ====================================================================================
        private static void InitCrystalPalace()
        {
            // 创建球队：红蓝条纹主场衣（红色近似）/ 白色客场衣 / 绿色门将衣
            TeamData team = CreateTeam(112, "水晶宫", League.EPL,
                new Color(0.85f, 0.10f, 0.20f),  // 主场：红蓝条纹（以红近似）
                new Color(0.95f, 0.95f, 0.95f),  // 客场：白
                new Color(0.10f, 0.70f, 0.20f),  // 门将：绿
                FormationType.F433, TacticStyle.Counter);

            // ---------- 首发 11 人（4-3-3 顺序：GK, LB, LCB, RCB, RB, LCM, CM, RCM, LW, ST, RW）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("亨德森",     1, PlayerPosition.GK, 60, 70, 25, 81, 78, 112), // GK  主力门将（核心）
                CreatePlayer("米切尔",     3, PlayerPosition.DF, 80, 74, 45, 78, 76, 112), // LB  左后卫
                CreatePlayer("格伊",       6, PlayerPosition.DF, 74, 74, 42, 83, 80, 112), // LCB 左中卫（核心）
                CreatePlayer("安德森",    16, PlayerPosition.DF, 74, 72, 42, 80, 78, 112), // RCB 右中卫
                CreatePlayer("克莱因",     2, PlayerPosition.DF, 78, 72, 42, 75, 74, 112), // RB  右后卫
                CreatePlayer("沃顿",      26, PlayerPosition.MF, 74, 80, 72, 78, 78, 112), // LCM 左中前卫
                CreatePlayer("杜库雷",     8, PlayerPosition.MF, 74, 78, 72, 77, 76, 112), // CM  中前卫
                CreatePlayer("埃泽",      10, PlayerPosition.MF, 80, 85, 82, 70, 80, 112), // RCM 右中前卫（核心组织）
                CreatePlayer("萨尔",       7, PlayerPosition.FW, 84, 76, 76, 50, 76, 112), // LW  左边锋
                CreatePlayer("马特塔",    14, PlayerPosition.FW, 80, 76, 80, 50, 78, 112), // ST  中锋（核心）
                CreatePlayer("弗兰卡",    11, PlayerPosition.FW, 82, 74, 74, 48, 74, 112)  // RW  右边锋
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("特纳",      30, PlayerPosition.GK, 58, 65, 25, 75, 72, 112), // GK  替补门将
                CreatePlayer("沃德",      17, PlayerPosition.DF, 74, 70, 40, 74, 72, 112), // DF  替补后卫
                CreatePlayer("理查兹",     5, PlayerPosition.DF, 74, 72, 42, 76, 74, 112), // DF  替补中卫
                CreatePlayer("休斯",      19, PlayerPosition.MF, 72, 76, 70, 72, 72, 112), // MF  替补中场
                CreatePlayer("莱尔马",    28, PlayerPosition.MF, 74, 78, 72, 76, 76, 112), // MF  替补中场
                CreatePlayer("爱德华",     9, PlayerPosition.FW, 78, 74, 75, 48, 74, 112), // FW  替补前锋
                CreatePlayer("阿尤",      20, PlayerPosition.FW, 78, 72, 74, 48, 74, 112)  // FW  替补边锋
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 113. 富勒姆（TeamId=113，4-4-2，控球，白/黑/橙）
        //     核心：伊沃比(81)、帕利尼亚(82)、莱诺(79)、希门尼斯(78)
        // ====================================================================================
        private static void InitFulham()
        {
            // 创建球队：白色主场衣 / 黑色客场衣 / 橙色门将衣
            TeamData team = CreateTeam(113, "富勒姆", League.EPL,
                new Color(0.95f, 0.95f, 0.95f),  // 主场：白
                new Color(0.10f, 0.10f, 0.10f),  // 客场：黑
                new Color(0.95f, 0.50f, 0.10f),  // 门将：橙
                FormationType.F442, TacticStyle.Possession);

            // ---------- 首发 11 人（4-4-2 顺序：GK, LB, LCB, RCB, RB, LM, LCM, RCM, RM, LST, RST）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("莱诺",       1, PlayerPosition.GK, 60, 70, 25, 81, 78, 113), // GK  主力门将（核心）
                CreatePlayer("罗宾逊",    33, PlayerPosition.DF, 80, 76, 45, 78, 78, 113), // LB  左后卫
                CreatePlayer("巴锡",       3, PlayerPosition.DF, 74, 72, 42, 80, 78, 113), // LCB 左中卫
                CreatePlayer("安德森",     4, PlayerPosition.DF, 74, 72, 42, 79, 76, 113), // RCB 右中卫
                CreatePlayer("卡斯塔涅",   2, PlayerPosition.DF, 80, 74, 42, 76, 76, 113), // RB  右后卫
                CreatePlayer("伊沃比",    17, PlayerPosition.MF, 84, 80, 80, 55, 80, 113), // LM  左前卫（核心）
                CreatePlayer("帕利尼亚",  26, PlayerPosition.MF, 72, 84, 72, 84, 82, 113), // LCM 左中前卫（核心后腰）
                CreatePlayer("里德",       6, PlayerPosition.MF, 74, 78, 72, 76, 76, 113), // RCM 右中前卫
                CreatePlayer("威廉",      20, PlayerPosition.MF, 82, 78, 76, 55, 76, 113), // RM  右前卫
                CreatePlayer("希门尼斯",   9, PlayerPosition.FW, 76, 74, 78, 50, 76, 113), // LST 左前锋（核心）
                CreatePlayer("穆尼斯",    19, PlayerPosition.FW, 80, 74, 75, 48, 74, 113)  // RST 右前锋
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("本达",      12, PlayerPosition.GK, 58, 64, 25, 74, 70, 113), // GK  替补门将
                CreatePlayer("泰特",      21, PlayerPosition.DF, 78, 72, 42, 74, 72, 113), // DF  替补边卫
                CreatePlayer("迪奥普",     5, PlayerPosition.DF, 72, 70, 40, 76, 74, 113), // DF  替补中卫
                CreatePlayer("卢基奇",     8, PlayerPosition.MF, 74, 76, 70, 74, 74, 113), // MF  替补中场
                CreatePlayer("佩雷拉",    18, PlayerPosition.MF, 74, 78, 74, 68, 74, 113), // MF  替补前腰
                CreatePlayer("特劳雷",    14, PlayerPosition.FW, 86, 70, 72, 45, 74, 113), // FW  替补边锋（速度型）
                CreatePlayer("维尼修斯",  30, PlayerPosition.FW, 76, 70, 72, 48, 72, 113)  // FW  替补前锋
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 114. 埃弗顿（TeamId=114，4-4-2，防守反击，蓝/白/黄）
        //     核心：勒温(79)、皮克福德(82)、盖耶(78)、塔尔科夫斯基(78)
        // ====================================================================================
        private static void InitEverton()
        {
            // 创建球队：蓝色主场衣 / 白色客场衣 / 黄色门将衣
            TeamData team = CreateTeam(114, "埃弗顿", League.EPL,
                new Color(0.10f, 0.30f, 0.85f),  // 主场：蓝
                new Color(0.95f, 0.95f, 0.95f),  // 客场：白
                new Color(0.95f, 0.85f, 0.10f),  // 门将：黄
                FormationType.F442, TacticStyle.DefensiveCounter);

            // ---------- 首发 11 人（4-4-2 顺序：GK, LB, LCB, RCB, RB, LM, LCM, RCM, RM, LST, RST）----------
            // 注：盖耶位置归类为 DF（防守型中场），实际司职 LCM
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("皮克福德",     1, PlayerPosition.GK, 60, 72, 25, 84, 80, 114), // GK  主力门将（核心，英格兰国门）
                CreatePlayer("米科连科",    19, PlayerPosition.DF, 78, 74, 45, 78, 76, 114), // LB  左后卫
                CreatePlayer("塔尔科夫斯基", 5, PlayerPosition.DF, 70, 72, 42, 80, 78, 114), // LCB 左中卫（核心）
                CreatePlayer("布兰斯韦特",   6, PlayerPosition.DF, 76, 72, 42, 79, 78, 114), // RCB 右中卫
                CreatePlayer("科尔曼",       2, PlayerPosition.DF, 76, 72, 42, 75, 74, 114), // RB  右后卫（老将队长）
                CreatePlayer("哈里森",      11, PlayerPosition.MF, 82, 76, 76, 50, 76, 114), // LM  左前卫
                CreatePlayer("盖耶",        27, PlayerPosition.DF, 74, 78, 70, 80, 78, 114), // LCM 左中前卫（防守型中场，核心）
                CreatePlayer("奥纳纳",       8, PlayerPosition.MF, 76, 78, 72, 78, 78, 114), // RCM 右中前卫
                CreatePlayer("麦克尼尔",     7, PlayerPosition.MF, 80, 76, 74, 52, 76, 114), // RM  右前卫
                CreatePlayer("勒温",         9, PlayerPosition.FW, 80, 74, 79, 50, 78, 114), // LST 左前锋（核心）
                CreatePlayer("贝托",        19, PlayerPosition.FW, 78, 72, 75, 48, 74, 114)  // RST 右前锋
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("弗吉尼亚",  12, PlayerPosition.GK, 58, 64, 25, 74, 70, 114), // GK  替补门将
                CreatePlayer("戈弗雷",     4, PlayerPosition.DF, 76, 70, 40, 76, 74, 114), // DF  替补后卫
                CreatePlayer("基恩",       5, PlayerPosition.DF, 70, 70, 40, 76, 74, 114), // DF  替补中卫
                CreatePlayer("杜库雷",    16, PlayerPosition.MF, 74, 76, 72, 74, 74, 114), // MF  替补中场
                CreatePlayer("加纳",      26, PlayerPosition.MF, 74, 74, 68, 72, 72, 114), // MF  替补中场（青训）
                CreatePlayer("切尔米蒂",  14, PlayerPosition.FW, 76, 70, 72, 45, 72, 114), // FW  替补前锋
                CreatePlayer("麦克尼尔",   7, PlayerPosition.FW, 80, 76, 74, 52, 76, 114)  // FW  替补边锋
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 115. 诺丁汉森林（TeamId=115，4-2-3-1，反击，红/白/绿）
        //     核心：吉布斯-怀特(82)、伍德(78)、穆里略(80)、塞尔斯(77)
        //     注：安德森曾获曼城 1.2 亿英镑报价（未成交）
        // ====================================================================================
        private static void InitNottinghamForest()
        {
            // 创建球队：红色主场衣 / 白色客场衣 / 绿色门将衣
            TeamData team = CreateTeam(115, "诺丁汉森林", League.EPL,
                new Color(0.85f, 0.10f, 0.10f),  // 主场：红
                new Color(0.95f, 0.95f, 0.95f),  // 客场：白
                new Color(0.10f, 0.70f, 0.20f),  // 门将：绿
                FormationType.F4231, TacticStyle.Counter);

            // ---------- 首发 11 人（4-2-3-1 顺序：GK, LB, LCB, RCB, RB, LDM, RDM, LAM, CAM, RAM, ST）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("塞尔斯",          1, PlayerPosition.GK, 58, 68, 25, 79, 76, 115), // GK  主力门将
                CreatePlayer("奥里耶",          7, PlayerPosition.DF, 78, 74, 45, 76, 74, 115), // LB  左后卫
                CreatePlayer("穆里略",          3, PlayerPosition.DF, 76, 74, 42, 82, 80, 115), // LCB 左中卫（核心）
                CreatePlayer("米伦科维奇",      4, PlayerPosition.DF, 72, 72, 42, 79, 78, 115), // RCB 右中卫
                CreatePlayer("威廉姆斯",        2, PlayerPosition.DF, 78, 74, 42, 76, 74, 115), // RB  右后卫
                CreatePlayer("桑加雷",          6, PlayerPosition.MF, 72, 76, 70, 78, 78, 115), // LDM 左后腰
                CreatePlayer("安德森",          8, PlayerPosition.MF, 74, 78, 72, 77, 76, 115), // RDM 右后腰（曼城报价目标）
                CreatePlayer("吉布斯-怀特",    10, PlayerPosition.MF, 80, 84, 80, 68, 80, 115), // LAM 左前腰（核心）
                CreatePlayer("多明格斯",       16, PlayerPosition.MF, 76, 78, 74, 68, 74, 115), // CAM 中前腰
                CreatePlayer("哈德森-奥多伊",  11, PlayerPosition.FW, 82, 78, 76, 55, 76, 115), // RAM 右前腰
                CreatePlayer("伍德",            9, PlayerPosition.FW, 70, 72, 78, 50, 76, 115)  // ST  单前锋（核心，新西兰高塔）
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("特纳",      30, PlayerPosition.GK, 58, 65, 25, 75, 72, 115), // GK  替补门将
                CreatePlayer("沃罗尔",     5, PlayerPosition.DF, 72, 70, 40, 75, 72, 115), // DF  替补中卫
                CreatePlayer("艾纳",      43, PlayerPosition.DF, 76, 70, 40, 74, 72, 115), // DF  替补边卫
                CreatePlayer("达尼洛",    28, PlayerPosition.MF, 74, 78, 72, 74, 74, 115), // MF  替补中场
                CreatePlayer("阿沃尼伊",  22, PlayerPosition.FW, 80, 74, 76, 50, 76, 115), // FW  替补前锋
                CreatePlayer("埃兰加",    21, PlayerPosition.FW, 86, 76, 76, 50, 76, 115), // FW  替补边锋（速度型）
                CreatePlayer("莱比",      17, PlayerPosition.FW, 76, 70, 71, 45, 70, 115)  // FW  替补前锋
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 116. 狼队（TeamId=116，4-3-3，反击，橙/黑/黄）
        //     核心：库尼亚(82)、黄喜灿(80)、勒米纳(78)、塞梅多(77)
        // ====================================================================================
        private static void InitWolves()
        {
            // 创建球队：橙色主场衣 / 黑色客场衣 / 黄色门将衣
            TeamData team = CreateTeam(116, "狼队", League.EPL,
                new Color(0.95f, 0.50f, 0.10f),  // 主场：橙
                new Color(0.10f, 0.10f, 0.10f),  // 客场：黑
                new Color(0.95f, 0.85f, 0.10f),  // 门将：黄
                FormationType.F433, TacticStyle.Counter);

            // ---------- 首发 11 人（4-3-3 顺序：GK, LB, LCB, RCB, RB, LCM, CM, RCM, LW, ST, RW）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("若泽·萨",     1, PlayerPosition.GK, 60, 70, 25, 79, 76, 116), // GK  主力门将
                CreatePlayer("艾特-努里",   3, PlayerPosition.DF, 80, 76, 45, 78, 76, 116), // LB  左后卫
                CreatePlayer("基尔曼",      5, PlayerPosition.DF, 74, 72, 42, 80, 78, 116), // LCB 左中卫
                CreatePlayer("托迪",        4, PlayerPosition.DF, 74, 72, 42, 77, 76, 116), // RCB 右中卫
                CreatePlayer("塞梅多",      2, PlayerPosition.DF, 82, 76, 45, 78, 78, 116), // RB  右后卫（核心）
                CreatePlayer("勒米纳",      6, PlayerPosition.MF, 74, 80, 72, 80, 78, 116), // LCM 左中前卫（核心后腰）
                CreatePlayer("戈麦斯",      8, PlayerPosition.MF, 74, 78, 72, 76, 76, 116), // CM  中前卫
                CreatePlayer("特劳雷",     28, PlayerPosition.MF, 76, 78, 72, 74, 74, 116), // RCM 右中前卫
                CreatePlayer("库尼亚",     10, PlayerPosition.FW, 84, 82, 82, 55, 80, 116), // LW  左边锋（核心，巴西前锋）
                CreatePlayer("黄喜灿",     11, PlayerPosition.FW, 84, 78, 80, 50, 78, 116), // ST  中锋（核心，韩国球星）
                CreatePlayer("萨拉维亚",   21, PlayerPosition.FW, 80, 76, 74, 50, 74, 116)  // RW  右边锋
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("本特利",    12, PlayerPosition.GK, 58, 64, 25, 74, 70, 116), // GK  替补门将
                CreatePlayer("道森",      15, PlayerPosition.DF, 70, 70, 40, 76, 74, 116), // DF  替补中卫（老将）
                CreatePlayer("布埃诺",    22, PlayerPosition.DF, 76, 72, 40, 75, 72, 116), // DF  替补左后卫
                CreatePlayer("贝勒加德",  17, PlayerPosition.MF, 76, 74, 70, 70, 72, 116), // MF  替补中场
                CreatePlayer("安德烈",     8, PlayerPosition.MF, 72, 78, 70, 76, 74, 116), // MF  替补后腰
                CreatePlayer("内托",       7, PlayerPosition.FW, 86, 76, 76, 50, 76, 116), // FW  替补边锋（速度型）
                CreatePlayer("戈麦斯",    19, PlayerPosition.FW, 76, 72, 72, 48, 72, 116)  // FW  替补前锋
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 117. 伯恩茅斯（TeamId=117，4-3-3，高位压迫，红黑条纹/白/橙）
        //     核心：克鲁伊维特(79)、塔维尼尔(78)、内托(78)、塞梅尼奥(77)
        //     注：红黑条纹主场衣以红色近似表示
        // ====================================================================================
        private static void InitBournemouth()
        {
            // 创建球队：红黑条纹主场衣（红色近似）/ 白色客场衣 / 橙色门将衣
            TeamData team = CreateTeam(117, "伯恩茅斯", League.EPL,
                new Color(0.85f, 0.10f, 0.10f),  // 主场：红黑条纹（以红近似）
                new Color(0.95f, 0.95f, 0.95f),  // 客场：白
                new Color(0.95f, 0.50f, 0.10f),  // 门将：橙
                FormationType.F433, TacticStyle.HighPress);

            // ---------- 首发 11 人（4-3-3 顺序：GK, LB, LCB, RCB, RB, LCM, CM, RCM, LW, ST, RW）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("内托",       1, PlayerPosition.GK, 60, 70, 25, 80, 76, 117), // GK  主力门将（核心）
                CreatePlayer("科尔克兹",  15, PlayerPosition.DF, 80, 74, 45, 78, 76, 117), // LB  左后卫
                CreatePlayer("塞内西",     5, PlayerPosition.DF, 72, 72, 42, 78, 76, 117), // LCB 左中卫
                CreatePlayer("扎巴尔尼",   3, PlayerPosition.DF, 76, 74, 42, 79, 78, 117), // RCB 右中卫
                CreatePlayer("史密斯",     2, PlayerPosition.DF, 78, 74, 42, 76, 74, 117), // RB  右后卫
                CreatePlayer("比尔马",    10, PlayerPosition.MF, 74, 78, 72, 76, 76, 117), // LCM 左中前卫
                CreatePlayer("克里斯蒂",   8, PlayerPosition.MF, 74, 78, 72, 74, 76, 117), // CM  中前卫
                CreatePlayer("塔维尼尔",  19, PlayerPosition.MF, 78, 80, 76, 72, 76, 117), // RCM 右中前卫（核心）
                CreatePlayer("克鲁伊维特", 7, PlayerPosition.FW, 84, 80, 79, 55, 78, 117), // LW  左边锋（核心）
                CreatePlayer("塞梅尼奥",  24, PlayerPosition.FW, 80, 76, 77, 50, 76, 117), // ST  中锋（核心）
                CreatePlayer("乌纳尔",    26, PlayerPosition.FW, 78, 76, 76, 50, 76, 117)  // RW  右边锋
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("特拉弗斯",  12, PlayerPosition.GK, 58, 64, 25, 74, 70, 117), // GK  替补门将
                CreatePlayer("凯利",       5, PlayerPosition.DF, 72, 70, 40, 76, 74, 117), // DF  替补中卫
                CreatePlayer("阿劳霍",     4, PlayerPosition.DF, 74, 70, 40, 75, 72, 117), // DF  替补后卫
                CreatePlayer("罗斯韦尔",  28, PlayerPosition.MF, 74, 76, 70, 70, 72, 117), // MF  替补中场
                CreatePlayer("亚当斯",    14, PlayerPosition.MF, 72, 76, 70, 74, 72, 117), // MF  替补后腰
                CreatePlayer("瓦塔拉",    11, PlayerPosition.FW, 82, 74, 74, 48, 74, 117), // FW  替补边锋
                CreatePlayer("西尼斯特拉", 17, PlayerPosition.FW, 82, 76, 74, 50, 74, 117)  // FW  替补边锋
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 118. 利兹联（TeamId=118，4-3-3，高位压迫，白/蓝/绿）—— 升班马
        //     核心：萨默维尔(80)、尼奥托(78)、鲁特尔(77)、梅斯利耶(77)
        // ====================================================================================
        private static void InitLeedsUnited()
        {
            // 创建球队：白色主场衣 / 蓝色客场衣 / 绿色门将衣
            TeamData team = CreateTeam(118, "利兹联", League.EPL,
                new Color(0.95f, 0.95f, 0.95f),  // 主场：白
                new Color(0.10f, 0.30f, 0.85f),  // 客场：蓝
                new Color(0.10f, 0.70f, 0.20f),  // 门将：绿
                FormationType.F433, TacticStyle.HighPress);

            // ---------- 首发 11 人（4-3-3 顺序：GK, LB, LCB, RCB, RB, LCM, CM, RCM, LW, ST, RW）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("梅斯利耶",   1, PlayerPosition.GK, 60, 70, 25, 79, 76, 118), // GK  主力门将（核心）
                CreatePlayer("菲尔波",     3, PlayerPosition.DF, 78, 74, 45, 76, 74, 118), // LB  左后卫
                CreatePlayer("斯特鲁伊克", 4, PlayerPosition.DF, 74, 72, 42, 78, 76, 118), // LCB 左中卫
                CreatePlayer("库珀",       6, PlayerPosition.DF, 70, 70, 40, 76, 74, 118), // RCB 右中卫（队长）
                CreatePlayer("艾林",       2, PlayerPosition.DF, 78, 74, 42, 76, 74, 118), // RB  右后卫
                CreatePlayer("阿奇",      25, PlayerPosition.MF, 74, 76, 70, 74, 74, 118), // LCM 左中前卫
                CreatePlayer("阿隆森",     7, PlayerPosition.MF, 76, 78, 76, 70, 76, 118), // CM  中前卫
                CreatePlayer("格耶",       8, PlayerPosition.MF, 74, 78, 72, 74, 74, 118), // RCM 右中前卫
                CreatePlayer("萨默维尔",  10, PlayerPosition.FW, 86, 80, 80, 55, 78, 118), // LW  左边锋（核心）
                CreatePlayer("鲁特尔",    24, PlayerPosition.FW, 80, 76, 77, 50, 76, 118), // ST  中锋（核心）
                CreatePlayer("尼奥托",    11, PlayerPosition.FW, 88, 78, 78, 50, 76, 118)  // RW  右边锋（核心，速度型）
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("达洛",      12, PlayerPosition.GK, 58, 64, 25, 73, 70, 118), // GK  替补门将
                CreatePlayer("拜拉姆",    15, PlayerPosition.DF, 76, 70, 40, 74, 72, 118), // DF  替补边卫
                CreatePlayer("安帕杜",    26, PlayerPosition.DF, 74, 72, 42, 76, 74, 118), // DF  替补中卫
                CreatePlayer("罗卡",      14, PlayerPosition.MF, 72, 78, 70, 76, 74, 118), // MF  替补后腰
                CreatePlayer("格林伍德",  17, PlayerPosition.MF, 76, 76, 74, 68, 72, 118), // MF  替补前腰
                CreatePlayer("詹姆斯",    20, PlayerPosition.FW, 86, 74, 75, 48, 74, 118), // FW  替补边锋（速度型）
                CreatePlayer("皮罗",      19, PlayerPosition.FW, 74, 70, 72, 45, 72, 118)  // FW  替补前锋
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 119. 桑德兰（TeamId=119，4-2-3-1，反击，红白条纹/黑/黄）—— 升班马
        //     核心：克拉克(76)、罗伯茨(75)、巴拉德(74)、帕特森(73)
        //     注：红白条纹主场衣以红色近似表示
        // ====================================================================================
        private static void InitSunderland()
        {
            // 创建球队：红白条纹主场衣（红色近似）/ 黑色客场衣 / 黄色门将衣
            TeamData team = CreateTeam(119, "桑德兰", League.EPL,
                new Color(0.85f, 0.10f, 0.10f),  // 主场：红白条纹（以红近似）
                new Color(0.10f, 0.10f, 0.10f),  // 客场：黑
                new Color(0.95f, 0.85f, 0.10f),  // 门将：黄
                FormationType.F4231, TacticStyle.Counter);

            // ---------- 首发 11 人（4-2-3-1 顺序：GK, LB, LCB, RCB, RB, LDM, RDM, LAM, CAM, RAM, ST）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("帕特森",     1, PlayerPosition.GK, 58, 66, 25, 75, 72, 119), // GK  主力门将
                CreatePlayer("赫耶德",     3, PlayerPosition.DF, 76, 72, 42, 75, 72, 119), // LB  左后卫
                CreatePlayer("巴拉德",     5, PlayerPosition.DF, 72, 70, 40, 76, 74, 119), // LCB 左中卫（核心）
                CreatePlayer("奥尼安",     4, PlayerPosition.DF, 72, 70, 40, 75, 72, 119), // RCB 右中卫
                CreatePlayer("哈金斯",     2, PlayerPosition.DF, 76, 70, 40, 74, 72, 119), // RB  右后卫
                CreatePlayer("尼尔",       6, PlayerPosition.MF, 72, 74, 68, 74, 72, 119), // LDM 左后腰
                CreatePlayer("埃文斯",     8, PlayerPosition.MF, 72, 74, 68, 74, 72, 119), // RDM 右后腰
                CreatePlayer("克拉克",    10, PlayerPosition.MF, 80, 78, 76, 65, 74, 119), // LAM 左前腰（核心）
                CreatePlayer("里格",       7, PlayerPosition.MF, 74, 76, 72, 65, 72, 119), // CAM 中前腰（青训小将）
                CreatePlayer("罗伯茨",    11, PlayerPosition.FW, 80, 76, 75, 50, 74, 119), // RAM 右前腰（核心）
                CreatePlayer("伊西多尔",   9, PlayerPosition.FW, 78, 72, 74, 48, 72, 119)  // ST  单前锋
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("巴福德",    12, PlayerPosition.GK, 56, 62, 25, 72, 68, 119), // GK  替补门将
                CreatePlayer("阿莱塞",    15, PlayerPosition.DF, 74, 68, 38, 73, 70, 119), // DF  替补后卫
                CreatePlayer("塞里",      26, PlayerPosition.DF, 72, 68, 38, 73, 70, 119), // DF  替补中卫
                CreatePlayer("普里查德",  17, PlayerPosition.MF, 72, 74, 68, 68, 70, 119), // MF  替补前腰（老将）
                CreatePlayer("沃森",      14, PlayerPosition.MF, 72, 74, 68, 70, 70, 119), // MF  替补中场
                CreatePlayer("沃特莫",    19, PlayerPosition.FW, 76, 70, 71, 45, 70, 119), // FW  替补边锋
                CreatePlayer("蒙德",      22, PlayerPosition.FW, 74, 68, 70, 45, 68, 119)  // FW  替补前锋
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 120. 赫尔城（TeamId=120，4-4-2，防守反击，橙黑条纹/白/绿）—— 升班马
        //     夺冠赔率 3001.0，最不被看好
        //     核心：菲洛根(75)、特拉奥雷(74)、庞德(73)、潘托里米(72)
        //     注：橙黑条纹主场衣以橙色近似表示
        // ====================================================================================
        private static void InitHullCity()
        {
            // 创建球队：橙黑条纹主场衣（橙色近似）/ 白色客场衣 / 绿色门将衣
            TeamData team = CreateTeam(120, "赫尔城", League.EPL,
                new Color(0.95f, 0.50f, 0.10f),  // 主场：橙黑条纹（以橙近似）
                new Color(0.95f, 0.95f, 0.95f),  // 客场：白
                new Color(0.10f, 0.70f, 0.20f),  // 门将：绿
                FormationType.F442, TacticStyle.DefensiveCounter);

            // ---------- 首发 11 人（4-4-2 顺序：GK, LB, LCB, RCB, RB, LM, LCM, RCM, RM, LST, RST）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("潘托里米",   1, PlayerPosition.GK, 58, 64, 25, 74, 70, 120), // GK  主力门将
                CreatePlayer("琼斯",       3, PlayerPosition.DF, 74, 70, 40, 74, 70, 120), // LB  左后卫
                CreatePlayer("庞德",       5, PlayerPosition.DF, 70, 68, 38, 75, 72, 120), // LCB 左中卫（核心）
                CreatePlayer("麦克纳尔蒂", 4, PlayerPosition.DF, 70, 68, 38, 74, 70, 120), // RCB 右中卫
                CreatePlayer("科伊尔",     2, PlayerPosition.DF, 74, 70, 40, 74, 70, 120), // RB  右后卫
                CreatePlayer("斯莱特",     8, PlayerPosition.MF, 78, 74, 72, 50, 72, 120), // LM  左前卫
                CreatePlayer("塞里",       6, PlayerPosition.MF, 72, 76, 68, 74, 72, 120), // LCM 左中前卫
                CreatePlayer("特拉奥雷",  14, PlayerPosition.MF, 76, 76, 72, 74, 72, 120), // RCM 右中前卫（核心，外援）
                CreatePlayer("图安泽贝",   7, PlayerPosition.MF, 76, 72, 70, 50, 70, 120), // RM  右前卫
                CreatePlayer("菲洛根",    11, PlayerPosition.FW, 82, 74, 75, 48, 72, 120), // LST 左前锋（核心）
                CreatePlayer("朗曼",       9, PlayerPosition.FW, 76, 72, 73, 48, 72, 120)  // RST 右前锋
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("巴顿",      12, PlayerPosition.GK, 56, 62, 25, 72, 68, 120), // GK  替补门将
                CreatePlayer("格雷",      15, PlayerPosition.DF, 72, 68, 38, 72, 68, 120), // DF  替补后卫
                CreatePlayer("维尔马",    25, PlayerPosition.DF, 72, 68, 38, 72, 68, 120), // DF  替补中卫
                CreatePlayer("多尔蒂",    17, PlayerPosition.MF, 72, 74, 68, 70, 70, 120), // MF  替补中场
                CreatePlayer("克劳斯",    22, PlayerPosition.MF, 72, 74, 68, 70, 70, 120), // MF  替补中场
                CreatePlayer("杰登",      19, PlayerPosition.FW, 76, 70, 71, 45, 70, 120), // FW  替补前锋
                CreatePlayer("扎胡尔",    21, PlayerPosition.FW, 74, 68, 70, 45, 68, 120)  // FW  替补前锋
            };

            SetLineup(team, starting, subs);
        }
    }
}
