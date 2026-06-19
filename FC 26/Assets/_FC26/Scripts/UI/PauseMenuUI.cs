//=============================================================================
// 文件名：PauseMenuUI.cs
// 所属模块：UI
// 命名空间：FC26.UI
// 作用：暂停菜单面板。比赛进行中按 Esc 呼出，提供：
//       1. 继续比赛（恢复游戏，关闭暂停菜单）；
//       2. 重新开始（重置比赛，回到开球）；
//       3. 返回主菜单（清理比赛状态，回主菜单）；
//       4. 退出游戏。
// 依赖：GameManager（暂停控制）、MatchManager（重置比赛）、UIManager。
// 备注：暂停时比赛逻辑冻结但 UI 仍响应（由 GameManager.SetPaused 控制）。
//=============================================================================
using UnityEngine;
using UnityEngine.UI;
using FC26.Core;

namespace FC26.UI
{
    /// <summary>
    /// 暂停菜单面板。提供继续/重开/返回主菜单/退出四个选项。
    /// </summary>
    public class PauseMenuUI : UIBase
    {
        /// <summary>
        /// 构建暂停菜单面板。
        /// </summary>
        public override void Build()
        {
            if (IsBuilt)
            {
                return;
            }

            base.Build();

            // ===== 全屏遮罩 + 内容区 =====
            RectTransform content = UIManager.Instance.CreatePanelContainer(Root, "PauseMenu");

            // ===== 标题 =====
            Text title = UIManager.Instance.CreateText(content, "暂停", new Vector2(0f, 220f), 48);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(1f, 0.85f, 0.2f);

            // ===== 4 个按钮（垂直排列）=====
            float buttonStartY = 120f;
            float buttonSpacing = 80f;

            // 1. 继续比赛
            UIManager.Instance.CreateButton(content, "继续比赛",
                new Vector2(0f, buttonStartY), OnResumeClicked);

            // 2. 重新开始
            UIManager.Instance.CreateButton(content, "重新开始",
                new Vector2(0f, buttonStartY - buttonSpacing), OnRestartClicked);

            // 3. 返回主菜单
            UIManager.Instance.CreateButton(content, "返回主菜单",
                new Vector2(0f, buttonStartY - buttonSpacing * 2f), OnBackToMenuClicked);

            // 4. 退出游戏
            UIManager.Instance.CreateButton(content, "退出游戏",
                new Vector2(0f, buttonStartY - buttonSpacing * 3f), OnExitClicked);

            // 底部提示
            Text hint = UIManager.Instance.CreateText(content, "按 Esc 继续比赛",
                new Vector2(0f, -220f), 20);
            hint.alignment = TextAnchor.MiddleCenter;
            hint.color = new Color(0.6f, 0.65f, 0.7f);

            Debug.Log("[PauseMenuUI] 暂停菜单构建完成。");
        }

        /// <summary>
        /// 继续：恢复游戏，隐藏暂停菜单。
        /// </summary>
        private void OnResumeClicked()
        {
            Debug.Log("[PauseMenuUI] 继续");
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.SetPaused(false);
            }
            Hide();
        }

        /// <summary>
        /// 重新开始：重置比赛状态，回到开球。
        /// </summary>
        private void OnRestartClicked()
        {
            Debug.Log("[PauseMenuUI] 重新开始");
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.SetPaused(false);
            }

            var matchMgr = Match.MatchManager.Instance;
            if (matchMgr != null)
            {
                matchMgr.StartMatch();
            }

            Hide();
        }

        /// <summary>
        /// 返回主菜单：清理比赛状态，显示主菜单。
        /// </summary>
        private void OnBackToMenuClicked()
        {
            Debug.Log("[PauseMenuUI] 返回主菜单");
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.SetPaused(false);
            }

            var matchMgr = Match.MatchManager.Instance;
            if (matchMgr != null)
            {
                matchMgr.EndMatch();
            }

            UIManager.Instance.ShowPanel<MainMenuUI>();
        }

        /// <summary>
        /// 退出游戏。
        /// </summary>
        private void OnExitClicked()
        {
            Debug.Log("[PauseMenuUI] 退出游戏");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
