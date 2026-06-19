//=============================================================================
// 文件名：PostMatchPanel.cs
// 所属模块：UI
// 命名空间：FC26.UI
// 作用：赛后数据面板。比赛结束后显示：
//       1. 最终比分与队名；
//       2. 各项统计数据（控球率、射门、射正、角球、犯规、黄牌、红牌）；
//       3. 简易柱状图：用 UGUI Image 水平缩放表示主客队数值对比。
// 依赖：MatchStatistics（数据源）、MatchManager（比分/队名）、UIManager。
// 备注：柱状图通过 Image 的 RectTransform 宽度按比例缩放实现，无需第三方图表库。
//=============================================================================
using UnityEngine;
using UnityEngine.UI;

namespace FC26.UI
{
    /// <summary>
    /// 赛后数据面板。显示比赛统计与柱状图对比。
    /// </summary>
    public class PostMatchPanel : UIBase
    {
        /// <summary>统计数据引用。</summary>
        private Match.MatchStatistics _stats;

        /// <summary>柱状图容器。</summary>
        private RectTransform _chartContainer;

        /// <summary>柱状图条目最大宽度（像素）。</summary>
        private const float MaxBarWidth = 280f;

        /// <summary>布局是否已构建（独立于 IsBuilt，避免与基类 Build 冲突）。</summary>
        private bool _layoutBuilt;

        /// <summary>
        /// 构建赛后面板（需传入统计数据）。
        /// 注意：ShowPanel 会先调用基类 Build() 创建 Root 并置 IsBuilt=true，
        ///       因此布局构建使用独立标志 _layoutBuilt，确保只构建一次。
        /// </summary>
        /// <param name="stats">比赛统计数据</param>
        public void Build(Match.MatchStatistics stats)
        {
            _stats = stats;

            // 基类 Build 创建 Root（若尚未创建）
            if (!IsBuilt)
            {
                base.Build();
            }

            // 布局只构建一次
            if (!_layoutBuilt)
            {
                BuildLayout();
                _layoutBuilt = true;
            }

            // 每次都刷新数据
            RefreshData();
            Debug.Log("[PostMatchPanel] 赛后面板构建完成。");
        }

        /// <summary>
        /// 构建面板布局。
        /// </summary>
        private void BuildLayout()
        {
            // ===== 全屏遮罩 + 内容区 =====
            RectTransform content = UIManager.Instance.CreatePanelContainer(Root, "PostMatchPanel");

            // ===== 标题 =====
            Text title = UIManager.Instance.CreateText(content, "比赛结束", new Vector2(0f, 260f), 40);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(1f, 0.85f, 0.2f);

            // ===== 最终比分 =====
            UIManager.Instance.CreateText(content, "最终比分", new Vector2(0f, 190f), 22).alignment = TextAnchor.MiddleCenter;
            // _scoreText 在 RefreshData 中赋值

            // ===== 柱状图容器 =====
            GameObject chartObj = new GameObject("ChartContainer");
            chartObj.transform.SetParent(content, false);
            _chartContainer = chartObj.AddComponent<RectTransform>();
            _chartContainer.anchoredPosition = new Vector2(0f, -20f);
            _chartContainer.sizeDelta = new Vector2(760f, 400f);

            // ===== 底部按钮 =====
            UIManager.Instance.CreateButton(content, "返回主菜单",
                new Vector2(0f, -260f), new Vector2(240f, 50f), OnBackToMenuClicked);
        }

        /// <summary>
        /// 刷新数据：填充比分与柱状图。
        /// </summary>
        private void RefreshData()
        {
            var matchMgr = Match.MatchManager.Instance;
            if (matchMgr == null || _stats == null)
            {
                return;
            }

            // 清空柱状图容器
            ClearContainer(_chartContainer);

            // 顶部比分显示
            string homeName = matchMgr.HomeTeam != null ? matchMgr.HomeTeam.TeamName : "主队";
            string awayName = matchMgr.AwayTeam != null ? matchMgr.AwayTeam.TeamName : "客队";
            UIManager.Instance.CreateText(_chartContainer,
                $"{homeName}  {matchMgr.HomeScore} - {matchMgr.AwayScore}  {awayName}",
                new Vector2(0f, 180f), 32).alignment = TextAnchor.MiddleCenter;

            // 通过 GetStats() 获取统计快照（包含主客队各项数据与控球率）
            Match.MatchStatsSnapshot snapshot = _stats.GetStats();

            // ===== 柱状图：各项数据对比 =====
            // 每行：左侧主队数值条 | 中间标签 | 右侧客队数值条
            float startY = 120f;
            float rowSpacing = 45f;

            // 控球率（百分比）
            AddComparisonRow("控球率", snapshot.HomePossessionRate, snapshot.AwayPossessionRate,
                startY, true);

            // 射门
            AddComparisonRow("射门", snapshot.Home.Shots, snapshot.Away.Shots,
                startY - rowSpacing, false);

            // 射正
            AddComparisonRow("射正", snapshot.Home.ShotsOnTarget, snapshot.Away.ShotsOnTarget,
                startY - rowSpacing * 2f, false);

            // 角球
            AddComparisonRow("角球", snapshot.Home.Corners, snapshot.Away.Corners,
                startY - rowSpacing * 3f, false);

            // 犯规
            AddComparisonRow("犯规", snapshot.Home.Fouls, snapshot.Away.Fouls,
                startY - rowSpacing * 4f, false);

            // 黄牌
            AddComparisonRow("黄牌", snapshot.Home.YellowCards, snapshot.Away.YellowCards,
                startY - rowSpacing * 5f, false);

            // 红牌
            AddComparisonRow("红牌", snapshot.Home.RedCards, snapshot.Away.RedCards,
                startY - rowSpacing * 6f, false);
        }

        /// <summary>
        /// 添加一行数据对比（主队条 | 标签 | 客队条）。
        /// </summary>
        /// <param name="label">数据项名称</param>
        /// <param name="homeValue">主队数值</param>
        /// <param name="awayValue">客队数值</param>
        /// <param name="y">行 Y 坐标</param>
        /// <param name="isPercentage">是否为百分比（控球率）</param>
        private void AddComparisonRow(string label, float homeValue, float awayValue, float y, bool isPercentage)
        {
            // 计算最大值用于条形宽度比例
            float maxVal = Mathf.Max(homeValue, awayValue, 0.0001f);
            float homeRatio = homeValue / maxVal;
            float awayRatio = awayValue / maxVal;

            // 主队条（左侧，向右延伸）- 蓝色
            Image homeBar = UIManager.Instance.CreateImage(_chartContainer, new Color(0.2f, 0.5f, 0.9f, 0.95f),
                new Vector2(-200f, y), new Vector2(MaxBarWidth * homeRatio, 24f));

            // 主队数值文本
            string homeText = isPercentage ? $"{homeValue * 100f:F0}%" : homeValue.ToString("F0");
            UIManager.Instance.CreateText(_chartContainer, homeText,
                new Vector2(-200f - MaxBarWidth * 0.5f - 30f, y), 18).alignment = TextAnchor.MiddleCenter;

            // 中间标签
            Text labelTxt = UIManager.Instance.CreateText(_chartContainer, label,
                new Vector2(0f, y), 20);
            labelTxt.alignment = TextAnchor.MiddleCenter;
            labelTxt.color = new Color(0.9f, 0.9f, 0.9f);

            // 客队条（右侧，向右延伸）- 红色
            Image awayBar = UIManager.Instance.CreateImage(_chartContainer, new Color(0.9f, 0.3f, 0.3f, 0.95f),
                new Vector2(200f, y), new Vector2(MaxBarWidth * awayRatio, 24f));

            // 客队数值文本
            string awayText = isPercentage ? $"{awayValue * 100f:F0}%" : awayValue.ToString("F0");
            UIManager.Instance.CreateText(_chartContainer, awayText,
                new Vector2(200f + MaxBarWidth * 0.5f + 30f, y), 18).alignment = TextAnchor.MiddleCenter;
        }

        /// <summary>
        /// 返回主菜单。
        /// </summary>
        private void OnBackToMenuClicked()
        {
            Debug.Log("[PostMatchPanel] 返回主菜单");
            UIManager.Instance.ShowPanel<MainMenuUI>();
        }

        /// <summary>
        /// 清空容器子物体。
        /// </summary>
        private void ClearContainer(RectTransform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }
    }
}
