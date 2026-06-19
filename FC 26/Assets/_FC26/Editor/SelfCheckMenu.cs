using UnityEditor;
using UnityEngine;

namespace FC26.Editor
{
    /// <summary>
    /// 工程自检菜单（Editor 工具）。
    /// <para>提供一键检查关键系统是否存在与编译通过的功能，</para>
    /// <para>便于在工程搭建后快速验证各模块完整性。</para>
    ///
    /// 使用方式：
    ///   - 菜单栏 → FC26 → 自检 → 检查关键系统
    ///   - 菜单栏 → FC26 → 自检 → 生成目录结构
    ///   - 菜单栏 → FC26 → 自检 → 输出运行说明
    /// </summary>
    public static class SelfCheckMenu
    {
        // 菜单路径常量
        private const string MenuRoot = "FC26/自检/";

        /// <summary>
        /// 检查关键系统是否存在与编译通过。
        /// <para>通过反射检查各模块核心类型是否存在，并输出检查报告到控制台。</para>
        /// </summary>
        [MenuItem(MenuRoot + "检查关键系统", priority = 0)]
        public static void CheckKeySystems()
        {
            Debug.Log("========== FC26 工程自检开始 ==========");

            int passCount = 0;
            int failCount = 0;

            // ===== Core 模块 =====
            passCount += CheckType("FC26.Core.GameManager", "Core/GameManager");
            passCount += CheckType("FC26.Core.GameStateMachine", "Core/GameStateMachine");
            passCount += CheckType("FC26.Core.EventBus", "Core/EventBus");
            passCount += CheckType("FC26.Core.ServiceLocator", "Core/ServiceLocator");
            passCount += CheckType("FC26.Core.MonoSingleton`1", "Core/MonoSingleton<T>");
            passCount += CheckType("FC26.Core.SceneBootstrapper", "Core/SceneBootstrapper");

            // ===== Data 模块 =====
            passCount += CheckType("FC26.Data.PlayerData", "Data/PlayerData");
            passCount += CheckType("FC26.Data.TeamData", "Data/TeamData");
            passCount += CheckType("FC26.Data.LeagueData", "Data/LeagueData");
            passCount += CheckType("FC26.Data.PlayerDatabase", "Data/PlayerDatabase");
            passCount += CheckType("FC26.Data.Formations", "Data/Formations");
            passCount += CheckType("FC26.Data.Tactics", "Data/Tactics");

            // ===== Input 模块 =====
            passCount += CheckType("FC26.Input.InputActions", "Input/InputActions");
            passCount += CheckType("FC26.Input.InputReader", "Input/InputReader");
            passCount += CheckType("FC26.Input.KeyBindings", "Input/KeyBindings");
            passCount += CheckType("FC26.Input.PlatformAdapter", "Input/PlatformAdapter");

            // ===== Camera 模块 =====
            passCount += CheckType("FC26.Camera.MatchCamera", "Camera/MatchCamera");
            passCount += CheckType("FC26.Camera.CameraUtility", "Camera/CameraUtility");

            // ===== Ball 模块 =====
            passCount += CheckType("FC26.Ball.BallEntity", "Ball/BallEntity");
            passCount += CheckType("FC26.Ball.BallPhysics", "Ball/BallPhysics");
            passCount += CheckType("FC26.Ball.BallManager", "Ball/BallManager");

            // ===== Player 模块 =====
            passCount += CheckType("FC26.Player.PlayerEntity", "Player/PlayerEntity");
            passCount += CheckType("FC26.Player.PlayerController", "Player/PlayerController");
            passCount += CheckType("FC26.Player.PlayerStateMachine", "Player/PlayerStateMachine");
            passCount += CheckType("FC26.Player.PlayerAnimator", "Player/PlayerAnimator");
            passCount += CheckType("FC26.Player.PlayerFactory", "Player/PlayerFactory");

            // ===== AI 模块 =====
            passCount += CheckType("FC26.AI.AIDecisionCore", "AI/AIDecisionCore");
            passCount += CheckType("FC26.AI.AIStateMachine", "AI/AIStateMachine");
            passCount += CheckType("FC26.AI.AIController", "AI/AIController");
            passCount += CheckType("FC26.AI.AIDifficulty", "AI/AIDifficulty");

            // ===== Match 模块 =====
            passCount += CheckType("FC26.Match.MatchManager", "Match/MatchManager");
            passCount += CheckType("FC26.Match.MatchTimer", "Match/MatchTimer");
            passCount += CheckType("FC26.Match.MatchStatistics", "Match/MatchStatistics");
            passCount += CheckType("FC26.Match.KickOffController", "Match/KickOffController");

            // ===== Referee 模块 =====
            passCount += CheckType("FC26.Referee.RefereeManager", "Referee/RefereeManager");
            passCount += CheckType("FC26.Referee.OffsideChecker", "Referee/OffsideChecker");
            passCount += CheckType("FC26.Referee.FoulChecker", "Referee/FoulChecker");
            passCount += CheckType("FC26.Referee.DisciplineSystem", "Referee/DisciplineSystem");
            passCount += CheckType("FC26.Referee.SetPieceController", "Referee/SetPieceController");

            // ===== Stadium 模块 =====
            passCount += CheckType("FC26.Stadium.StadiumBuilder", "Stadium/StadiumBuilder");
            passCount += CheckType("FC26.Stadium.GoalAndProps", "Stadium/GoalAndProps");
            passCount += CheckType("FC26.Stadium.CrowdController", "Stadium/CrowdController");
            passCount += CheckType("FC26.Stadium.RefereeNPC", "Stadium/RefereeNPC");
            passCount += CheckType("FC26.Stadium.KitMaterial", "Stadium/KitMaterial");

            // ===== UI 模块 =====
            passCount += CheckType("FC26.UI.UIBase", "UI/UIBase");
            passCount += CheckType("FC26.UI.UIManager", "UI/UIManager");
            passCount += CheckType("FC26.UI.MainMenuUI", "UI/MainMenuUI");
            passCount += CheckType("FC26.UI.TeamSelectUI", "UI/TeamSelectUI");
            passCount += CheckType("FC26.UI.LineupUI", "UI/LineupUI");
            passCount += CheckType("FC26.UI.MatchHUD", "UI/MatchHUD");
            passCount += CheckType("FC26.UI.PostMatchPanel", "UI/PostMatchPanel");
            passCount += CheckType("FC26.UI.PauseMenuUI", "UI/PauseMenuUI");

            // ===== 汇总 =====
            Debug.Log("---------- 自检汇总 ----------");
            Debug.Log($"通过: {passCount} 项");
            Debug.Log($"失败: {failCount} 项");

            if (failCount == 0)
            {
                Debug.Log("✓ FC26 工程自检全部通过！所有关键系统已就位。");
            }
            else
            {
                Debug.LogWarning($"✗ 有 {failCount} 项检查未通过，请查看上方日志。");
            }

            // ===== 数据层完整性检查 =====
            CheckDataIntegrity();

            Debug.Log("========== FC26 工程自检结束 ==========");
        }

        /// <summary>
        /// 检查指定类型是否存在（通过反射）。
        /// </summary>
        /// <param name="typeFullName">类型全名（含命名空间）</param>
        /// <param name="displayName">显示名称</param>
        /// <returns>存在返回 1，不存在返回 0</returns>
        private static int CheckType(string typeFullName, string displayName)
        {
            // 遍历所有程序集查找类型
            System.Type foundType = null;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foundType = assembly.GetType(typeFullName);
                if (foundType != null)
                    break;
            }

            if (foundType != null)
            {
                Debug.Log($"  ✓ {displayName} ({typeFullName})");
                return 1;
            }
            else
            {
                Debug.LogWarning($"  ✗ {displayName} ({typeFullName}) —— 未找到！");
                return 0;
            }
        }

        /// <summary>
        /// 检查数据层完整性（球队数量、球员数量）。
        /// </summary>
        private static void CheckDataIntegrity()
        {
            Debug.Log("---------- 数据层完整性检查 ----------");

            try
            {
                // 通过反射调用 PlayerDatabase.GetAllTeams()
                var dbType = System.AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType("FC26.Data.PlayerDatabase"))
                    .FirstOrDefault(t => t != null);

                if (dbType == null)
                {
                    Debug.LogWarning("  ✗ PlayerDatabase 类型未找到，无法检查数据完整性。");
                    return;
                }

                var getAllTeams = dbType.GetMethod("GetAllTeams", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (getAllTeams == null)
                {
                    Debug.LogWarning("  ✗ PlayerDatabase.GetAllTeams 方法未找到。");
                    return;
                }

                var teams = getAllTeams.Invoke(null, null) as System.Collections.IList;
                if (teams == null)
                {
                    Debug.LogWarning("  ✗ GetAllTeams 返回 null 或非列表类型。");
                    return;
                }

                Debug.Log($"  ✓ 球队总数: {teams.Count}（预期 36：中超 16 + 英超 20）");

                // 检查每队球员数
                int totalPlayers = 0;
                foreach (var team in teams)
                {
                    var teamType = team.GetType();
                    var startersField = teamType.GetField("Starters");
                    var subsField = teamType.GetField("Substitutes");

                    if (startersField != null && subsField != null)
                    {
                        var starters = startersField.GetValue(team) as System.Array;
                        var subs = subsField.GetValue(team) as System.Array;
                        int starterCount = starters?.Length ?? 0;
                        int subCount = subs?.Length ?? 0;
                        totalPlayers += starterCount + subCount;

                        if (starterCount != 11 || subCount != 7)
                        {
                            var nameField = teamType.GetField("TeamName");
                            string teamName = nameField?.GetValue(team)?.ToString() ?? "?";
                            Debug.LogWarning($"  ⚠ {teamName}: 首发 {starterCount} 人，替补 {subCount} 人（预期 11+7）");
                        }
                    }
                }

                Debug.Log($"  ✓ 球员总数: {totalPlayers}（预期 648：36 队 × 18 人）");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"  ✗ 数据完整性检查异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 生成 _FC26 目录结构（调用 ProjectStructure 工具）。
        /// </summary>
        [MenuItem(MenuRoot + "生成目录结构", priority = 1)]
        public static void GenerateDirectoryStructure()
        {
            Debug.Log("开始生成 _FC26 目录结构...");

            // 调用 ProjectStructure 工具（如果存在）
            var psType = System.AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("FC26.Editor.ProjectStructure"))
                .FirstOrDefault(t => t != null);

            if (psType != null)
            {
                var createMethod = psType.GetMethod("CreateDirectories", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (createMethod != null)
                {
                    createMethod.Invoke(null, null);
                    Debug.Log("✓ 目录结构生成完成。");
                    return;
                }
            }

            // 兜底：直接创建目录
            string basePath = "Assets/_FC26";
            string[] dirs = {
                "Scripts/Core", "Scripts/Data", "Scripts/Input", "Scripts/Match",
                "Scripts/Player", "Scripts/Ball", "Scripts/AI", "Scripts/Stadium",
                "Scripts/Referee", "Scripts/UI", "Scripts/Camera", "Scripts/Utils",
                "Prefabs", "Materials", "Scenes", "ScriptableObjects", "Settings", "Editor"
            };

            foreach (var dir in dirs)
            {
                string fullPath = $"{basePath}/{dir}";
                if (!AssetDatabase.IsValidFolder(fullPath))
                {
                    // 逐级创建
                    string[] parts = fullPath.Split('/');
                    string current = parts[0];
                    for (int i = 1; i < parts.Length; i++)
                    {
                        string next = $"{current}/{parts[i]}";
                        if (!AssetDatabase.IsValidFolder(next))
                        {
                            AssetDatabase.CreateFolder(current, parts[i]);
                        }
                        current = next;
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("✓ 目录结构生成完成（兜底方式）。");
        }

        /// <summary>
        /// 输出运行说明到控制台。
        /// </summary>
        [MenuItem(MenuRoot + "输出运行说明", priority = 2)]
        public static void PrintRunInstructions()
        {
            Debug.Log("========== FC26 工程运行说明 ==========");
            Debug.Log("");
            Debug.Log("【1. 环境要求】");
            Debug.Log("  - Unity 2022 LTS（推荐 2022.3.x）");
            Debug.Log("  - URP 3D 渲染管线");
            Debug.Log("  - .NET Standard 2.1 或 .NET 4.x");
            Debug.Log("");
            Debug.Log("【2. 工程配置】");
            Debug.Log("  - 打开 Edit → Project Settings → Graphics，将 Scriptable Render Pipeline Settings 设为 URP 资产");
            Debug.Log("  - Layer 配置：Field(8), Player(9), Ball(10), Goal(11), UI(12)");
            Debug.Log("  - Tag 配置：Field, Player, Ball, Goal");
            Debug.Log("");
            Debug.Log("【3. 场景加载】");
            Debug.Log("  - 打开 Assets/_FC26/Scenes/Match.unity（若无则新建空场景）");
            Debug.Log("  - 在场景中创建空 GameObject，挂载 SceneBootstrapper 脚本");
            Debug.Log("  - 点击 Play，SceneBootstrapper 会自动构建球场/球员/UI/摄像机");
            Debug.Log("");
            Debug.Log("【4. 键位说明】（仅键鼠，不支持手柄）");
            Debug.Log("  - WASD：跑动（W前/A左/S后/D右，相对摄像机方向）");
            Debug.Log("  - 鼠标左键：传球（按住时长决定力度，可长传）");
            Debug.Log("  - 鼠标右键：射门（按住时长决定力度）");
            Debug.Log("  - Space：直塞");
            Debug.Log("  - Left Shift：铲断");
            Debug.Log("  - Left Ctrl：拼抢（macOS 可重绑定为 Left Command）");
            Debug.Log("  - Q / E：切换球员（上一名/下一名）");
            Debug.Log("  - 鼠标滚轮：镜头缩放");
            Debug.Log("  - Esc：暂停");
            Debug.Log("");
            Debug.Log("【5. 跨平台适配】");
            Debug.Log("  - 运行时通过 Application.platform 检测 Windows / macOS");
            Debug.Log("  - macOS 上易冲突键（Left Ctrl）可在 KeyBindings 中重绑定");
            Debug.Log("");
            Debug.Log("【6. 数据说明】");
            Debug.Log("  - 中超 16 队（2026 赛季最新阵容）：上海海港/申花/泰山/国安/蓉城/三镇/浙江/津门虎/河南/海牛/新鹏城/英博/西海岸/铁人/铜梁龙/玉昆");
            Debug.Log("  - 英超 20 队（2026-27 赛季阵容）：曼城/阿森纳/利物浦/曼联/切尔西/热刺/维拉/纽卡/布莱顿/西汉姆/布伦特福德/水晶宫/富勒姆/埃弗顿/森林/狼队/伯恩茅斯/利兹联/桑德兰/赫尔城");
            Debug.Log("  - 每队 18 人（首发 11 + 替补 7），共 648 人");
            Debug.Log("");
            Debug.Log("【7. 自检】");
            Debug.Log("  - 菜单栏 → FC26 → 自检 → 检查关键系统");
            Debug.Log("");
            Debug.Log("========================================");
        }
    }
}
