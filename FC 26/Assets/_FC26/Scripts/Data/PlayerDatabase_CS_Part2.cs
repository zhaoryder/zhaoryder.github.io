using UnityEngine;

namespace FC26.Data
{
    /// <summary>
    /// PlayerDatabase 中超联赛分部（Part2）。
    /// 本文件实现 <c>InitCSLeague_Part2()</c>，负责创建中超 2026 赛季后 8 支球队
    /// （TeamId 9 ~ 16）的完整大名单（每队 18 人：首发 11 + 替补 7），
    /// 并注册到 <c>_teams</c> 与 <c>_allTeams</c>。
    ///
    /// 辅助方法（由主文件 PlayerDatabase.cs 提供，本文件直接调用）：
    ///   - CreateTeam(...)      创建球队并返回 TeamData（内部已注册到 _teams / _allTeams）
    ///   - CreatePlayer(...)    创建球员并返回 PlayerData
    ///   - SetLineup(...)       设置首发与替补
    ///
    /// 大名单摘要（TeamId 9-16，2026 赛季最新阵容）：
    /// ┌────┬──────────────┬────────┬──────────────────┬──────────────────────────────────────────┐
    /// │ ID │ 球队         │ 阵型   │ 战术             │ 核心球员                                 │
    /// ├────┼──────────────┼────────┼──────────────────┼──────────────────────────────────────────┤
    /// │ 9  │ 河南队       │ 4-4-2  │ DefensiveCounter │ 古斯塔沃(80)、姆布拉(78)、王上源(78)     │
    /// │ 10 │ 青岛海牛     │ 4-4-2  │ Counter          │ 希特兰(76)、安杰尔科维奇(76)、叶博亚(75) │
    /// │ 11 │ 深圳新鹏城   │ 4-2-3-1│ Counter          │ 加布里埃尔·哈维尔(78)、蒂亚戈(77)、埃杜(76)│
    /// │ 12 │ 大连英博海发 │ 4-4-2  │ DefensiveCounter │ 斯坦丘(80)、马莱莱(78)、阿利米(76)       │
    /// │ 13 │ 青岛西海岸   │ 4-2-3-1│ Counter          │ 梅米舍维奇(78)、雷森德(78)、阿齐兹(77)   │
    /// │ 14 │ 辽宁铁人     │ 4-4-2  │ DefensiveCounter │ 邦本宜裕(77)、姆本扎(77)、严鼎皓(76)     │
    /// │ 15 │ 重庆铜梁龙   │ 4-4-2  │ Counter          │ 向余望(75)、尤尼(75)、黄希扬(74)         │
    /// │ 16 │ 云南玉昆     │ 4-2-3-1│ DefensiveCounter │ 奥斯卡(78)、约尼查(77)、黄紫昌(77)       │
    /// └────┴──────────────┴────────┴──────────────────┴──────────────────────────────────────────┘
    ///
    /// 能力值定位：中超外援核心 77-80，本土主力 73-76，替补 70-73。
    /// 每队 18 人配置：1 GK + 6 DF + 5 MF + 4 FW（442）或 1 GK + 6 DF + 5 MF + 4 FW（4231）。
    /// CreatePlayer 参数顺序：name, number, pos, speed, passing, shooting, defense, stamina, teamId。
    /// 首发顺序严格对应 Formations.cs 中各阵型坐标顺序。
    ///
    /// 2026 赛季动态要点：
    ///   - 河南队：主帅丹尼尔-拉莫斯；阿奇姆彭、卡多索、尹鸿博、黄紫昌等 10 人离队；
    ///             古斯塔沃、姆布拉、阿布拉汗、阿卜杜肉苏力等 6 名新援加盟；王上源留队。
    ///   - 青岛海牛：主帅米兰·里斯特夫斯基；新援王峤、刘鑫瑜、张卫、穆斯塔帕等；降级热门。
    ///   - 深圳新鹏城：主帅陈涛；夏窗首签加布里埃尔·哈维尔（巴西中卫，400 万欧身价）。
    ///   - 大连英博海发：斯坦丘、马莱莱、阿利米、马马杜四外援；新援李昂、罗竞。
    ///   - 青岛西海岸：主帅郑智（助教黄博文、刘健、梅方）；队史标王雷森德；新援梅米舍维奇。
    ///   - 辽宁铁人：8 年后重返中超；新援严鼎皓、邦本宜裕、费利佩、李提香、田依浓。
    ///   - 重庆铜梁龙：主帅刘建业（本土少帅）；卡里略等三外援离队；青年之师。
    ///   - 云南玉昆：二年级中超；新援黄紫昌、徐新、邓函文、石柯、鲍亚雄、卡约；候永永归化。
    /// </summary>
    public static partial class PlayerDatabase
    {
        /// <summary>
        /// 初始化中超联赛后 8 队（TeamId 9-16）。
        /// 由主文件 PlayerDatabase 静态构造函数通过 partial 调用链触发。
        /// </summary>
        static partial void InitCSLeague_Part2()
        {
            InitHenan();           // 9  河南队
            InitQingdaoHainiu();   // 10 青岛海牛
            InitShenzhen();        // 11 深圳新鹏城
            InitDalianYingbo();    // 12 大连英博海发
            InitQingdaoXihai();    // 13 青岛西海岸
            InitLiaoningTieren();  // 14 辽宁铁人
            InitChongqing();       // 15 重庆铜梁龙
            InitYunnanYukun();     // 16 云南玉昆
        }

        // ====================================================================================
        // 9. 河南队（TeamId=9，4-4-2，防守反击，红/白/黑）
        //     2026 阵容：主帅丹尼尔-拉莫斯；10 人离队（含阿奇姆彭、卡多索、尹鸿博、黄紫昌）；
        //     6 名新援：古斯塔沃、姆布拉、阿布拉汗、阿卜杜肉苏力等；王上源留队；迈达纳后卫。
        //     核心：古斯塔沃(80,中超铜靴)、姆布拉(78)、王上源(78)、迈达纳(78)
        // ====================================================================================
        private static void InitHenan()
        {
            // 创建球队：红色主场衣 / 白色客场衣 / 黑色门将衣
            TeamData team = CreateTeam(9, "河南队", League.CSL,
                new Color(0.85f, 0.10f, 0.10f),  // 主场：红
                new Color(0.95f, 0.95f, 0.95f),  // 客场：白
                new Color(0.10f, 0.10f, 0.10f),  // 门将：黑
                FormationType.F442, TacticStyle.DefensiveCounter);

            // ---------- 首发 11 人（4-4-2 顺序：GK, LB, LCB, RCB, RB, LM, LCM, RCM, RM, LST, RST）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("王国明",       18, PlayerPosition.GK, 55, 71, 25, 85, 78, 9), // GK  主力门将
                CreatePlayer("刘家辉",        5, PlayerPosition.DF, 78, 71, 40, 78, 76, 9), // LB  左后卫
                CreatePlayer("迈达纳",        2, PlayerPosition.DF, 76, 73, 38, 85, 80, 9), // LCB 左中卫（外援核心）
                CreatePlayer("周缘德",        3, PlayerPosition.DF, 71, 68, 38, 80, 75, 9), // RCB 右中卫
                CreatePlayer("杨阔",         16, PlayerPosition.DF, 78, 71, 40, 78, 76, 9), // RB  右后卫
                CreatePlayer("王上源",       10, PlayerPosition.MF, 80, 83, 76, 72, 80, 9), // LM  左前卫（核心队长）
                CreatePlayer("阿布拉汗",      6, PlayerPosition.MF, 74, 78, 68, 76, 76, 9), // LCM 左中前卫（新援）
                CreatePlayer("阿卜杜肉苏力",  8, PlayerPosition.MF, 74, 78, 68, 76, 76, 9), // RCM 右中前卫（新援）
                CreatePlayer("钟义浩",        7, PlayerPosition.MF, 76, 79, 72, 68, 76, 9), // RM  右前卫
                CreatePlayer("古斯塔沃",      9, PlayerPosition.FW, 84, 77, 84, 50, 82, 9), // LST 左前锋（新援外援，中超铜靴）
                CreatePlayer("姆布拉",       11, PlayerPosition.FW, 82, 75, 82, 50, 80, 9)  // RST 右前锋（新援外援）
            };

            // ---------- 替补 7 人 ----------
            // 注：黄紫昌已离队，21 号由其他前锋顶替
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("王金帅",       17, PlayerPosition.GK, 52, 68, 25, 82, 75, 9), // GK  替补门将
                CreatePlayer("刘易鑫",       27, PlayerPosition.DF, 76, 69, 40, 76, 74, 9), // DF  替补后卫
                CreatePlayer("徐浩峰",       25, PlayerPosition.DF, 77, 70, 40, 77, 75, 9), // DF  替补后卫
                CreatePlayer("杨意林",       14, PlayerPosition.MF, 74, 77, 70, 66, 74, 9), // MF  替补前卫（新援）
                CreatePlayer("冯伯元",       21, PlayerPosition.FW, 77, 70, 77, 50, 75, 9), // FW  替补前锋（顶替已离队黄紫昌）
                CreatePlayer("陈克强",       19, PlayerPosition.FW, 75, 68, 75, 50, 73, 9), // FW  替补前锋
                CreatePlayer("韩东",         23, PlayerPosition.FW, 75, 68, 75, 50, 73, 9)  // FW  替补前锋
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 10. 青岛海牛（TeamId=10，4-4-2，反击，橙/白/蓝）
        //     2026 阵容：主帅米兰·里斯特夫斯基；新援王峤、刘鑫瑜、张卫、穆斯塔帕、颜卓彬、吴星宇；
        //     外援叶博亚、希特兰、恩约马、梅萨乌迪、安杰尔科维奇；降级热门。
        //     核心：希特兰(76)、安杰尔科维奇(76)、叶博亚(75)、恩约马(75)
        // ====================================================================================
        private static void InitQingdaoHainiu()
        {
            // 创建球队：橙色主场衣 / 白色客场衣 / 蓝色门将衣
            TeamData team = CreateTeam(10, "青岛海牛", League.CSL,
                new Color(0.95f, 0.50f, 0.10f),  // 主场：橙
                new Color(0.95f, 0.95f, 0.95f),  // 客场：白
                new Color(0.10f, 0.30f, 0.85f),  // 门将：蓝
                FormationType.F442, TacticStyle.Counter);

            // ---------- 首发 11 人（4-4-2）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("牟鹏飞",        1, PlayerPosition.GK, 52, 68, 25, 82, 75, 10), // GK  主力门将
                CreatePlayer("张卫",          2, PlayerPosition.DF, 77, 70, 40, 77, 75, 10), // LB  左后卫（新援）
                CreatePlayer("安杰尔科维奇",  5, PlayerPosition.DF, 74, 71, 38, 83, 78, 10), // LCB 左中卫（外援核心）
                CreatePlayer("刘军帅",       26, PlayerPosition.DF, 72, 69, 38, 81, 76, 10), // RCB 右中卫
                CreatePlayer("王峤",         17, PlayerPosition.DF, 76, 69, 40, 76, 74, 10), // RB  右后卫（新援）
                CreatePlayer("恩约马",        6, PlayerPosition.MF, 77, 80, 73, 69, 77, 10), // LM  左前卫（外援）
                CreatePlayer("梅萨乌迪",      8, PlayerPosition.MF, 74, 78, 68, 76, 76, 10), // LCM 左中前卫（外援）
                CreatePlayer("罗森文",       14, PlayerPosition.MF, 73, 77, 67, 75, 75, 10), // RCM 右中前卫
                CreatePlayer("叶博亚",        7, PlayerPosition.MF, 77, 80, 73, 69, 77, 10), // RM  右前卫（外援）
                CreatePlayer("刘鑫瑜",        9, PlayerPosition.FW, 78, 71, 78, 50, 76, 10), // LST 左前锋（新援）
                CreatePlayer("希特兰",       11, PlayerPosition.FW, 80, 73, 80, 50, 78, 10)  // RST 右前锋（外援核心）
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("刘君",         22, PlayerPosition.GK, 50, 65, 25, 79, 72, 10), // GK  替补门将
                CreatePlayer("李海龙",       25, PlayerPosition.DF, 76, 69, 40, 76, 74, 10), // DF  替补后卫
                CreatePlayer("穆斯塔帕",      4, PlayerPosition.DF, 70, 67, 38, 79, 74, 10), // DF  替补中卫（新援）
                CreatePlayer("颜卓彬",       15, PlayerPosition.MF, 73, 76, 69, 65, 73, 10), // MF  替补前卫（新援）
                CreatePlayer("吴星宇",       18, PlayerPosition.MF, 73, 76, 69, 65, 73, 10), // MF  替补前卫（新援）
                CreatePlayer("韦林顿",       10, PlayerPosition.FW, 77, 70, 77, 50, 75, 10), // FW  替补前锋
                CreatePlayer("姜宁",         19, PlayerPosition.FW, 74, 67, 74, 50, 72, 10)  // FW  替补前锋（老将）
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 11. 深圳新鹏城（TeamId=11，4-2-3-1，反击，蓝/白/橙）
        //     2026 阵容：主帅陈涛；夏窗首签加布里埃尔·哈维尔（25 岁巴西中卫，400 万欧身价）；
        //     蒂亚戈、姚均晟、杜月徵、李智、张卫、彭鹏。
        //     核心：加布里埃尔·哈维尔(78)、蒂亚戈(77)、埃杜(76)、姚均晟(76)
        // ====================================================================================
        private static void InitShenzhen()
        {
            // 创建球队：蓝色主场衣 / 白色客场衣 / 橙色门将衣
            TeamData team = CreateTeam(11, "深圳新鹏城", League.CSL,
                new Color(0.10f, 0.30f, 0.85f),  // 主场：蓝
                new Color(0.95f, 0.95f, 0.95f),  // 客场：白
                new Color(0.95f, 0.50f, 0.10f),  // 门将：橙
                FormationType.F4231, TacticStyle.Counter);

            // ---------- 首发 11 人（4-2-3-1 顺序：GK, LB, LCB, RCB, RB, LDM, RDM, LAM, CAM, RAM, ST）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("彭鹏",              1, PlayerPosition.GK, 52, 68, 25, 82, 75, 11), // GK  主力门将
                CreatePlayer("李智",              5, PlayerPosition.DF, 78, 71, 40, 78, 76, 11), // LB  左后卫
                CreatePlayer("加布里埃尔·哈维尔", 4, PlayerPosition.DF, 76, 73, 38, 85, 80, 11), // LCB 左中卫（夏窗新援外援，巴西中卫）
                CreatePlayer("杜月徵",            3, PlayerPosition.DF, 71, 68, 38, 80, 75, 11), // RCB 右中卫
                CreatePlayer("张卫",              2, PlayerPosition.DF, 78, 71, 40, 78, 76, 11), // RB  右后卫
                CreatePlayer("张煜",              6, PlayerPosition.MF, 73, 77, 67, 75, 75, 11), // LDM 左后腰
                CreatePlayer("姚均晟",            8, PlayerPosition.MF, 76, 80, 70, 78, 78, 11), // RDM 右后腰（新援）
                CreatePlayer("南松",             10, PlayerPosition.MF, 75, 78, 71, 67, 75, 11), // LAM 左前腰
                CreatePlayer("蒂亚戈",            7, PlayerPosition.MF, 79, 82, 75, 71, 79, 11), // CAM 中前腰（外援核心）
                CreatePlayer("王峤",             11, PlayerPosition.FW, 76, 69, 76, 50, 74, 11), // RAM 右前腰
                CreatePlayer("埃杜",              9, PlayerPosition.FW, 80, 73, 80, 50, 78, 11)  // ST  单前锋（外援）
            };

            // ---------- 替补 7 人 ----------
            // 注：杜月徵已首发，19 号替补由其他前锋顶替
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("季家葆",    22, PlayerPosition.GK, 50, 65, 25, 79, 72, 11), // GK  替补门将
                CreatePlayer("田子羿",     4, PlayerPosition.DF, 70, 67, 38, 79, 74, 11), // DF  替补中卫
                CreatePlayer("林敏",      25, PlayerPosition.DF, 69, 66, 38, 78, 73, 11), // DF  替补后卫
                CreatePlayer("陈正",      14, PlayerPosition.MF, 71, 75, 65, 73, 73, 11), // MF  替补中场
                CreatePlayer("胡家宝",    19, PlayerPosition.FW, 75, 68, 75, 50, 73, 11), // FW  替补前锋（顶替杜月徵替补位）
                CreatePlayer("申桓涛",    17, PlayerPosition.FW, 74, 67, 74, 50, 72, 11), // FW  替补前锋
                CreatePlayer("李宁",      21, PlayerPosition.FW, 74, 67, 74, 50, 72, 11)  // FW  替补前锋
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 12. 大连英博海发（TeamId=12，4-4-2，防守反击，蓝/白/黄）
        //     2026 阵容：斯坦丘(10)、马莱莱(11)、阿利米(4)、马马杜(2)四外援；
        //     朱鹏宇(16)、毛伟杰(22)；新援李昂(17)、罗竞(7)；隋维杰门将。
        //     核心：斯坦丘(80)、马莱莱(78)、马马杜(77)、阿利米(76)
        // ====================================================================================
        private static void InitDalianYingbo()
        {
            // 创建球队：蓝色主场衣 / 白色客场衣 / 黄色门将衣
            TeamData team = CreateTeam(12, "大连英博海发", League.CSL,
                new Color(0.10f, 0.30f, 0.85f),  // 主场：蓝
                new Color(0.95f, 0.95f, 0.95f),  // 客场：白
                new Color(0.95f, 0.85f, 0.10f),  // 门将：黄
                FormationType.F442, TacticStyle.DefensiveCounter);

            // ---------- 首发 11 人（4-4-2）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("隋维杰",    1, PlayerPosition.GK, 53, 69, 25, 83, 76, 12), // GK  主力门将（老将）
                CreatePlayer("吕鹏",      6, PlayerPosition.DF, 78, 71, 40, 78, 76, 12), // LB  左后卫
                CreatePlayer("马马杜",    2, PlayerPosition.DF, 75, 72, 38, 84, 79, 12), // LCB 左中卫（外援）
                CreatePlayer("宋岳",      5, PlayerPosition.DF, 71, 68, 38, 80, 75, 12), // RCB 右中卫
                CreatePlayer("和晓强",   17, PlayerPosition.DF, 77, 70, 40, 77, 75, 12), // RB  右后卫
                CreatePlayer("张华晨",    8, PlayerPosition.MF, 76, 79, 72, 68, 76, 12), // LM  左前卫
                CreatePlayer("毛伟杰",   22, PlayerPosition.MF, 73, 77, 67, 75, 75, 12), // LCM 左中前卫
                CreatePlayer("阿利米",    4, PlayerPosition.MF, 76, 80, 70, 78, 78, 12), // RCM 右中前卫（外援）
                CreatePlayer("罗竞",      7, PlayerPosition.MF, 76, 79, 72, 68, 76, 12), // RM  右前卫（新援）
                CreatePlayer("马莱莱",   11, PlayerPosition.FW, 82, 75, 82, 50, 80, 12), // LST 左前锋（外援）
                CreatePlayer("斯坦丘",   10, PlayerPosition.FW, 84, 77, 84, 50, 82, 12)  // RST 右前锋（外援核心）
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("于金永",    25, PlayerPosition.GK, 50, 65, 25, 79, 72, 12), // GK  替补门将
                CreatePlayer("李昂",      17, PlayerPosition.DF, 73, 68, 38, 80, 75, 12), // DF  替补中卫（新援）
                CreatePlayer("晋鹏翔",     3, PlayerPosition.DF, 70, 67, 38, 79, 74, 12), // DF  替补中卫
                CreatePlayer("朱鹏宇",   16, PlayerPosition.MF, 73, 77, 67, 75, 75, 12), // MF  替补中场
                CreatePlayer("杨铭锐",   27, PlayerPosition.FW, 75, 68, 75, 50, 73, 12), // FW  替补前锋
                CreatePlayer("彭顺杰",     5, PlayerPosition.FW, 75, 68, 75, 50, 73, 12), // FW  替补前锋
                CreatePlayer("闫奕涵",   14, PlayerPosition.FW, 74, 67, 74, 50, 72, 12)  // FW  替补前锋
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 13. 青岛西海岸（TeamId=13，4-2-3-1，反击，红/白/绿）
        //     2026 阵容：主帅郑智（助教黄博文、刘健、梅方）；新援梅米舍维奇(5)、雷森德(23,队史标王)、
        //     巴拉克(18)；保留卢斯、阿齐兹、戴维森；彭欣力、何小珂。
        //     核心：梅米舍维奇(78)、雷森德(78)、阿齐兹(77)、戴维森(76)
        // ====================================================================================
        private static void InitQingdaoXihai()
        {
            // 创建球队：红色主场衣 / 白色客场衣 / 绿色门将衣
            TeamData team = CreateTeam(13, "青岛西海岸", League.CSL,
                new Color(0.85f, 0.15f, 0.15f),  // 主场：红
                new Color(0.95f, 0.95f, 0.95f),  // 客场：白
                new Color(0.10f, 0.70f, 0.20f),  // 门将：绿
                FormationType.F4231, TacticStyle.Counter);

            // ---------- 首发 11 人（4-2-3-1）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("李昊",         16, PlayerPosition.GK, 52, 68, 25, 82, 75, 13), // GK  主力门将
                CreatePlayer("赵宏略",        3, PlayerPosition.DF, 78, 71, 40, 78, 76, 13), // LB  左后卫
                CreatePlayer("梅米舍维奇",    5, PlayerPosition.DF, 76, 73, 38, 85, 80, 13), // LCB 左中卫（新援外援核心）
                CreatePlayer("刘洋",          4, PlayerPosition.DF, 71, 68, 38, 80, 75, 13), // RCB 右中卫
                CreatePlayer("陈宇浩",        2, PlayerPosition.DF, 77, 70, 40, 77, 75, 13), // RB  右后卫
                CreatePlayer("雷森德",       23, PlayerPosition.MF, 78, 82, 72, 80, 80, 13), // LDM 左后腰（新援外援，队史标王）
                CreatePlayer("彭欣力",        8, PlayerPosition.MF, 75, 79, 69, 77, 77, 13), // RDM 右后腰（新援）
                CreatePlayer("何小珂",       10, PlayerPosition.MF, 76, 79, 72, 68, 76, 13), // LAM 左前腰（新援）
                CreatePlayer("段刘愚",        7, PlayerPosition.MF, 76, 79, 72, 68, 76, 13), // CAM 中前腰
                CreatePlayer("戴维森",       11, PlayerPosition.FW, 80, 73, 80, 50, 78, 13), // RAM 右前腰（外援）
                CreatePlayer("阿齐兹",        9, PlayerPosition.FW, 81, 74, 81, 50, 79, 13)  // ST  单前锋（外援核心）
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("刘世博",    26, PlayerPosition.GK, 50, 66, 25, 80, 73, 13), // GK  替补门将
                CreatePlayer("巴拉克",    18, PlayerPosition.DF, 71, 68, 38, 80, 75, 13), // DF  替补中卫（新援外援）
                CreatePlayer("董春雨",     1, PlayerPosition.DF, 69, 66, 38, 78, 73, 13), // DF  替补后卫
                CreatePlayer("卢斯",      22, PlayerPosition.MF, 77, 80, 73, 69, 77, 13), // MF  替补前卫（外援）
                CreatePlayer("张修维",    14, PlayerPosition.FW, 77, 70, 77, 50, 75, 13), // FW  替补前锋
                CreatePlayer("谭龙",      19, PlayerPosition.FW, 76, 69, 76, 50, 74, 13), // FW  替补前锋（老将）
                CreatePlayer("雷文杰",    21, PlayerPosition.FW, 75, 68, 75, 50, 73, 13)  // FW  替补前锋
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 14. 辽宁铁人（TeamId=14，4-4-2，防守反击，红/白/黑）
        //     2026 阵容：8 年后重返中超；新援严鼎皓(8)、邦本宜裕(10)、费利佩(15)、李提香(18)、田依浓(33)；
        //     前锋安以恩(7)、姆本扎(9)、陈彬彬(11)、臧一锋(14)、田玉达(17)、桂子涵(20)、热菲尼奥(47)。
        //     核心：邦本宜裕(77)、姆本扎(77)、安以恩(76)、严鼎皓(76)
        // ====================================================================================
        private static void InitLiaoningTieren()
        {
            // 创建球队：红色主场衣 / 白色客场衣 / 黑色门将衣
            TeamData team = CreateTeam(14, "辽宁铁人", League.CSL,
                new Color(0.85f, 0.10f, 0.10f),  // 主场：红
                new Color(0.95f, 0.95f, 0.95f),  // 客场：白
                new Color(0.10f, 0.10f, 0.10f),  // 门将：黑
                FormationType.F442, TacticStyle.DefensiveCounter);

            // ---------- 首发 11 人（4-4-2）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("刘伟国",     1, PlayerPosition.GK, 51, 67, 25, 81, 74, 14), // GK  主力门将
                CreatePlayer("张洪福",    34, PlayerPosition.DF, 76, 69, 40, 76, 74, 14), // LB  左后卫
                CreatePlayer("商隐",        3, PlayerPosition.DF, 71, 68, 38, 80, 75, 14), // LCB 左中卫
                CreatePlayer("孙捷",        5, PlayerPosition.DF, 71, 68, 38, 80, 75, 14), // RCB 右中卫
                CreatePlayer("吴俊杰",      2, PlayerPosition.DF, 76, 69, 40, 76, 74, 14), // RB  右后卫
                CreatePlayer("严鼎皓",      8, PlayerPosition.MF, 78, 81, 74, 70, 78, 14), // LM  左前卫（新援核心）
                CreatePlayer("邦本宜裕",   10, PlayerPosition.MF, 77, 81, 71, 79, 79, 14), // LCM 左中前卫（新援外援核心）
                CreatePlayer("李提香",     18, PlayerPosition.MF, 75, 79, 69, 77, 77, 14), // RCM 右中前卫（新援）
                CreatePlayer("田依浓",     33, PlayerPosition.MF, 76, 79, 72, 68, 76, 14), // RM  右前卫（新援）
                CreatePlayer("姆本扎",      9, PlayerPosition.FW, 81, 74, 81, 50, 79, 14), // LST 左前锋（外援核心）
                CreatePlayer("安以恩",      7, PlayerPosition.FW, 80, 73, 80, 50, 78, 14)  // RST 右前锋（外援）
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("庄佳杰",    22, PlayerPosition.GK, 50, 64, 25, 78, 71, 14), // GK  替补门将
                CreatePlayer("刘浪舟",     4, PlayerPosition.DF, 75, 68, 40, 75, 73, 14), // DF  替补后卫
                CreatePlayer("宋琛",       6, PlayerPosition.DF, 69, 66, 38, 78, 73, 14), // DF  替补后卫
                CreatePlayer("费利佩",    15, PlayerPosition.MF, 76, 79, 72, 68, 76, 14), // MF  替补前卫（新援外援）
                CreatePlayer("陈彬彬",    11, PlayerPosition.FW, 77, 70, 77, 50, 75, 14), // FW  替补前锋（新援）
                CreatePlayer("田玉达",    17, PlayerPosition.FW, 77, 70, 77, 50, 75, 14), // FW  替补前锋（新援）
                CreatePlayer("热菲尼奥",  47, PlayerPosition.FW, 78, 71, 78, 50, 76, 14)  // FW  替补前锋（外援）
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 15. 重庆铜梁龙（TeamId=15，4-4-2，反击，红/白/蓝）
        //     2026 阵容：主帅刘建业（本土少帅，2025 年 12 月 14 日官宣）；
        //     卡里略、莱昂纳多、萨达乌斯卡斯三名外援离队；新援岳瑞杰租借加盟；青年之师。
        //     核心：向余望(75)、尤尼(75)、黄希扬(74)、冯劲(74)
        // ====================================================================================
        private static void InitChongqing()
        {
            // 创建球队：红色主场衣 / 白色客场衣 / 蓝色门将衣
            TeamData team = CreateTeam(15, "重庆铜梁龙", League.CSL,
                new Color(0.85f, 0.10f, 0.10f),  // 主场：红
                new Color(0.95f, 0.95f, 0.95f),  // 客场：白
                new Color(0.10f, 0.30f, 0.85f),  // 门将：蓝
                FormationType.F442, TacticStyle.Counter);

            // ---------- 首发 11 人（4-4-2）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("陈钊",      1, PlayerPosition.GK, 51, 67, 25, 81, 74, 15), // GK  主力门将
                CreatePlayer("岳瑞杰",    3, PlayerPosition.DF, 75, 68, 40, 75, 73, 15), // LB  左后卫（新援租借，18 岁）
                CreatePlayer("龙成",      5, PlayerPosition.DF, 71, 68, 38, 80, 75, 15), // LCB 左中卫
                CreatePlayer("杨挺",      4, PlayerPosition.DF, 70, 67, 38, 79, 74, 15), // RCB 右中卫
                CreatePlayer("蒋哲",      2, PlayerPosition.DF, 77, 70, 40, 77, 75, 15), // RB  右后卫
                CreatePlayer("黄希扬",    6, PlayerPosition.MF, 76, 79, 72, 68, 76, 15), // LM  左前卫（老将）
                CreatePlayer("向余望",   10, PlayerPosition.MF, 75, 79, 69, 77, 77, 15), // LCM 左中前卫（核心）
                CreatePlayer("郑毅",      8, PlayerPosition.MF, 73, 77, 67, 75, 75, 15), // RCM 右中前卫
                CreatePlayer("冯劲",      7, PlayerPosition.MF, 76, 79, 72, 68, 76, 15), // RM  右前卫
                CreatePlayer("孙锡鹏",    9, PlayerPosition.FW, 77, 70, 77, 50, 75, 15), // LST 左前锋
                CreatePlayer("尤尼",     11, PlayerPosition.FW, 79, 72, 79, 50, 77, 15)  // RST 右前锋（外援核心）
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("王梓翔",   22, PlayerPosition.GK, 50, 64, 25, 78, 71, 15), // GK  替补门将
                CreatePlayer("汪士钦",   25, PlayerPosition.DF, 75, 68, 40, 75, 73, 15), // DF  替补后卫
                CreatePlayer("胡靖",      3, PlayerPosition.DF, 69, 66, 38, 78, 73, 15), // DF  替补后卫
                CreatePlayer("李智钊",   14, PlayerPosition.MF, 70, 74, 64, 72, 72, 15), // MF  替补前卫
                CreatePlayer("石笑天",   18, PlayerPosition.FW, 75, 68, 75, 50, 73, 15), // FW  替补前锋
                CreatePlayer("吴庆",     19, PlayerPosition.FW, 74, 67, 74, 50, 72, 15), // FW  替补前锋（老将）
                CreatePlayer("宋攀",     21, PlayerPosition.FW, 74, 67, 74, 50, 72, 15)  // FW  替补前锋
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 16. 云南玉昆（TeamId=16，4-2-3-1，防守反击，红/白/橙）
        //     2026 阵容：二年级中超；新援黄紫昌、徐新、邓函文、石柯、鲍亚雄、卡约；
        //     原有外援奥斯卡、约尼查、布尔克；候永永归化；韩子龙、叶楚贵。
        //     核心：奥斯卡(78)、约尼查(77)、黄紫昌(77)、徐新(76)、候永永(75)
        // ====================================================================================
        private static void InitYunnanYukun()
        {
            // 创建球队：红色主场衣 / 白色客场衣 / 橙色门将衣
            TeamData team = CreateTeam(16, "云南玉昆", League.CSL,
                new Color(0.85f, 0.15f, 0.15f),  // 主场：红
                new Color(0.95f, 0.95f, 0.95f),  // 客场：白
                new Color(0.95f, 0.50f, 0.10f),  // 门将：橙
                FormationType.F4231, TacticStyle.DefensiveCounter);

            // ---------- 首发 11 人（4-2-3-1）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("鲍亚雄",     1, PlayerPosition.GK, 53, 69, 25, 83, 76, 16), // GK  主力门将（新援）
                CreatePlayer("邓函文",     2, PlayerPosition.DF, 79, 72, 40, 79, 77, 16), // LB  左后卫（新援）
                CreatePlayer("石柯",       5, PlayerPosition.DF, 73, 70, 38, 82, 77, 16), // LCB 左中卫（新援）
                CreatePlayer("弋腾",      18, PlayerPosition.DF, 72, 69, 38, 81, 76, 16), // RCB 右中卫
                CreatePlayer("徐宏杰",      3, PlayerPosition.DF, 77, 70, 40, 77, 75, 16), // RB  右后卫
                CreatePlayer("徐新",       6, PlayerPosition.MF, 76, 80, 70, 78, 78, 16), // LDM 左后腰（新援）
                CreatePlayer("约尼查",    10, PlayerPosition.MF, 77, 81, 71, 79, 79, 16), // RDM 右后腰（外援核心）
                CreatePlayer("叶楚贵",      7, PlayerPosition.MF, 77, 80, 73, 69, 77, 16), // LAM 左前腰
                CreatePlayer("候永永",      8, PlayerPosition.MF, 77, 80, 73, 69, 77, 16), // CAM 中前腰（归化核心）
                CreatePlayer("黄紫昌",     11, PlayerPosition.FW, 81, 74, 81, 50, 79, 16), // RAM 右前腰（新援核心）
                CreatePlayer("奥斯卡",      9, PlayerPosition.FW, 82, 75, 82, 50, 80, 16)  // ST  单前锋（外援核心）
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("余鉴贤",   24, PlayerPosition.GK, 50, 65, 25, 79, 72, 16), // GK  替补门将
                CreatePlayer("李松益",    4, PlayerPosition.DF, 78, 71, 40, 78, 76, 16), // DF  替补后卫
                CreatePlayer("赵宇豪",    6, PlayerPosition.DF, 71, 68, 38, 80, 75, 16), // DF  替补中卫
                CreatePlayer("卡约",     14, PlayerPosition.MF, 76, 79, 72, 68, 76, 16), // MF  替补前卫（新援外援）
                CreatePlayer("韩子龙",   27, PlayerPosition.FW, 78, 71, 78, 50, 76, 16), // FW  替补前锋
                CreatePlayer("克莱伯",   19, PlayerPosition.FW, 79, 72, 79, 50, 77, 16), // FW  替补前锋（外援）
                CreatePlayer("布尔克",   23, PlayerPosition.FW, 78, 71, 78, 50, 76, 16)  // FW  替补前锋（外援）
            };

            SetLineup(team, starting, subs);
        }
    }
}
