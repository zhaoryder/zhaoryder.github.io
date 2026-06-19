using UnityEngine;

namespace FC26.Data
{
    /// <summary>
    /// PlayerDatabase 英超联赛分部（Part1）。
    /// 本文件实现 <c>InitEPLLeague_Part1()</c>，负责创建英超 2026-27 赛季前 10 支球队
    /// （TeamId 101 ~ 110）的完整大名单（每队 18 人：首发 11 + 替补 7），
    /// 并注册到 <c>_teams</c> 与 <c>_allTeams</c>。
    ///
    /// 辅助方法（由主文件 PlayerDatabase.cs 提供，本文件直接调用）：
    ///   - CreateTeam(...)      创建球队并返回 TeamData
    ///   - CreatePlayer(...)    创建球员并返回 PlayerData
    ///   - SetLineup(...)       设置首发与替补
    ///
    /// 大名单摘要（TeamId 101-110）：
    /// ┌─────┬──────────────┬────────┬──────────────┬──────────────────────────────────────────┐
    /// │ ID  │ 球队         │ 阵型   │ 战术         │ 核心球员                                 │
    /// ├─────┼──────────────┼────────┼──────────────┼──────────────────────────────────────────┤
    /// │ 101 │ 阿森纳       │ 4-2-3-1│ Possession   │ 萨卡(88)、厄德高(89)、赖斯(88)、吉奥克雷斯(87)│
    /// │ 102 │ 曼城         │ 4-3-3  │ Possession   │ 哈兰德(93)、德布劳内(91)、福登(89)、罗德里(90)│
    /// │ 103 │ 利物浦       │ 4-3-3  │ HighPress    │ 萨拉赫(91)、范迪克(89)、麦卡利斯特(87)     │
    /// │ 104 │ 曼联         │ 4-2-3-1│ Counter      │ B费(88)、卡塞米罗(85)、拉什福德(84)       │
    /// │ 105 │ 切尔西       │ 4-2-3-1│ Possession   │ 帕尔默(87)、凯塞多(85)、恩佐(84)         │
    /// │ 106 │ 热刺         │ 4-2-3-1│ HighPress    │ 孙兴慜(87)、麦迪逊(85)、罗梅罗(85)       │
    /// │ 107 │ 阿斯顿维拉   │ 4-2-3-1│ Counter      │ 沃特金斯(84)、道格拉斯·路易斯(83)、马丁内斯(84)│
    /// │ 108 │ 纽卡斯尔联   │ 4-3-3  │ HighPress    │ 伊萨克(86)、吉马良斯(85)、博特曼(83)     │
    /// │ 109 │ 布莱顿       │ 4-2-3-1│ Possession   │ 三笘薰(84)、弗格森(82)、邓克(80)         │
    /// │ 110 │ 西汉姆联     │ 4-4-2  │ Counter      │ 鲍恩(83)、帕奎塔(83)、库杜斯(82)         │
    /// └─────┴──────────────┴────────┴──────────────┴──────────────────────────────────────────┘
    ///
    /// 能力值定位：豪门球星 85-93，主力 75-85，替补 68-78。
    /// 每队 18 人配置：1 GK + 7 DF + 6 MF + 4 FW。
    /// CreatePlayer 参数顺序：name, number, pos, speed, passing, shooting, defense, stamina, teamId。
    /// 首发顺序严格对应 Formations.cs 中各阵型坐标顺序。
    ///
    /// 2026 夏窗动态：
    ///   - 曼城：1.2 亿英镑报价安德森（诺丁汉森林中场，未最终成交）
    ///   - 利物浦：签下 17 岁哥伦比亚新星马丁内斯
    ///   - 曼联：计划 3 亿英镑引援重建
    ///   - 切尔西：预签巴尔科（左后卫）+ 埃梅加（前锋）
    /// </summary>
    public static partial class PlayerDatabase
    {
        /// <summary>
        /// 初始化英超联赛前 10 队（TeamId 101-110）。
        /// 由主文件 PlayerDatabase.Init() 通过 partial 调用链触发。
        /// </summary>
        static partial void InitEPLLeague_Part1()
        {
            InitArsenal();          // 101 阿森纳
            InitManCity();          // 102 曼城
            InitLiverpool();        // 103 利物浦
            InitManUnited();        // 104 曼联
            InitChelsea();          // 105 切尔西
            InitTottenham();        // 106 热刺
            InitAstonVilla();       // 107 阿斯顿维拉
            InitNewcastle();        // 108 纽卡斯尔联
            InitBrighton();         // 109 布莱顿
            InitWestHam();          // 110 西汉姆联
        }

        // ====================================================================================
        // 101. 阿森纳（TeamId=101，4-2-3-1，控球，红/白/绿）
        //     2026-27 预测首发：拉亚; 廷贝尔, 萨利巴, 加布里埃尔, 卡拉菲奥里; 厄德高, 苏维门迪, 赖斯;
        //                       萨卡, 吉奥克雷斯, 阿尔瓦雷斯
        //     核心：萨卡(88)、厄德高(89)、赖斯(88)、萨利巴(86)、吉奥克雷斯(87)
        // ====================================================================================
        private static void InitArsenal()
        {
            // 创建球队：红色主场衣 / 白色客场衣 / 绿色门将衣
            TeamData team = CreateTeam(101, "阿森纳", League.EPL,
                new Color(0.85f, 0.10f, 0.10f),  // 主场：红
                new Color(0.95f, 0.95f, 0.95f),  // 客场：白
                new Color(0.10f, 0.70f, 0.20f),  // 门将：绿
                FormationType.F4231, TacticStyle.Possession);

            // ---------- 首发 11 人（4-2-3-1 顺序：GK, LB, LCB, RCB, RB, LDM, RDM, LAM, CAM, RAM, ST）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("拉亚",       22, PlayerPosition.GK, 60, 70, 25, 90, 83, 101), // GK  主力门将
                CreatePlayer("卡拉菲奥里", 33, PlayerPosition.DF, 80, 78, 45, 82, 80, 101), // LB  左后卫
                CreatePlayer("加布里埃尔",  6, PlayerPosition.DF, 74, 74, 42, 86, 82, 101), // LCB 左中卫
                CreatePlayer("萨利巴",      2, PlayerPosition.DF, 82, 80, 45, 88, 84, 101), // RCB 右中卫（核心）
                CreatePlayer("廷贝尔",     12, PlayerPosition.DF, 84, 78, 42, 82, 82, 101), // RB  右后卫
                CreatePlayer("赖斯",       41, PlayerPosition.MF, 76, 86, 72, 85, 86, 101), // LDM 左后腰（核心）
                CreatePlayer("苏维门迪",   24, PlayerPosition.MF, 72, 86, 68, 82, 84, 101), // RDM 右后腰（新援）
                CreatePlayer("萨卡",        7, PlayerPosition.FW, 90, 85, 88, 55, 85, 101), // LAM 左前腰（核心）
                CreatePlayer("厄德高",      8, PlayerPosition.MF, 82, 92, 86, 70, 86, 101), // CAM 中前腰（核心）
                CreatePlayer("阿尔瓦雷斯", 19, PlayerPosition.FW, 84, 82, 85, 52, 82, 101), // RAM 右前腰
                CreatePlayer("吉奥克雷斯",  9, PlayerPosition.FW, 86, 80, 88, 50, 85, 101)  // ST  单前锋（核心）
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("怀特",         4, PlayerPosition.DF, 80, 75, 42, 80, 78, 101), // DF  替补后卫
                CreatePlayer("基维奥尔",    15, PlayerPosition.DF, 72, 70, 38, 78, 76, 101), // DF  替补中卫
                CreatePlayer("富安健洋",    18, PlayerPosition.DF, 72, 72, 38, 78, 76, 101), // DF  替补后卫
                CreatePlayer("托马斯",       5, PlayerPosition.MF, 72, 82, 72, 80, 78, 101), // MF  替补后腰
                CreatePlayer("哈弗茨",      29, PlayerPosition.MF, 78, 78, 78, 68, 80, 101), // MF  替补前腰/伪9
                CreatePlayer("史密斯·罗维", 10, PlayerPosition.MF, 76, 78, 72, 62, 76, 101), // MF  替补前腰
                CreatePlayer("特罗萨德",    11, PlayerPosition.FW, 78, 76, 78, 50, 78, 101)  // FW  替补边锋
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 102. 曼城（TeamId=102，4-3-3，控球，浅蓝/白/橙）
        //     核心：哈兰德(93)、德布劳内(91)、福登(89)、罗德里(90)、迪亚斯(88)、格拉利什(85)
        //     2026 夏窗：1.2 亿英镑报价安德森（诺丁汉森林中场，未最终成交）
        // ====================================================================================
        private static void InitManCity()
        {
            TeamData team = CreateTeam(102, "曼城", League.EPL,
                new Color(0.40f, 0.70f, 0.95f),  // 主场：浅蓝
                new Color(0.95f, 0.95f, 0.95f),  // 客场：白
                new Color(0.95f, 0.50f, 0.10f),  // 门将：橙
                FormationType.F433, TacticStyle.Possession);

            // ---------- 首发 11 人（4-3-3 顺序：GK, LB, LCB, RCB, RB, LCM, CM, RCM, LW, ST, RW）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("埃德森",    31, PlayerPosition.GK, 62, 75, 25, 88, 82, 102), // GK  主力门将（出球型）
                CreatePlayer("格瓦迪奥尔",24, PlayerPosition.DF, 78, 76, 45, 84, 82, 102), // LB  左后卫
                CreatePlayer("迪亚斯",     3, PlayerPosition.DF, 72, 75, 42, 89, 84, 102), // LCB 左中卫（核心）
                CreatePlayer("阿克",       6, PlayerPosition.DF, 74, 72, 42, 83, 80, 102), // RCB 右中卫
                CreatePlayer("沃克",       2, PlayerPosition.DF, 88, 72, 40, 78, 78, 102), // RB  右后卫（速度型）
                CreatePlayer("罗德里",    16, PlayerPosition.MF, 72, 88, 75, 85, 86, 102), // LCM 左中前卫（核心后腰）
                CreatePlayer("德布劳内",  17, PlayerPosition.MF, 80, 93, 88, 72, 84, 102), // CM  中前卫（核心组织）
                CreatePlayer("福登",      47, PlayerPosition.MF, 84, 88, 85, 68, 84, 102), // RCM 右中前卫（核心）
                CreatePlayer("格拉利什",  10, PlayerPosition.FW, 82, 84, 80, 55, 84, 102), // LW  左边锋（核心）
                CreatePlayer("哈兰德",     9, PlayerPosition.FW, 88, 75, 93, 50, 85, 102), // ST  中锋（超级核心）
                CreatePlayer("多库",      11, PlayerPosition.FW, 90, 78, 78, 45, 80, 102)  // RW  右边锋（突破型）
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("斯通斯",         5, PlayerPosition.DF, 74, 75, 45, 84, 80, 102), // DF  替补中卫
                CreatePlayer("阿坎吉",        25, PlayerPosition.DF, 76, 74, 42, 82, 80, 102), // DF  替补中卫
                CreatePlayer("刘易斯",        82, PlayerPosition.DF, 76, 72, 40, 74, 75, 102), // DF  替补边卫
                CreatePlayer("贝尔纳多·席尔瓦",20, PlayerPosition.MF, 80, 86, 80, 72, 84, 102), // MF  替补中场（核心轮换）
                CreatePlayer("科瓦契奇",       8, PlayerPosition.MF, 78, 82, 75, 78, 82, 102), // MF  替补中场
                CreatePlayer("努内斯",        27, PlayerPosition.MF, 76, 76, 72, 72, 76, 102), // MF  替补中场
                CreatePlayer("萨维尼奥",      26, PlayerPosition.FW, 84, 74, 75, 45, 76, 102)  // FW  替补边锋
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 103. 利物浦（TeamId=103，4-3-3，高位压迫，红/黑/黄）
        //     核心：萨拉赫(91)、范迪克(89)、麦卡利斯特(87)、迪亚斯(86)、加克波(85)
        //     2026 夏窗：签下 17 岁哥伦比亚新星马丁内斯
        // ====================================================================================
        private static void InitLiverpool()
        {
            TeamData team = CreateTeam(103, "利物浦", League.EPL,
                new Color(0.80f, 0.10f, 0.10f),  // 主场：红
                new Color(0.10f, 0.10f, 0.10f),  // 客场：黑
                new Color(0.95f, 0.85f, 0.10f),  // 门将：黄
                FormationType.F433, TacticStyle.HighPress);

            // ---------- 首发 11 人（4-3-3）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("阿利松",    1, PlayerPosition.GK, 60, 72, 25, 91, 84, 103), // GK  主力门将（世界级）
                CreatePlayer("罗伯逊",   26, PlayerPosition.DF, 82, 80, 50, 80, 84, 103), // LB  左后卫
                CreatePlayer("范迪克",     4, PlayerPosition.DF, 76, 78, 50, 90, 84, 103), // LCB 左中卫（核心）
                CreatePlayer("科纳特",     5, PlayerPosition.DF, 82, 72, 42, 85, 80, 103), // RCB 右中卫
                CreatePlayer("阿诺德",   66, PlayerPosition.DF, 80, 85, 55, 78, 80, 103), // RB  右后卫（出球型）
                CreatePlayer("麦卡利斯特",10, PlayerPosition.MF, 74, 88, 80, 80, 85, 103), // LCM 左中前卫（核心）
                CreatePlayer("索博斯洛伊", 8, PlayerPosition.MF, 80, 82, 80, 72, 82, 103), // CM  中前卫
                CreatePlayer("赫拉芬贝赫",38, PlayerPosition.MF, 78, 80, 72, 76, 80, 103), // RCM 右中前卫
                CreatePlayer("迪亚斯",     7, PlayerPosition.FW, 88, 80, 82, 52, 82, 103), // LW  左边锋（核心）
                CreatePlayer("努涅斯",     9, PlayerPosition.FW, 86, 75, 83, 50, 82, 103), // ST  中锋
                CreatePlayer("萨拉赫",    11, PlayerPosition.FW, 88, 85, 90, 55, 85, 103)  // RW  右边锋（超级核心）
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("戈麦斯",     2, PlayerPosition.DF, 78, 70, 40, 78, 78, 103), // DF  替补后卫（多面手）
                CreatePlayer("夸安萨",    78, PlayerPosition.DF, 74, 68, 38, 75, 75, 103), // DF  替补中卫
                CreatePlayer("齐米卡斯",  84, PlayerPosition.DF, 76, 72, 40, 75, 76, 103), // DF  替补左后卫
                CreatePlayer("远藤航",     3, PlayerPosition.MF, 70, 76, 68, 78, 78, 103), // MF  替补后腰
                CreatePlayer("琼斯",      17, PlayerPosition.MF, 76, 78, 72, 70, 78, 103), // MF  替补中场
                CreatePlayer("马丁内斯",  31, PlayerPosition.MF, 74, 72, 68, 65, 72, 103), // MF  17 岁哥伦比亚新星（夏窗新签）
                CreatePlayer("加克波",    18, PlayerPosition.FW, 82, 80, 82, 52, 82, 103)  // FW  替补边锋（核心轮换）
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 104. 曼联（TeamId=104，4-2-3-1，反击，红/白/蓝）
        //     核心：B费(88)、拉什福德(84)、霍伊伦(83)、卡塞米罗(85)、马丁内斯(84)
        //     2026 夏窗：计划 3 亿英镑引援重建
        // ====================================================================================
        private static void InitManUnited()
        {
            TeamData team = CreateTeam(104, "曼联", League.EPL,
                new Color(0.85f, 0.05f, 0.05f),  // 主场：红
                new Color(0.95f, 0.95f, 0.95f),  // 客场：白
                new Color(0.10f, 0.30f, 0.80f),  // 门将：蓝
                FormationType.F4231, TacticStyle.Counter);

            // ---------- 首发 11 人（4-2-3-1）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("奥纳纳",    24, PlayerPosition.GK, 62, 72, 28, 86, 82, 104), // GK  主力门将（出球型）
                CreatePlayer("肖",        23, PlayerPosition.DF, 80, 76, 45, 82, 80, 104), // LB  左后卫
                CreatePlayer("马丁内斯",   6, PlayerPosition.DF, 76, 75, 42, 85, 82, 104), // LCB 左中卫（核心）
                CreatePlayer("德利赫特",   4, PlayerPosition.DF, 72, 72, 42, 85, 82, 104), // RCB 右中卫
                CreatePlayer("达洛特",    20, PlayerPosition.DF, 82, 75, 42, 80, 80, 104), // RB  右后卫
                CreatePlayer("卡塞米罗",  18, PlayerPosition.MF, 70, 80, 75, 86, 82, 104), // LDM 左后腰（核心）
                CreatePlayer("乌加特",    25, PlayerPosition.MF, 76, 78, 68, 82, 82, 104), // RDM 右后腰
                CreatePlayer("拉什福德",  10, PlayerPosition.FW, 88, 78, 83, 50, 80, 104), // LAM 左前腰（核心）
                CreatePlayer("B费",        8, PlayerPosition.MF, 78, 88, 85, 70, 82, 104), // CAM 中前腰（核心组织）
                CreatePlayer("加纳乔",    17, PlayerPosition.FW, 86, 75, 78, 45, 78, 104), // RAM 右前腰
                CreatePlayer("霍伊伦",     9, PlayerPosition.FW, 82, 72, 80, 50, 80, 104)  // ST  单前锋（核心）
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("马奎尔",     5, PlayerPosition.DF, 70, 70, 42, 80, 76, 104), // DF  替补中卫
                CreatePlayer("林德洛夫",   2, PlayerPosition.DF, 74, 72, 40, 76, 75, 104), // DF  替补中卫
                CreatePlayer("马兹拉维",   3, PlayerPosition.DF, 76, 74, 40, 78, 76, 104), // DF  替补边卫
                CreatePlayer("芒特",       7, PlayerPosition.MF, 76, 78, 72, 68, 76, 104), // MF  替补前腰
                CreatePlayer("梅努",      37, PlayerPosition.MF, 76, 78, 70, 72, 78, 104), // MF  替补中场（青训）
                CreatePlayer("埃里克森",  14, PlayerPosition.MF, 70, 80, 75, 65, 72, 104), // MF  替补中场（老将）
                CreatePlayer("齐尔克泽",  11, PlayerPosition.FW, 76, 75, 75, 48, 76, 104)  // FW  替补前锋
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 105. 切尔西（TeamId=105，4-2-3-1，控球，蓝/白/绿）
        //     核心：帕尔默(87)、杰克逊(82)、凯塞多(85)、恩佐(84)、库库雷利亚(82)
        //     2026 夏窗：预签巴尔科（左后卫）+ 埃梅加（前锋）
        // ====================================================================================
        private static void InitChelsea()
        {
            TeamData team = CreateTeam(105, "切尔西", League.EPL,
                new Color(0.10f, 0.30f, 0.85f),  // 主场：蓝
                new Color(0.95f, 0.95f, 0.95f),  // 客场：白
                new Color(0.10f, 0.70f, 0.20f),  // 门将：绿
                FormationType.F4231, TacticStyle.Possession);

            // ---------- 首发 11 人（4-2-3-1）----------
            // 注：内托(LAM)归类为 MF 以平衡位置分布（4231 边前腰可兼中场）
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("桑切斯",     1, PlayerPosition.GK, 60, 68, 25, 82, 78, 105), // GK  主力门将
                CreatePlayer("库库雷利亚",  3, PlayerPosition.DF, 80, 78, 48, 82, 82, 105), // LB  左后卫（核心）
                CreatePlayer("科尔威尔",   26, PlayerPosition.DF, 74, 72, 42, 82, 80, 105), // LCB 左中卫
                CreatePlayer("福法纳",     29, PlayerPosition.DF, 82, 70, 40, 82, 78, 105), // RCB 右中卫
                CreatePlayer("詹姆斯",     28, PlayerPosition.DF, 82, 80, 50, 82, 82, 105), // RB  右后卫（核心）
                CreatePlayer("凯塞多",     25, PlayerPosition.MF, 76, 82, 72, 85, 84, 105), // LDM 左后腰（核心）
                CreatePlayer("恩佐",        8, PlayerPosition.MF, 74, 85, 78, 78, 82, 105), // RDM 右后腰（核心）
                CreatePlayer("内托",        7, PlayerPosition.MF, 84, 78, 75, 65, 78, 105), // LAM 左前腰（边锋客串）
                CreatePlayer("帕尔默",     20, PlayerPosition.MF, 78, 88, 85, 68, 82, 105), // CAM 中前腰（核心）
                CreatePlayer("马杜埃凯",   11, PlayerPosition.FW, 84, 75, 78, 48, 78, 105), // RAM 右前腰
                CreatePlayer("杰克逊",     15, PlayerPosition.FW, 84, 75, 80, 50, 80, 105)  // ST  单前锋（核心）
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("巴尔科",     30, PlayerPosition.DF, 76, 70, 38, 74, 72, 105), // DF  夏窗预签左后卫
                CreatePlayer("巴迪亚西勒",  6, PlayerPosition.DF, 72, 68, 38, 78, 76, 105), // DF  替补中卫
                CreatePlayer("古斯托",     27, PlayerPosition.DF, 82, 72, 40, 78, 78, 105), // DF  替补右后卫
                CreatePlayer("拉维亚",     45, PlayerPosition.MF, 74, 76, 68, 78, 78, 105), // MF  替补后腰
                CreatePlayer("费利克斯",   14, PlayerPosition.MF, 78, 80, 78, 65, 78, 105), // MF  替补前腰
                CreatePlayer("恩昆库",     18, PlayerPosition.FW, 82, 80, 84, 55, 80, 105), // FW  替补前锋（核心轮换）
                CreatePlayer("埃梅加",      9, PlayerPosition.FW, 80, 70, 76, 48, 74, 105)  // FW  夏窗预签前锋
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 106. 热刺（TeamId=106，4-2-3-1，高位压迫，白/蓝/橙）
        //     核心：孙兴慜(87)、麦迪逊(85)、罗梅罗(85)、维卡里奥(83)、波罗(83)
        // ====================================================================================
        private static void InitTottenham()
        {
            TeamData team = CreateTeam(106, "热刺", League.EPL,
                new Color(0.95f, 0.95f, 0.95f),  // 主场：白
                new Color(0.10f, 0.30f, 0.80f),  // 客场：蓝
                new Color(0.95f, 0.50f, 0.10f),  // 门将：橙
                FormationType.F4231, TacticStyle.HighPress);

            // ---------- 首发 11 人（4-2-3-1）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("维卡里奥",   1, PlayerPosition.GK, 62, 70, 25, 84, 82, 106), // GK  主力门将（核心）
                CreatePlayer("乌多吉",    13, PlayerPosition.DF, 84, 75, 45, 80, 82, 106), // LB  左后卫
                CreatePlayer("罗梅罗",    17, PlayerPosition.DF, 76, 75, 45, 86, 82, 106), // LCB 左中卫（核心）
                CreatePlayer("范德文",    37, PlayerPosition.DF, 88, 72, 42, 84, 82, 106), // RCB 右中卫（速度型）
                CreatePlayer("波罗",      23, PlayerPosition.DF, 82, 78, 50, 80, 82, 106), // RB  右后卫（核心）
                CreatePlayer("比苏马",     8, PlayerPosition.MF, 78, 80, 70, 82, 82, 106), // LDM 左后腰
                CreatePlayer("萨尔",      29, PlayerPosition.MF, 78, 75, 68, 78, 80, 106), // RDM 右后腰
                CreatePlayer("孙兴慜",     7, PlayerPosition.FW, 86, 82, 86, 55, 82, 106), // LAM 左前腰（核心队长）
                CreatePlayer("麦迪逊",    10, PlayerPosition.MF, 76, 85, 80, 70, 80, 106), // CAM 中前腰（核心）
                CreatePlayer("库卢塞夫斯基",21, PlayerPosition.FW, 82, 82, 80, 55, 82, 106), // RAM 右前腰
                CreatePlayer("索兰克",    19, PlayerPosition.FW, 78, 75, 80, 52, 80, 106)  // ST  单前锋
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("德拉古辛",   6, PlayerPosition.DF, 72, 68, 40, 78, 76, 106), // DF  替补中卫
                CreatePlayer("戴维斯",    33, PlayerPosition.DF, 72, 70, 38, 76, 75, 106), // DF  替补左后卫
                CreatePlayer("斯彭斯",    24, PlayerPosition.DF, 76, 70, 38, 74, 74, 106), // DF  替补右后卫
                CreatePlayer("本坦库尔",  30, PlayerPosition.MF, 74, 78, 72, 76, 78, 106), // MF  替补中场
                CreatePlayer("贝里瓦尔",  15, PlayerPosition.MF, 72, 72, 65, 68, 72, 106), // MF  替补中场（青训）
                CreatePlayer("洛塞尔索",  18, PlayerPosition.MF, 72, 78, 72, 68, 74, 106), // MF  替补前腰
                CreatePlayer("理查利森",   9, PlayerPosition.FW, 80, 75, 78, 52, 78, 106)  // FW  替补前锋
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 107. 阿斯顿维拉（TeamId=107，4-2-3-1，反击，栗色/白/黄）
        //     核心：沃特金斯(84)、道格拉斯·路易斯(83)、马丁内斯(84)、保·托雷斯(83)
        // ====================================================================================
        private static void InitAstonVilla()
        {
            TeamData team = CreateTeam(107, "阿斯顿维拉", League.EPL,
                new Color(0.50f, 0.10f, 0.15f),  // 主场：栗色
                new Color(0.95f, 0.95f, 0.95f),  // 客场：白
                new Color(0.95f, 0.85f, 0.10f),  // 门将：黄
                FormationType.F4231, TacticStyle.Counter);

            // ---------- 首发 11 人（4-2-3-1）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("马丁内斯",   1, PlayerPosition.GK, 60, 70, 28, 87, 82, 107), // GK  主力门将（核心，世界杯冠军）
                CreatePlayer("迪涅",      15, PlayerPosition.DF, 78, 76, 45, 80, 78, 107), // LB  左后卫
                CreatePlayer("保·托雷斯",  4, PlayerPosition.DF, 74, 75, 42, 84, 82, 107), // LCB 左中卫（核心）
                CreatePlayer("孔萨",       6, PlayerPosition.DF, 78, 72, 42, 82, 80, 107), // RCB 右中卫
                CreatePlayer("卡什",       2, PlayerPosition.DF, 80, 72, 45, 80, 80, 107), // RB  右后卫
                CreatePlayer("道格拉斯·路易斯",8, PlayerPosition.MF, 74, 84, 75, 80, 82, 107), // LDM 左后腰（核心）
                CreatePlayer("蒂勒曼斯",  18, PlayerPosition.MF, 72, 84, 78, 78, 82, 107), // RDM 右后腰
                CreatePlayer("贝利",      31, PlayerPosition.FW, 86, 78, 78, 50, 78, 107), // LAM 左前腰（速度型）
                CreatePlayer("罗杰斯",    27, PlayerPosition.MF, 78, 78, 75, 70, 78, 107), // CAM 中前腰
                CreatePlayer("布恩迪亚",  10, PlayerPosition.FW, 78, 82, 76, 52, 78, 107), // RAM 右前腰
                CreatePlayer("沃特金斯",  11, PlayerPosition.FW, 84, 78, 84, 52, 82, 107)  // ST  单前锋（核心）
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("明斯",       5, PlayerPosition.DF, 70, 68, 40, 80, 78, 107), // DF  替补中卫（老将队长）
                CreatePlayer("迭戈·卡洛斯",12, PlayerPosition.DF, 72, 68, 40, 78, 76, 107), // DF  替补中卫
                CreatePlayer("莫雷诺",    23, PlayerPosition.DF, 76, 72, 40, 76, 76, 107), // DF  替补左后卫
                CreatePlayer("麦金",       7, PlayerPosition.MF, 74, 78, 75, 76, 80, 107), // MF  替补中场（队长）
                CreatePlayer("拉姆齐",    41, PlayerPosition.MF, 76, 75, 70, 68, 75, 107), // MF  替补中场（青训）
                CreatePlayer("巴克利",    25, PlayerPosition.MF, 70, 76, 72, 68, 74, 107), // MF  替补前腰
                CreatePlayer("杜兰",       9, PlayerPosition.FW, 78, 70, 76, 48, 76, 107)  // FW  替补前锋
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 108. 纽卡斯尔联（TeamId=108，4-3-3，高位压迫，黑白条纹/蓝/绿）
        //     核心：伊萨克(86)、吉马良斯(85)、博特曼(83)、波普(82)、特里皮尔(83)
        //     注：黑白条纹球衣以深灰单色近似表示
        // ====================================================================================
        private static void InitNewcastle()
        {
            TeamData team = CreateTeam(108, "纽卡斯尔联", League.EPL,
                new Color(0.15f, 0.15f, 0.15f),  // 主场：黑白条纹（以深灰近似）
                new Color(0.10f, 0.30f, 0.80f),  // 客场：蓝
                new Color(0.10f, 0.70f, 0.20f),  // 门将：绿
                FormationType.F433, TacticStyle.HighPress);

            // ---------- 首发 11 人（4-3-3）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("波普",      22, PlayerPosition.GK, 58, 65, 25, 85, 80, 108), // GK  主力门将（核心）
                CreatePlayer("霍尔",      18, PlayerPosition.DF, 78, 75, 45, 78, 78, 108), // LB  左后卫
                CreatePlayer("博特曼",     4, PlayerPosition.DF, 72, 70, 42, 84, 82, 108), // LCB 左中卫（核心）
                CreatePlayer("舍尔",       5, PlayerPosition.DF, 74, 75, 45, 82, 80, 108), // RCB 右中卫
                CreatePlayer("利夫拉门托", 21, PlayerPosition.DF, 82, 74, 42, 78, 78, 108), // RB  右后卫
                CreatePlayer("吉马良斯",  39, PlayerPosition.MF, 76, 86, 78, 80, 84, 108), // LCM 左中前卫（核心）
                CreatePlayer("托纳利",     8, PlayerPosition.MF, 76, 84, 75, 80, 84, 108), // CM  中前卫
                CreatePlayer("若埃林顿",   7, PlayerPosition.MF, 80, 78, 75, 80, 84, 108), // RCM 右中前卫
                CreatePlayer("戈登",      10, PlayerPosition.FW, 86, 78, 80, 52, 80, 108), // LW  左边锋
                CreatePlayer("伊萨克",    14, PlayerPosition.FW, 84, 80, 86, 50, 82, 108), // ST  中锋（核心）
                CreatePlayer("阿尔米隆",  24, PlayerPosition.FW, 84, 75, 75, 48, 78, 108)  // RW  右边锋
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("伯恩",       2, PlayerPosition.DF, 68, 65, 40, 78, 76, 108), // DF  替补中卫/左后卫
                CreatePlayer("塔吉特",    15, PlayerPosition.DF, 74, 70, 40, 75, 75, 108), // DF  替补左后卫
                CreatePlayer("克拉夫特",  17, PlayerPosition.DF, 72, 68, 38, 74, 74, 108), // DF  替补右后卫
                CreatePlayer("朗斯塔夫",  36, PlayerPosition.MF, 72, 76, 70, 72, 76, 108), // MF  替补中场（青训）
                CreatePlayer("威洛克",    28, PlayerPosition.MF, 78, 76, 72, 70, 78, 108), // MF  替补中场
                CreatePlayer("迈利",      72, PlayerPosition.MF, 70, 72, 65, 65, 72, 108), // MF  替补中场（青训小将）
                CreatePlayer("巴恩斯",     9, PlayerPosition.FW, 80, 75, 78, 48, 78, 108)  // FW  替补边锋
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 109. 布莱顿（TeamId=109，4-2-3-1，控球，蓝白条纹/白/橙）
        //     核心：三笘薰(84)、弗格森(82)、邓克(80)
        //     注：麦卡利斯特已离队（转投利物浦）；蓝白条纹球衣以蓝色近似表示
        // ====================================================================================
        private static void InitBrighton()
        {
            TeamData team = CreateTeam(109, "布莱顿", League.EPL,
                new Color(0.10f, 0.40f, 0.80f),  // 主场：蓝白条纹（以蓝近似）
                new Color(0.95f, 0.95f, 0.95f),  // 客场：白
                new Color(0.95f, 0.50f, 0.10f),  // 门将：橙
                FormationType.F4231, TacticStyle.Possession);

            // ---------- 首发 11 人（4-2-3-1）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("弗尔布鲁根",  1, PlayerPosition.GK, 58, 65, 25, 82, 78, 109), // GK  主力门将
                CreatePlayer("埃斯图皮尼安",3, PlayerPosition.DF, 82, 76, 45, 80, 80, 109), // LB  左后卫
                CreatePlayer("邓克",        5, PlayerPosition.DF, 68, 70, 42, 82, 78, 109), // LCB 左中卫（核心老将）
                CreatePlayer("范赫克",      4, PlayerPosition.DF, 74, 70, 40, 78, 76, 109), // RCB 右中卫
                CreatePlayer("兰普泰",      2, PlayerPosition.DF, 84, 72, 42, 76, 76, 109), // RB  右后卫
                CreatePlayer("巴莱巴",      8, PlayerPosition.MF, 76, 78, 70, 78, 80, 109), // LDM 左后腰
                CreatePlayer("吉尔莫",      6, PlayerPosition.MF, 72, 80, 68, 76, 80, 109), // RDM 右后腰
                CreatePlayer("三笘薰",     22, PlayerPosition.FW, 86, 80, 82, 52, 82, 109), // LAM 左前腰（核心，日本球星）
                CreatePlayer("鲁特尔",     20, PlayerPosition.MF, 78, 80, 75, 68, 78, 109), // CAM 中前腰
                CreatePlayer("阿丁格拉",    11, PlayerPosition.FW, 82, 76, 76, 48, 76, 109), // RAM 右前腰
                CreatePlayer("弗格森",      9, PlayerPosition.FW, 78, 72, 82, 50, 78, 109)  // ST  单前锋（核心，爱尔兰小将）
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("韦伯斯特",  15, PlayerPosition.DF, 70, 68, 40, 76, 75, 109), // DF  替补中卫
                CreatePlayer("伊戈尔",    29, PlayerPosition.DF, 72, 68, 38, 76, 75, 109), // DF  替补中卫
                CreatePlayer("米尔纳",     7, PlayerPosition.DF, 72, 75, 42, 74, 75, 109), // DF  替补边卫（老将多面手）
                CreatePlayer("莫德尔",    19, PlayerPosition.MF, 72, 74, 68, 70, 74, 109), // MF  替补中场
                CreatePlayer("阿亚里",    40, PlayerPosition.MF, 70, 72, 65, 68, 72, 109), // MF  替补中场（青训）
                CreatePlayer("奥马奥尼",  41, PlayerPosition.MF, 68, 70, 62, 65, 70, 109), // MF  替补中场（青训小将）
                CreatePlayer("韦尔贝克",  18, PlayerPosition.FW, 76, 74, 75, 48, 75, 109)  // FW  替补前锋（老将）
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 110. 西汉姆联（TeamId=110，4-4-2，反击，栗色/蓝/绿）
        //     核心：鲍恩(83)、帕奎塔(83)、库杜斯(82)、阿尔瓦雷斯(81)
        // ====================================================================================
        private static void InitWestHam()
        {
            TeamData team = CreateTeam(110, "西汉姆联", League.EPL,
                new Color(0.50f, 0.10f, 0.15f),  // 主场：栗色
                new Color(0.10f, 0.30f, 0.80f),  // 客场：蓝
                new Color(0.10f, 0.70f, 0.20f),  // 门将：绿
                FormationType.F442, TacticStyle.Counter);

            // ---------- 首发 11 人（4-4-2 顺序：GK, LB, LCB, RCB, RB, LM, LCM, RCM, RM, LST, RST）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("阿雷奥拉",   1, PlayerPosition.GK, 60, 68, 25, 84, 80, 110), // GK  主力门将
                CreatePlayer("埃默森",    33, PlayerPosition.DF, 80, 75, 45, 80, 80, 110), // LB  左后卫
                CreatePlayer("基尔曼",    15, PlayerPosition.DF, 74, 72, 42, 82, 80, 110), // LCB 左中卫
                CreatePlayer("托迪博",     4, PlayerPosition.DF, 80, 70, 40, 84, 80, 110), // RCB 右中卫
                CreatePlayer("万-比萨卡", 29, PlayerPosition.DF, 84, 70, 38, 80, 78, 110), // RB  右后卫（防守型）
                CreatePlayer("库杜斯",    14, PlayerPosition.MF, 84, 78, 78, 60, 80, 110), // LM  左前卫（核心，加纳球星）
                CreatePlayer("帕奎塔",     8, PlayerPosition.MF, 78, 84, 78, 75, 82, 110), // LCM 左中前卫（核心）
                CreatePlayer("阿尔瓦雷斯",19, PlayerPosition.MF, 74, 78, 70, 80, 80, 110), // RCM 右中前卫（核心，墨西哥后腰）
                CreatePlayer("鲍恩",      20, PlayerPosition.MF, 84, 78, 80, 55, 80, 110), // RM  右前卫（核心）
                CreatePlayer("菲尔克鲁格",11, PlayerPosition.FW, 70, 72, 82, 50, 80, 110), // LST 左前锋（德国中锋）
                CreatePlayer("安东尼奥",   9, PlayerPosition.FW, 80, 70, 76, 50, 78, 110)  // RST 右前锋
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("祖马",      23, PlayerPosition.DF, 68, 65, 40, 78, 75, 110), // DF  替补中卫
                CreatePlayer("库法尔",     2, PlayerPosition.DF, 74, 70, 38, 76, 75, 110), // DF  替补右后卫
                CreatePlayer("克雷斯韦尔", 5, PlayerPosition.DF, 70, 70, 38, 74, 72, 110), // DF  替补左后卫（老将）
                CreatePlayer("绍切克",    28, PlayerPosition.MF, 72, 75, 75, 78, 80, 110), // MF  替补中场（捷克高塔）
                CreatePlayer("沃德-普劳斯", 7, PlayerPosition.MF, 70, 82, 75, 70, 78, 110), // MF  替补中场（定位球专家）
                CreatePlayer("萨默维尔",  10, PlayerPosition.FW, 82, 75, 75, 48, 76, 110), // FW  替补边锋
                CreatePlayer("因斯",      18, PlayerPosition.FW, 74, 70, 74, 45, 72, 110)  // FW  替补前锋
            };

            SetLineup(team, starting, subs);
        }
    }
}
