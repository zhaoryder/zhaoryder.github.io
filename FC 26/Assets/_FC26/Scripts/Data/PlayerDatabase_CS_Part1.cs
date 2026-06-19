using UnityEngine;

namespace FC26.Data
{
    /// <summary>
    /// PlayerDatabase 中超联赛分部（Part1）。
    /// 本文件实现 <c>InitCSLeague_Part1()</c>，负责创建中超 2026 赛季前 8 支球队
    /// （TeamId 1 ~ 8）的完整大名单（每队 18 人：首发 11 + 替补 7），
    /// 并由主文件 CreateTeam 内部自动注册到 <c>_teams</c> 与 <c>_allTeams</c>。
    ///
    /// 辅助方法（由主文件 PlayerDatabase.cs 提供，本文件直接调用）：
    ///   - CreateTeam(...)      创建球队并返回 TeamData（内部已注册到全局字典/列表）
    ///   - CreatePlayer(...)    创建球员并返回 PlayerData
    ///   - SetLineup(...)       设置首发与替补
    ///
    /// 大名单摘要（TeamId 1-8，2026 赛季最新阵容）：
    /// ┌────┬────────────┬────────┬──────────────────┬──────────────────────────────────────┐
    /// │ ID │ 球队       │ 阵型   │ 战术             │ 核心球员                             │
    /// ├────┼────────────┼────────┼──────────────────┼──────────────────────────────────────┤
    /// │ 1  │ 上海海港   │ 4-3-3  │ Possession       │ 奥斯卡(85)、武磊(83)、巴尔加斯(82)   │
    /// │ 2  │ 上海申花   │ 4-4-2  │ DefensiveCounter │ 朱辰杰(82)、特谢拉(82)、吴曦(80)     │
    /// │ 3  │ 山东泰山   │ 4-4-2  │ Counter          │ 克雷桑(85)、卡扎伊什维利(82)、王大雷(83)│
    /// │ 4  │ 北京国安   │ 4-2-3-1│ Possession       │ 塞尔吉尼奥(82)、张稀哲(79)、法比奥(80)│
    /// │ 5  │ 成都蓉城   │ 4-3-3  │ HighPress        │ 罗慕洛(82)、韦世豪(82)、费利佩(80)   │
    /// │ 6  │ 武汉三镇   │ 4-2-3-1│ Counter          │ 陶强龙(76)、张晓彬(75)、刘越(74)     │
    /// │ 7  │ 浙江队     │ 4-2-3-1│ Possession       │ 米特里策(80)、托利奇(78)、赵博(78)   │
    /// │ 8  │ 天津津门虎 │ 4-4-2  │ Counter          │ 谢蒂内(79)、格劳(78)、王秋明(78)     │
    /// └────┴────────────┴────────┴──────────────────┴──────────────────────────────────────┘
    ///
    /// 能力值定位：中超球星 80-85，主力 74-80，替补 70-76。
    /// 每队 18 人配置：1 GK + 7 DF + 6 MF + 4 FW（位置分布随阵型略有差异）。
    /// CreatePlayer 参数顺序：name, number, pos, speed, passing, shooting, defense, stamina, teamId。
    /// 首发顺序严格对应 Formations.cs 中各阵型坐标顺序：
    ///   433:  GK, LB, LCB, RCB, RB, LCM, CM, RCM, LW, ST, RW
    ///   442:  GK, LB, LCB, RCB, RB, LM, LCM, RCM, RM, LST, RST
    ///   4231: GK, LB, LCB, RCB, RB, LDM, RDM, LAM, CAM, RAM, ST
    ///
    /// 2026 赛季动态要点（务必使用 2026 最新阵容，非 2024 旧数据）：
    ///   - 上海海港：加布里埃尔夏窗租借至深圳新鹏城（不在海港）；新援让·克劳德、安佩姆、
    ///              杨希、卢永涛、岳鑫、张源、安永佳入队；奥斯卡、武磊、巴尔加斯留队。
    ///   - 上海申花：五外援米内罗、特谢拉、马纳法、拉唐、盖伊；门将薛庆浩主力。
    ///   - 山东泰山：年轻化重构（00后占 20 人）；新援佩德罗·阿尔瓦罗；17 岁王庚睿主力左边卫。
    ///   - 北京国安：张稀哲队长 10 号；新援恩科洛洛、孔特、拉莫斯、贾非凡、王禹、
    ///              阿布都海米提、茹子楠、邓捷夫；法比奥、张玉宁前锋。
    ///   - 成都蓉城：蹇韬、刘殿座门将；韦世豪、费利佩、罗慕洛、韦林顿·席尔瓦；
    ///              新援冯卓毅回归 6 号；买提江；帅惟浩未进名单。
    ///   - 武汉三镇：大换血；主帅本杰明·莫拉；朴志洙/门德斯/帕拉西奥斯/图多列已离队；
    ///              新援李申圆、方镜淇、陈哲超；青训熊继政、王康、余田乐。
    ///   - 浙江队：赵博门将；米特里策前锋；王玉栋、方昊新星；新援托利奇、朴镇燮中场；
    ///            卢卡斯后卫；朴镇燮是 2026 中超唯一世界杯参赛球员。
    ///   - 天津津门虎：主帅于根伟；新援谢蒂内 7 号、格劳 5 号、科尔多瓦 18 号、齐雨熙、
    ///                季胜攀、乃博宁林、李嗣镕；基莱斯、哈达斯留队；萨尔瓦多未报名。
    /// </summary>
    public partial class PlayerDatabase
    {
        /// <summary>
        /// 初始化中超联赛前 8 队（TeamId 1-8）。
        /// 由主文件 PlayerDatabase 静态构造函数通过 partial 调用链触发。
        /// </summary>
        static partial void InitCSLeague_Part1()
        {
            InitShanghaiPort();       // 1  上海海港
            InitShanghaiShenhua();    // 2  上海申花
            InitShandongTaishan();    // 3  山东泰山
            InitBeijingGuoan();       // 4  北京国安
            InitChengduRongcheng();   // 5  成都蓉城
            InitWuhanThreeTown();     // 6  武汉三镇
            InitZhejiangFC();         // 7  浙江队
            InitTianjinTeda();        // 8  天津津门虎
        }

        // ====================================================================================
        // 1. 上海海港（TeamId=1，4-3-3，控球，红/白/绿）
        //     2026 阵容：奥斯卡、武磊、巴尔加斯留队；加布里埃尔夏窗租借至深圳新鹏城（不在海港）；
        //                新援：让·克劳德、安佩姆、杨希、卢永涛、岳鑫、张源、安永佳。
        //     核心：奥斯卡(85)、武磊(83)、巴尔加斯(82)、颜骏凌(82)
        // ====================================================================================
        private static void InitShanghaiPort()
        {
            // 创建球队：红色主场衣 / 白色客场衣 / 绿色门将衣
            TeamData team = CreateTeam(1, "上海海港", League.CSL,
                new Color(0.85f, 0.10f, 0.10f),  // 主场：红
                new Color(0.95f, 0.95f, 0.95f),  // 客场：白
                new Color(0.10f, 0.70f, 0.20f),  // 门将：绿
                FormationType.F433, TacticStyle.Possession);

            // ---------- 首发 11 人（4-3-3 顺序：GK, LB, LCB, RCB, RB, LCM, CM, RCM, LW, ST, RW）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("颜骏凌",   1, PlayerPosition.GK, 64, 70, 25, 88, 82, 1), // GK  主力门将（国脚）
                CreatePlayer("李昂",    17, PlayerPosition.DF, 76, 74, 40, 80, 78, 1), // LB  左后卫（新援回归）
                CreatePlayer("魏震",     5, PlayerPosition.DF, 78, 76, 40, 82, 80, 1), // LCB 左中卫
                CreatePlayer("张琳芃",   4, PlayerPosition.DF, 76, 74, 40, 80, 78, 1), // RCB 右中卫（老将）
                CreatePlayer("王振澳",  19, PlayerPosition.DF, 80, 72, 40, 78, 76, 1), // RB  右后卫
                CreatePlayer("张源",     6, PlayerPosition.MF, 72, 80, 70, 73, 77, 1), // LCM 左中前卫（新援）
                CreatePlayer("奥斯卡",   8, PlayerPosition.MF, 80, 89, 80, 82, 86, 1), // CM  中前卫（核心组织）
                CreatePlayer("徐新",    15, PlayerPosition.MF, 72, 80, 70, 73, 77, 1), // RCM 右中前卫
                CreatePlayer("武磊",     7, PlayerPosition.FW, 87, 79, 85, 50, 83, 1), // LW  左边锋（核心，国脚）
                CreatePlayer("巴尔加斯",10, PlayerPosition.FW, 84, 78, 84, 50, 82, 1), // ST  中锋（外援）
                CreatePlayer("安永佳",  11, PlayerPosition.FW, 77, 71, 77, 50, 75, 1)  // RW  右边锋（新援）
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("陈威",    22, PlayerPosition.GK, 56, 62, 25, 80, 74, 1), // GK  替补门将
                CreatePlayer("杨希",    23, PlayerPosition.DF, 73, 69, 38, 75, 73, 1), // DF  替补后卫（新援）
                CreatePlayer("岳鑫",    25, PlayerPosition.DF, 73, 69, 38, 75, 73, 1), // DF  替补后卫（新援）
                CreatePlayer("吾米提江", 3, PlayerPosition.DF, 72, 68, 38, 74, 72, 1), // DF  替补中卫
                CreatePlayer("卢永涛",  14, PlayerPosition.MF, 70, 78, 68, 71, 75, 1), // MF  替补中场（新援）
                CreatePlayer("蒯纪闻",  30, PlayerPosition.MF, 68, 76, 66, 69, 73, 1), // MF  替补前腰（青训）
                CreatePlayer("李圣龙",   9, PlayerPosition.FW, 78, 72, 78, 50, 76, 1)  // FW  替补前锋
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 2. 上海申花（TeamId=2，4-4-2，防守反击，蓝/白/橙）
        //     2026 阵容：五外援米内罗、特谢拉、马纳法、拉唐、盖伊；门将薛庆浩主力。
        //     核心：朱辰杰(82)、特谢拉(82)、吴曦(80)、马纳法(80)、米内罗(81)
        // ====================================================================================
        private static void InitShanghaiShenhua()
        {
            // 创建球队：蓝色主场衣 / 白色客场衣 / 橙色门将衣
            TeamData team = CreateTeam(2, "上海申花", League.CSL,
                new Color(0.10f, 0.20f, 0.85f),  // 主场：蓝
                new Color(0.95f, 0.95f, 0.95f),  // 客场：白
                new Color(0.95f, 0.50f, 0.10f),  // 门将：橙
                FormationType.F442, TacticStyle.DefensiveCounter);

            // ---------- 首发 11 人（4-4-2 顺序：GK, LB, LCB, RCB, RB, LM, LCM, RCM, RM, LST, RST）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("薛庆浩",   1, PlayerPosition.GK, 60, 66, 25, 84, 78, 2), // GK  主力门将
                CreatePlayer("陈晋一",  27, PlayerPosition.DF, 78, 72, 40, 78, 76, 2), // LB  左后卫
                CreatePlayer("朱辰杰",   5, PlayerPosition.DF, 80, 78, 40, 84, 82, 2), // LCB 左中卫（核心，国脚）
                CreatePlayer("金顺凯",   3, PlayerPosition.DF, 74, 70, 40, 76, 74, 2), // RCB 右中卫
                CreatePlayer("马纳法",  13, PlayerPosition.DF, 84, 76, 42, 82, 80, 2), // RB  右后卫（外援）
                CreatePlayer("吴曦",    15, PlayerPosition.MF, 76, 84, 74, 78, 81, 2), // LM  左前卫（核心，老队长）
                CreatePlayer("高天意",  17, PlayerPosition.MF, 74, 82, 72, 75, 79, 2), // LCM 左中前卫
                CreatePlayer("特谢拉",  10, PlayerPosition.MF, 78, 86, 76, 79, 83, 2), // RCM 右中前卫（外援核心）
                CreatePlayer("于汉超",  22, PlayerPosition.MF, 74, 80, 72, 73, 77, 2), // RM  右前卫（老将）
                CreatePlayer("米内罗",   9, PlayerPosition.FW, 83, 77, 83, 50, 81, 2), // LST 左前锋（外援）
                CreatePlayer("路易斯",  11, PlayerPosition.FW, 81, 75, 81, 50, 79, 2)  // RST 右前锋
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("鲍亚雄",  30, PlayerPosition.GK, 57, 63, 25, 81, 75, 2), // GK  替补门将
                CreatePlayer("杨帅",    26, PlayerPosition.DF, 74, 70, 40, 76, 74, 2), // DF  替补中卫
                CreatePlayer("杨泽翔",  16, PlayerPosition.DF, 73, 69, 38, 75, 73, 2), // DF  替补后卫
                CreatePlayer("谢鹏飞",  30, PlayerPosition.MF, 72, 80, 70, 73, 77, 2), // MF  替补前腰（国脚）
                CreatePlayer("拉唐",     8, PlayerPosition.MF, 72, 80, 70, 73, 77, 2), // MF  替补中场（新援外援）
                CreatePlayer("盖伊",     7, PlayerPosition.FW, 77, 71, 77, 50, 75, 2), // FW  替补前锋（新援外援）
                CreatePlayer("刘诚宇",  39, PlayerPosition.FW, 74, 68, 74, 50, 72, 2)  // FW  替补前锋（青训）
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 3. 山东泰山（TeamId=3，4-4-2，反击，橙/蓝/黑）
        //     2026 阵容：年轻化重构（00后占 20 人）；新援佩德罗·阿尔瓦罗；17 岁王庚睿主力左边卫。
        //     核心：克雷桑(85)、卡扎伊什维利(82)、王大雷(83)、郑铮(80)
        // ====================================================================================
        private static void InitShandongTaishan()
        {
            // 创建球队：橙色主场衣 / 蓝色客场衣 / 黑色门将衣
            TeamData team = CreateTeam(3, "山东泰山", League.CSL,
                new Color(0.90f, 0.45f, 0.10f),  // 主场：橙
                new Color(0.10f, 0.20f, 0.85f),  // 客场：蓝
                new Color(0.10f, 0.10f, 0.10f),  // 门将：黑
                FormationType.F442, TacticStyle.Counter);

            // ---------- 首发 11 人（4-4-2）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("王大雷",          14, PlayerPosition.GK, 65, 71, 25, 89, 83, 3), // GK  主力门将（核心，国脚）
                CreatePlayer("王庚睿",          17, PlayerPosition.DF, 74, 68, 38, 74, 72, 3), // LB  左后卫（17 岁主力）
                CreatePlayer("郑铮",             4, PlayerPosition.DF, 78, 76, 40, 82, 80, 3), // LCB 左中卫（老将）
                CreatePlayer("佩德罗·阿尔瓦罗", 23, PlayerPosition.DF, 76, 74, 40, 80, 78, 3), // RCB 右中卫（新援外援）
                CreatePlayer("刘洋",             2, PlayerPosition.DF, 78, 74, 40, 80, 78, 3), // RB  右后卫（国脚）
                CreatePlayer("谢文能",          25, PlayerPosition.MF, 74, 82, 72, 75, 79, 3), // LM  左前卫
                CreatePlayer("陈蒲",            21, PlayerPosition.MF, 73, 81, 71, 74, 78, 3), // LCM 左中前卫
                CreatePlayer("段刘愚",          16, PlayerPosition.MF, 71, 79, 69, 72, 76, 3), // RCM 右中前卫（青训）
                CreatePlayer("刘彬彬",          11, PlayerPosition.MF, 78, 80, 72, 73, 77, 3), // RM  右前卫（速度型）
                CreatePlayer("克雷桑",           9, PlayerPosition.FW, 87, 81, 87, 50, 85, 3), // LST 左前锋（超级核心，外援）
                CreatePlayer("卡扎伊什维利",    10, PlayerPosition.FW, 84, 78, 84, 50, 82, 3)  // RST 右前锋（外援）
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("于金永",   1, PlayerPosition.GK, 55, 61, 25, 79, 73, 3), // GK  替补门将
                CreatePlayer("高准翼",   5, PlayerPosition.DF, 78, 74, 40, 80, 78, 3), // DF  替补后卫（国脚）
                CreatePlayer("彭啸",    31, PlayerPosition.DF, 72, 68, 38, 74, 72, 3), // DF  替补中卫（青训）
                CreatePlayer("史松宸",  33, PlayerPosition.MF, 68, 76, 66, 69, 73, 3), // MF  替补中场（青训）
                CreatePlayer("泽卡",    19, PlayerPosition.FW, 82, 76, 82, 50, 80, 3), // FW  替补前锋（外援）
                CreatePlayer("买乌郎",  29, PlayerPosition.FW, 75, 69, 75, 50, 73, 3), // FW  替补前锋（青训）
                CreatePlayer("杨展彭",  28, PlayerPosition.FW, 73, 67, 73, 50, 71, 3)  // FW  替补前锋（中场轮换）
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 4. 北京国安（TeamId=4，4-2-3-1，控球，绿/白/黄）
        //     2026 阵容：张稀哲队长 10 号；新援恩科洛洛、孔特、拉莫斯、贾非凡、王禹、
        //                阿布都海米提、茹子楠、邓捷夫；法比奥、张玉宁前锋。
        //     核心：塞尔吉尼奥(82)、法比奥(80)、恩科洛洛(80)、张稀哲(79)、拉莫斯(79)
        // ====================================================================================
        private static void InitBeijingGuoan()
        {
            // 创建球队：绿色主场衣 / 白色客场衣 / 黄色门将衣
            TeamData team = CreateTeam(4, "北京国安", League.CSL,
                new Color(0.10f, 0.60f, 0.20f),  // 主场：绿
                new Color(0.95f, 0.95f, 0.95f),  // 客场：白
                new Color(0.95f, 0.85f, 0.10f),  // 门将：黄
                FormationType.F4231, TacticStyle.Possession);

            // ---------- 首发 11 人（4-2-3-1 顺序：GK, LB, LCB, RCB, RB, LDM, RDM, LAM, CAM, RAM, ST）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("侯森",         1, PlayerPosition.GK, 60, 66, 25, 84, 78, 4), // GK  主力门将
                CreatePlayer("李磊",         4, PlayerPosition.DF, 78, 74, 40, 80, 78, 4), // LB  左后卫（国脚）
                CreatePlayer("拉莫斯",       5, PlayerPosition.DF, 79, 75, 40, 81, 79, 4), // LCB 左中卫（新援外援）
                CreatePlayer("阿不都海米提",24, PlayerPosition.DF, 75, 71, 40, 77, 75, 4), // RCB 右中卫（新援）
                CreatePlayer("茹子楠",      17, PlayerPosition.DF, 78, 72, 40, 78, 76, 4), // RB  右后卫（新援）
                CreatePlayer("恩科洛洛",     6, PlayerPosition.MF, 76, 84, 74, 78, 81, 4), // LDM 左后腰（新援外援）
                CreatePlayer("塞尔吉尼奥",  10, PlayerPosition.MF, 78, 86, 76, 79, 83, 4), // RDM 右后腰（核心，归化国脚）
                CreatePlayer("达万",         8, PlayerPosition.MF, 74, 82, 72, 75, 79, 4), // LAM 左前腰
                CreatePlayer("张稀哲",      10, PlayerPosition.MF, 75, 83, 73, 76, 80, 4), // CAM 中前腰（队长）
                CreatePlayer("杨立瑜",      29, PlayerPosition.MF, 74, 80, 72, 73, 77, 4), // RAM 右前腰
                CreatePlayer("法比奥",      11, PlayerPosition.FW, 82, 76, 82, 50, 80, 4)  // ST  单前锋（外援）
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("韩佳奇",  22, PlayerPosition.GK, 57, 63, 25, 81, 75, 4), // GK  替补门将（国脚）
                CreatePlayer("王刚",    27, PlayerPosition.DF, 78, 72, 40, 78, 76, 4), // DF  替补右后卫（老将）
                CreatePlayer("贾非凡",  32, PlayerPosition.DF, 73, 69, 38, 75, 73, 4), // DF  替补后卫（新援）
                CreatePlayer("王禹",    14, PlayerPosition.MF, 69, 77, 67, 70, 74, 4), // MF  替补中场（新援）
                CreatePlayer("张玉宁",   9, PlayerPosition.MF, 76, 84, 78, 78, 81, 4), // MF  替补中场/前锋（核心，国脚）
                CreatePlayer("方昊",     7, PlayerPosition.FW, 76, 70, 76, 50, 74, 4), // FW  替补前锋
                CreatePlayer("邓捷夫",  38, PlayerPosition.FW, 72, 66, 72, 50, 70, 4)  // FW  替补前锋（新援）
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 5. 成都蓉城（TeamId=5，4-3-3，高位压迫，红/黑/蓝）
        //     2026 阵容：蹇韬、刘殿座门将；韦世豪、费利佩、罗慕洛、韦林顿·席尔瓦；
        //                新援冯卓毅回归 6 号；买提江；帅惟浩未进名单。
        //     核心：罗慕洛(82)、韦世豪(82)、费利佩(80)、刘殿座(80)
        // ====================================================================================
        private static void InitChengduRongcheng()
        {
            // 创建球队：红色主场衣 / 黑色客场衣 / 蓝色门将衣
            TeamData team = CreateTeam(5, "成都蓉城", League.CSL,
                new Color(0.85f, 0.15f, 0.15f),  // 主场：红
                new Color(0.10f, 0.10f, 0.10f),  // 客场：黑
                new Color(0.10f, 0.30f, 0.85f),  // 门将：蓝
                FormationType.F433, TacticStyle.HighPress);

            // ---------- 首发 11 人（4-3-3 顺序：GK, LB, LCB, RCB, RB, LCM, CM, RCM, LW, ST, RW）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("蹇韬",          1, PlayerPosition.GK, 60, 66, 25, 84, 78, 5), // GK  主力门将（国脚）
                CreatePlayer("胡荷韬",        2, PlayerPosition.DF, 78, 72, 40, 78, 76, 5), // LB  左后卫
                CreatePlayer("韩鹏飞",       18, PlayerPosition.DF, 78, 74, 40, 80, 78, 5), // LCB 左中卫
                CreatePlayer("亚历斯祖",      3, PlayerPosition.DF, 78, 74, 40, 80, 78, 5), // RCB 右中卫（外援）
                CreatePlayer("王东升",       17, PlayerPosition.DF, 77, 71, 40, 77, 75, 5), // RB  右后卫
                CreatePlayer("买提江",       14, PlayerPosition.MF, 74, 82, 72, 75, 79, 5), // LCM 左中前卫
                CreatePlayer("罗慕洛",       10, PlayerPosition.MF, 78, 86, 76, 79, 83, 5), // CM  中前卫（核心，外援）
                CreatePlayer("冯卓毅",        6, PlayerPosition.MF, 71, 79, 69, 72, 76, 5), // RCM 右中前卫（新援回归）
                CreatePlayer("韦世豪",        7, PlayerPosition.FW, 84, 78, 84, 50, 82, 5), // LW  左边锋（核心，国脚）
                CreatePlayer("费利佩",        9, PlayerPosition.FW, 82, 76, 82, 50, 80, 5), // ST  中锋（外援）
                CreatePlayer("韦林顿·席尔瓦",11, PlayerPosition.FW, 81, 75, 81, 50, 79, 5)  // RW  右边锋（外援）
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("刘殿座",  16, PlayerPosition.GK, 62, 68, 25, 86, 80, 5), // GK  替补门将（国脚）
                CreatePlayer("贺一然",   4, PlayerPosition.DF, 73, 69, 38, 75, 73, 5), // DF  替补后卫
                CreatePlayer("董岩鋆",  19, PlayerPosition.DF, 73, 69, 38, 75, 73, 5), // DF  替补中卫
                CreatePlayer("杨明洋",  16, PlayerPosition.MF, 71, 79, 69, 72, 76, 5), // MF  替补中场
                CreatePlayer("茹萨",     5, PlayerPosition.MF, 72, 80, 70, 73, 77, 5), // MF  替补中场（外援）
                CreatePlayer("唐创",    21, PlayerPosition.FW, 74, 68, 74, 50, 72, 5), // FW  替补前锋
                CreatePlayer("罗慕洛替补",8, PlayerPosition.FW, 76, 70, 76, 50, 74, 5)  // FW  替补前锋
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 6. 武汉三镇（TeamId=6，4-2-3-1，反击，橙/蓝/绿）
        //     2026 阵容：大换血；主帅本杰明·莫拉；朴志洙/门德斯/帕拉西奥斯/图多列已离队；
        //                新援李申圆、方镜淇、陈哲超；青训熊继政、王康、余田乐。
        //     核心：陶强龙(76)、张晓彬(75)、方镜淇(75)、李申圆(75)
        // ====================================================================================
        private static void InitWuhanThreeTown()
        {
            // 创建球队：橙色主场衣 / 蓝色客场衣 / 绿色门将衣
            TeamData team = CreateTeam(6, "武汉三镇", League.CSL,
                new Color(0.95f, 0.50f, 0.10f),  // 主场：橙
                new Color(0.10f, 0.20f, 0.85f),  // 客场：蓝
                new Color(0.10f, 0.70f, 0.20f),  // 门将：绿
                FormationType.F4231, TacticStyle.Counter);

            // ---------- 首发 11 人（4-2-3-1 顺序：GK, LB, LCB, RCB, RB, LDM, RDM, LAM, CAM, RAM, ST）----------
            // 注：朴志洙已离队，用新中卫王康（青训）顶替；刘越单前锋。
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("方镜淇",   1, PlayerPosition.GK, 57, 63, 25, 81, 75, 6), // GK  主力门将（新援）
                CreatePlayer("陈哲超",   5, PlayerPosition.DF, 74, 70, 40, 76, 74, 6), // LB  左后卫（新援）
                CreatePlayer("朴志洙替补",4, PlayerPosition.DF, 73, 69, 38, 75, 73, 6), // LCB 左中卫（新中卫顶替离队的朴志洙）
                CreatePlayer("王康",    25, PlayerPosition.DF, 72, 68, 38, 74, 72, 6), // RCB 右中卫（青训）
                CreatePlayer("李申圆",  17, PlayerPosition.DF, 77, 71, 40, 77, 75, 6), // RB  右后卫（新援）
                CreatePlayer("熊继政",   8, PlayerPosition.MF, 68, 76, 66, 69, 73, 6), // LDM 左后腰（青训）
                CreatePlayer("余田乐",   6, PlayerPosition.MF, 67, 75, 65, 68, 72, 6), // RDM 右后腰（青训）
                CreatePlayer("何超",    15, PlayerPosition.MF, 70, 78, 68, 71, 75, 6), // LAM 左前腰
                CreatePlayer("张晓彬",  10, PlayerPosition.MF, 71, 79, 69, 72, 76, 6), // CAM 中前腰
                CreatePlayer("陶强龙",  11, PlayerPosition.MF, 74, 80, 72, 73, 77, 6), // RAM 右前腰（核心，国脚）
                CreatePlayer("刘越",     9, PlayerPosition.FW, 76, 70, 76, 50, 74, 6)  // ST  单前锋
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("郭通",    22, PlayerPosition.GK, 52, 58, 25, 76, 70, 6), // GK  替补门将
                CreatePlayer("刘奕鸣",   3, PlayerPosition.DF, 74, 70, 40, 76, 74, 6), // DF  替补中卫（老将）
                CreatePlayer("任航",     5, PlayerPosition.DF, 72, 68, 38, 74, 72, 6), // DF  替补中卫（老将）
                CreatePlayer("王毅",    14, PlayerPosition.MF, 67, 75, 65, 68, 72, 6), // MF  替补中场
                CreatePlayer("姜至鹏",   7, PlayerPosition.FW, 75, 69, 75, 50, 73, 6), // FW  替补前锋（老将）
                CreatePlayer("肖开提",  18, PlayerPosition.FW, 72, 66, 72, 50, 70, 6), // FW  替补前锋（青训）
                CreatePlayer("阿齐兹",  19, PlayerPosition.FW, 75, 69, 75, 50, 73, 6)  // FW  替补前锋（外援）
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 7. 浙江队（TeamId=7，4-2-3-1，控球，绿/白/橙）
        //     2026 阵容：赵博门将；米特里策前锋；王玉栋、方昊新星；新援托利奇、朴镇燮中场；
        //                卢卡斯后卫；朴镇燮是 2026 中超唯一世界杯参赛球员。
        //     核心：米特里策(80)、托利奇(78)、卢卡斯(78)、赵博(78)、莱昂纳多(78)
        // ====================================================================================
        private static void InitZhejiangFC()
        {
            // 创建球队：绿色主场衣 / 白色客场衣 / 橙色门将衣
            TeamData team = CreateTeam(7, "浙江队", League.CSL,
                new Color(0.10f, 0.70f, 0.30f),  // 主场：绿
                new Color(0.95f, 0.95f, 0.95f),  // 客场：白
                new Color(0.95f, 0.50f, 0.10f),  // 门将：橙
                FormationType.F4231, TacticStyle.Possession);

            // ---------- 首发 11 人（4-2-3-1 顺序：GK, LB, LCB, RCB, RB, LDM, RDM, LAM, CAM, RAM, ST）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("赵博",     1, PlayerPosition.GK, 60, 66, 25, 84, 78, 7), // GK  主力门将
                CreatePlayer("刘浩帆",   3, PlayerPosition.DF, 77, 71, 40, 77, 75, 7), // LB  左后卫
                CreatePlayer("卢卡斯",   4, PlayerPosition.DF, 78, 74, 40, 80, 78, 7), // LCB 左中卫（外援）
                CreatePlayer("汪仕钦",   5, PlayerPosition.DF, 74, 70, 40, 76, 74, 7), // RCB 右中卫
                CreatePlayer("董宇",     2, PlayerPosition.DF, 74, 70, 40, 76, 74, 7), // RB  右后卫
                CreatePlayer("托利奇",   8, PlayerPosition.MF, 74, 82, 72, 75, 79, 7), // LDM 左后腰（新援外援）
                CreatePlayer("朴镇燮",  10, PlayerPosition.MF, 72, 80, 70, 73, 77, 7), // RDM 右后腰（新援外援，韩国国脚，唯一世界杯参赛球员）
                CreatePlayer("程进",     7, PlayerPosition.MF, 72, 80, 70, 73, 77, 7), // LAM 左前腰
                CreatePlayer("姚均晟",   6, PlayerPosition.MF, 71, 79, 69, 72, 76, 7), // CAM 中前腰
                CreatePlayer("方昊",    11, PlayerPosition.FW, 77, 71, 77, 50, 75, 7), // RAM 右前腰（新援）
                CreatePlayer("米特里策", 9, PlayerPosition.FW, 82, 76, 82, 50, 80, 7)  // ST  单前锋（外援）
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("顾超",     22, PlayerPosition.GK, 57, 63, 25, 81, 75, 7), // GK  替补门将（老将）
                CreatePlayer("高迪",     17, PlayerPosition.DF, 73, 69, 38, 75, 73, 7), // DF  替补后卫
                CreatePlayer("梁诺恒",   25, PlayerPosition.DF, 73, 69, 38, 75, 73, 7), // DF  替补中卫
                CreatePlayer("王玉栋",   39, PlayerPosition.MF, 70, 78, 68, 71, 75, 7), // MF  替补中场（新星）
                CreatePlayer("莱昂纳多", 10, PlayerPosition.FW, 80, 74, 80, 50, 78, 7), // FW  替补前锋（外援）
                CreatePlayer("鲍盛鑫",   19, PlayerPosition.FW, 73, 67, 73, 50, 71, 7), // FW  替补前锋（青训）
                CreatePlayer("宁方泽",   21, PlayerPosition.FW, 73, 67, 73, 50, 71, 7)  // FW  替补前锋（青训）
            };

            SetLineup(team, starting, subs);
        }

        // ====================================================================================
        // 8. 天津津门虎（TeamId=8，4-4-2，反击，紫/白/黄）
        //     2026 阵容：主帅于根伟；新援谢蒂内 7 号、格劳 5 号、科尔多瓦 18 号、齐雨熙、
        //                季胜攀、乃博宁林、李嗣镕；基莱斯、哈达斯留队；萨尔瓦多未报名。
        //     核心：谢蒂内(79)、格劳(78)、王秋明(78)、科尔多瓦(78)、基莱斯(78)
        // ====================================================================================
        private static void InitTianjinTeda()
        {
            // 创建球队：紫色主场衣 / 白色客场衣 / 黄色门将衣
            TeamData team = CreateTeam(8, "天津津门虎", League.CSL,
                new Color(0.50f, 0.10f, 0.60f),  // 主场：紫
                new Color(0.95f, 0.95f, 0.95f),  // 客场：白
                new Color(0.95f, 0.85f, 0.10f),  // 门将：黄
                FormationType.F442, TacticStyle.Counter);

            // ---------- 首发 11 人（4-4-2 顺序：GK, LB, LCB, RCB, RB, LM, LCM, RCM, RM, LST, RST）----------
            PlayerData[] starting = new PlayerData[]
            {
                CreatePlayer("齐雨熙",  21, PlayerPosition.GK, 57, 63, 25, 81, 75, 8), // GK  主力门将（新援）
                CreatePlayer("孙铭谦",  31, PlayerPosition.DF, 74, 70, 40, 76, 74, 8), // LB  左后卫
                CreatePlayer("科尔多瓦",18, PlayerPosition.DF, 78, 74, 40, 80, 78, 8), // LCB 左中卫（新援外援）
                CreatePlayer("杨帆",     4, PlayerPosition.DF, 76, 72, 40, 78, 76, 8), // RCB 右中卫
                CreatePlayer("吴兴涵",  17, PlayerPosition.DF, 78, 72, 40, 78, 76, 8), // RB  右后卫
                CreatePlayer("格劳",     5, PlayerPosition.MF, 76, 82, 74, 75, 79, 8), // LM  左前卫（新援外援）
                CreatePlayer("王秋明",  10, PlayerPosition.MF, 74, 82, 72, 75, 79, 8), // LCM 左中前卫（核心，队长）
                CreatePlayer("哈达斯",   8, PlayerPosition.MF, 73, 81, 71, 74, 78, 8), // RCM 右中前卫（外援）
                CreatePlayer("巴顿",    14, PlayerPosition.MF, 74, 80, 72, 73, 77, 8), // RM  右前卫
                CreatePlayer("谢蒂内",   7, PlayerPosition.FW, 81, 75, 81, 50, 79, 8), // LST 左前锋（新援外援）
                CreatePlayer("基莱斯",   9, PlayerPosition.FW, 80, 74, 80, 50, 78, 8)  // RST 右前锋（外援）
            };

            // ---------- 替补 7 人 ----------
            PlayerData[] subs = new PlayerData[]
            {
                CreatePlayer("闫炳良",  25, PlayerPosition.GK, 54, 60, 25, 78, 72, 8), // GK  替补门将
                CreatePlayer("王政豪",   3, PlayerPosition.DF, 72, 68, 38, 74, 72, 8), // DF  替补后卫
                CreatePlayer("李嗣镕",  27, PlayerPosition.DF, 71, 67, 38, 73, 71, 8), // DF  替补后卫（新援）
                CreatePlayer("谢维军",  11, PlayerPosition.MF, 70, 78, 68, 71, 75, 8), // MF  替补中场
                CreatePlayer("季胜攀",  15, PlayerPosition.MF, 68, 76, 66, 69, 73, 8), // MF  替补中场（新援）
                CreatePlayer("乃博宁林",23, PlayerPosition.FW, 72, 66, 72, 50, 70, 8), // FW  替补前锋（新援）
                CreatePlayer("蔡承峻",  39, PlayerPosition.FW, 72, 66, 72, 50, 70, 8)  // FW  替补前锋（青训）
            };

            SetLineup(team, starting, subs);
        }
    }
}
