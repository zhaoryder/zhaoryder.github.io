//=============================================================================
// 文件名：MainMenuUI.cs
// 所属模块：UI
// 命名空间：FC26.UI
// 作用：主菜单面板。游戏启动后首个显示的界面。
//       包含 5 个按钮：开始比赛、选择球队、阵容、设置、退出。
//       按钮点击后通过 UIManager.Push 切换到对应子面板，或触发游戏状态切换。
// 依赖：UIManager（面板栈与控件创建）、GameManager（状态切换）。
// 备注：运行时构建，不依赖预制体。鼠标点击通过 Button.onClick 适配。
//=============================================================================
using UnityEngine;
using UnityEngine.UI;
using FC26.Core;

namespace FC26.UI
{
    /// <summary>
    /// 主菜单面板。提供游戏入口与功能导航。
    /// </summary>
    public class MainMenuUI : UIBase
    {
        /// <summary>主菜单标题文本引用（便于后续动态修改）。</summary>
        private Text _titleText;

        /// <summary>
        /// 构建主菜单面板：标题 + 5 个按钮。
        /// </summary>
        public override void Build()
        {
            if (IsBuilt)
            {
                return;
            }

            base.Build();

            // ===== 背景：半透明深色 =====
            UIManager.Instance.CreateImage(Root, new Color(0.08f, 0.10f, 0.14f, 0.95f),
                Vector2.zero, new Vector2(1920f, 1080f));

            // ===== 标题 =====
            _titleText = UIManager.Instance.CreateText(Root, "FC 26", new Vector2(0f, 380f), 72);
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.fontSize = 72;
            _titleText.color = new Color(1f, 0.85f, 0.2f); // 金色标题

            // 副标题
            Text subtitle = UIManager.Instance.CreateText(Root, "3D 足球对战", new Vector2(0f, 300f), 32);
            subtitle.alignment = TextAnchor.MiddleCenter;
            subtitle.color = new Color(0.8f, 0.85f, 0.9f);

            // ===== 5 个按钮（垂直排列）=====
            float buttonStartY = 180f;
            float buttonSpacing = 80f;

            // 1. 开始比赛
            UIManager.Instance.CreateButton(Root, "开始比赛",
                new Vector2(0f, buttonStartY), OnStartMatchClicked);

            // 2. 选择球队
            UIManager.Instance.CreateButton(Root, "选择球队",
                new Vector2(0f, buttonStartY - buttonSpacing), OnTeamSelectClicked);

            // 3. 阵容
            UIManager.Instance.CreateButton(Root, "阵容",
                new Vector2(0f, buttonStartY - buttonSpacing * 2f), OnLineupClicked);

            // 4. 设置
            UIManager.Instance.CreateButton(Root, "设置",
                new Vector2(0f, buttonStartY - buttonSpacing * 3f), OnSettingsClicked);

            // 5. 退出
            UIManager.Instance.CreateButton(Root, "退出",
                new Vector2(0f, buttonStartY - buttonSpacing * 4f), OnExitClicked);

            // 底部提示
            Text hint = UIManager.Instance.CreateText(Root, "鼠标点击操作  |  Esc 暂停",
                new Vector2(0f, -480f), 20);
            hint.alignment = TextAnchor.MiddleCenter;
            hint.color = new Color(0.6f, 0.65f, 0.7f);

            Debug.Log("[MainMenuUI] 主菜单构建完成。");
        }

        // ===== 按钮回调 =====

        /// <summary>
        /// 开始比赛：切换到球队选择面板（若已选队则直接进入比赛）。
        /// </summary>
        private void OnStartMatchClicked()
        {
            Debug.Log("[MainMenuUI] 点击：开始比赛");
            // 进入球队选择（首次启动默认未选队）
            UIManager.Instance.ShowPanel<TeamSelectUI>();
        }

        /// <summary>
        /// 选择球队：切换到球队选择面板。
        /// </summary>
        private void OnTeamSelectClicked()
        {
            Debug.Log("[MainMenuUI] 点击：选择球队");
            UIManager.Instance.ShowPanel<TeamSelectUI>();
        }

        /// <summary>
        /// 阵容：切换到阵容面板（需先选队）。
        /// </summary>
        private void OnLineupClicked()
        {
            Debug.Log("[MainMenuUI] 点击：阵容");
            // 阵容面板需要球队数据，若未选队则提示
            var matchMgr = Match.MatchManager.Instance;
            if (matchMgr != null && matchMgr.HomeTeam != null)
            {
                UIManager.Instance.ShowPanel<LineupUI>();
                var lineup = UIManager.Instance.GetCurrentPanel() as LineupUI;
                lineup?.Build(matchMgr.HomeTeam);
            }
            else
            {
                Debug.LogWarning("[MainMenuUI] 尚未选择球队，请先进入选择球队。");
                UIManager.Instance.ShowPanel<TeamSelectUI>();
            }
        }

        /// <summary>
        /// 设置：暂未实现独立设置面板，仅日志占位。
        /// </summary>
        private void OnSettingsClicked()
        {
            Debug.Log("[MainMenuUI] 点击：设置（暂未实现）");
        }

        /// <summary>
        /// 退出：退出应用（编辑器模式下停止 Play）。
        /// </summary>
        private void OnExitClicked()
        {
            Debug.Log("[MainMenuUI] 点击：退出");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
