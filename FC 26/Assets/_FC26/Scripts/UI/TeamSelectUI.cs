//=============================================================================
// 文件名：TeamSelectUI.cs
// 所属模块：UI
// 命名空间：FC26.UI
// 作用：球队选择面板。提供：
//       1. 联赛切换（中超 CSL / 英超 EPL）；
//       2. 球队列表网格显示（从 PlayerDatabase 获取）；
//       3. 选择主队/客队（点击球队按钮先选主队，再选客队）；
//       4. 确认后进入阵容面板或直接开始比赛。
// 依赖：PlayerDatabase（球队数据）、MatchManager（保存所选队伍）、UIManager。
// 备注：运行时构建，不依赖预制体。球队按钮使用网格布局。
//=============================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FC26.Data;

namespace FC26.UI
{
    /// <summary>
    /// 球队选择面板。支持联赛切换与主客队选择。
    /// </summary>
    public class TeamSelectUI : UIBase
    {
        /// <summary>当前选中的联赛。</summary>
        private League _currentLeague = League.CSL;

        /// <summary>主队选择状态（true=已选主队，下一步选客队）。</summary>
        private bool _homeSelected;

        /// <summary>已选主队数据。</summary>
        private TeamData _selectedHome;

        /// <summary>已选客队数据。</summary>
        private TeamData _selectedAway;

        /// <summary>球队列表容器（切换联赛时清空重建）。</summary>
        private RectTransform _teamListContainer;

        /// <summary>状态提示文本。</summary>
        private Text _statusText;

        /// <summary>返回按钮（返回主菜单）。</summary>
        private Button _backButton;

        /// <summary>确认按钮（选完主客队后可用）。</summary>
        private Button _confirmButton;

        /// <summary>
        /// 构建球队选择面板。
        /// </summary>
        public override void Build()
        {
            if (IsBuilt)
            {
                return;
            }

            base.Build();

            // ===== 背景 =====
            UIManager.Instance.CreateImage(Root, new Color(0.08f, 0.10f, 0.14f, 0.98f),
                Vector2.zero, new Vector2(1920f, 1080f));

            // ===== 标题 =====
            Text title = UIManager.Instance.CreateText(Root, "选择球队", new Vector2(0f, 460f), 48);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(1f, 0.85f, 0.2f);

            // ===== 联赛切换按钮 =====
            UIManager.Instance.CreateButton(Root, "中超 CSL",
                new Vector2(-200f, 360f), new Vector2(220f, 50f), () => SwitchLeague(League.CSL));
            UIManager.Instance.CreateButton(Root, "英超 EPL",
                new Vector2(200f, 360f), new Vector2(220f, 50f), () => SwitchLeague(League.EPL));

            // ===== 状态提示 =====
            _statusText = UIManager.Instance.CreateText(Root, "请选择主队", new Vector2(0f, 300f), 28);
            _statusText.alignment = TextAnchor.MiddleCenter;
            _statusText.color = Color.white;

            // ===== 球队列表容器 =====
            GameObject listObj = new GameObject("TeamList");
            listObj.transform.SetParent(Root, false);
            _teamListContainer = listObj.AddComponent<RectTransform>();
            _teamListContainer.anchoredPosition = new Vector2(0f, 20f);
            _teamListContainer.sizeDelta = new Vector2(1400f, 500f);

            // ===== 底部按钮：返回 / 确认 =====
            _backButton = UIManager.Instance.CreateButton(Root, "返回主菜单",
                new Vector2(-300f, -460f), new Vector2(220f, 50f), OnBackClicked);

            _confirmButton = UIManager.Instance.CreateButton(Root, "确认并开始",
                new Vector2(300f, -460f), new Vector2(220f, 50f), OnConfirmClicked);
            _confirmButton.interactable = false; // 选完主客队后启用

            // 初始填充球队列表
            RefreshTeamList();

            Debug.Log("[TeamSelectUI] 球队选择面板构建完成。");
        }

        /// <summary>
        /// 切换联赛并刷新球队列表。
        /// </summary>
        /// <param name="league">目标联赛</param>
        private void SwitchLeague(League league)
        {
            if (_currentLeague == league)
            {
                return;
            }

            _currentLeague = league;
            RefreshTeamList();
            Debug.Log($"[TeamSelectUI] 切换联赛: {league}");
        }

        /// <summary>
        /// 刷新球队列表（清空容器后按当前联赛重新构建按钮网格）。
        /// </summary>
        private void RefreshTeamList()
        {
            // 清空旧按钮
            for (int i = _teamListContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_teamListContainer.GetChild(i).gameObject);
            }

            // 获取当前联赛球队
            List<TeamData> teams = PlayerDatabase.GetTeamsByLeague(_currentLeague);

            // 网格布局：每行 4 个，间距 320x100
            int columns = 4;
            float spacingX = 320f;
            float spacingY = 100f;
            float startX = -(columns - 1) * spacingX * 0.5f;
            float startY = 200f;

            for (int i = 0; i < teams.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;
                Vector2 pos = new Vector2(startX + col * spacingX, startY - row * spacingY);

                TeamData team = teams[i];
                string label = $"{team.TeamName}";
                Button btn = UIManager.Instance.CreateButton(_teamListContainer, label, pos,
                    new Vector2(280f, 70f), () => OnTeamClicked(team));
            }
        }

        /// <summary>
        /// 球队按钮点击：先选主队，再选客队。
        /// </summary>
        /// <param name="team">被点击的球队</param>
        private void OnTeamClicked(TeamData team)
        {
            if (!_homeSelected)
            {
                // 选主队
                _selectedHome = team;
                _homeSelected = true;
                _statusText.text = $"主队: {team.TeamName}  |  请选择客队";
                Debug.Log($"[TeamSelectUI] 选择主队: {team.TeamName}");
            }
            else
            {
                // 选客队（不能与主队相同）
                if (team == _selectedHome)
                {
                    Debug.LogWarning("[TeamSelectUI] 客队不能与主队相同。");
                    return;
                }

                _selectedAway = team;
                _statusText.text = $"主队: {_selectedHome.TeamName}  vs  客队: {team.TeamName}";
                _confirmButton.interactable = true;
                Debug.Log($"[TeamSelectUI] 选择客队: {team.TeamName}");
            }
        }

        /// <summary>
        /// 返回主菜单。
        /// </summary>
        private void OnBackClicked()
        {
            Debug.Log("[TeamSelectUI] 返回主菜单");
            UIManager.Instance.ShowPanel<MainMenuUI>();
        }

        /// <summary>
        /// 确认选择：保存到 MatchManager，进入阵容面板。
        /// </summary>
        private void OnConfirmClicked()
        {
            if (_selectedHome == null || _selectedAway == null)
            {
                Debug.LogWarning("[TeamSelectUI] 主客队未选全，无法确认。");
                return;
            }

            // 保存到 MatchManager
            var matchMgr = Match.MatchManager.Instance;
            if (matchMgr != null)
            {
                matchMgr.SetTeams(_selectedHome, _selectedAway);
            }

            Debug.Log($"[TeamSelectUI] 确认选择: {_selectedHome.TeamName} vs {_selectedAway.TeamName}");

            // 进入阵容面板（先看主队阵容）
            UIManager.Instance.ShowPanel<LineupUI>();
            var lineup = UIManager.Instance.GetCurrentPanel() as LineupUI;
            lineup?.Build(_selectedHome);
        }

        /// <summary>
        /// 重写 Show：每次显示时重置选择状态。
        /// </summary>
        public override void Show()
        {
            base.Show();
            _homeSelected = false;
            _selectedHome = null;
            _selectedAway = null;
            if (_statusText != null)
            {
                _statusText.text = "请选择主队";
            }
            if (_confirmButton != null)
            {
                _confirmButton.interactable = false;
            }
        }
    }
}
