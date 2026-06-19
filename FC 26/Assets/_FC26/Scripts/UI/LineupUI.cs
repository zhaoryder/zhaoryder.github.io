//=============================================================================
// 文件名：LineupUI.cs
// 所属模块：UI
// 命名空间：FC26.UI
// 作用：阵容面板。显示指定球队的首发 11 人与替补 7 人，支持：
//       1. 阵型显示（4-4-2 / 4-3-3 / 4-2-3-1）；
//       2. 首发与替补列表；
//       3. 点击替换：先点首发球员（标记待替换），再点替补完成交换；
//       4. 确认后开始比赛。
// 依赖：TeamData（球队阵容）、Formations（阵型坐标）、MatchManager、UIManager。
// 备注：拖拽替换简化为点击替换（点首发 -> 点替补 -> 交换）。
//=============================================================================
using UnityEngine;
using UnityEngine.UI;
using FC26.Data;

namespace FC26.UI
{
    /// <summary>
    /// 阵容面板。显示首发/替补并支持点击替换。
    /// </summary>
    public class LineupUI : UIBase
    {
        /// <summary>当前展示的球队数据。</summary>
        private TeamData _team;

        /// <summary>阵型名称文本。</summary>
        private Text _formationText;

        /// <summary>球队名称文本。</summary>
        private Text _teamNameText;

        /// <summary>首发列表容器。</summary>
        private RectTransform _startersContainer;

        /// <summary>替补列表容器。</summary>
        private RectTransform _subsContainer;

        /// <summary>待替换的首发球员索引（-1=未选中）。</summary>
        private int _pendingStarterIndex = -1;

        /// <summary>操作提示文本。</summary>
        private Text _hintText;

        /// <summary>布局是否已构建（独立于 IsBuilt，避免与基类 Build 冲突）。</summary>
        private bool _layoutBuilt;

        /// <summary>
        /// 构建阵容面板（需传入球队数据）。
        /// 注意：ShowPanel 会先调用基类 Build() 创建 Root 并置 IsBuilt=true，
        ///       因此布局构建使用独立标志 _layoutBuilt，确保只构建一次。
        /// </summary>
        /// <param name="team">要展示的球队</param>
        public void Build(TeamData team)
        {
            _team = team;

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
            RefreshLineup();
            Debug.Log($"[LineupUI] 阵容面板构建完成: {team?.TeamName}");
        }

        /// <summary>
        /// 构建面板布局（标题、分区、按钮）。
        /// </summary>
        private void BuildLayout()
        {
            // ===== 背景 =====
            UIManager.Instance.CreateImage(Root, new Color(0.08f, 0.10f, 0.14f, 0.98f),
                Vector2.zero, new Vector2(1920f, 1080f));

            // ===== 标题区 =====
            _teamNameText = UIManager.Instance.CreateText(Root, "", new Vector2(0f, 460f), 44);
            _teamNameText.alignment = TextAnchor.MiddleCenter;
            _teamNameText.color = new Color(1f, 0.85f, 0.2f);

            _formationText = UIManager.Instance.CreateText(Root, "", new Vector2(0f, 400f), 28);
            _formationText.alignment = TextAnchor.MiddleCenter;
            _formationText.color = new Color(0.8f, 0.85f, 0.9f);

            // ===== 分区标题 =====
            Text startersTitle = UIManager.Instance.CreateText(Root, "首发 11 人",
                new Vector2(-450f, 330f), 26);
            startersTitle.alignment = TextAnchor.MiddleCenter;
            startersTitle.color = new Color(0.4f, 0.8f, 1f);

            Text subsTitle = UIManager.Instance.CreateText(Root, "替补 7 人",
                new Vector2(450f, 330f), 26);
            subsTitle.alignment = TextAnchor.MiddleCenter;
            subsTitle.color = new Color(0.4f, 0.8f, 1f);

            // ===== 首发列表容器 =====
            GameObject startersObj = new GameObject("StartersList");
            startersObj.transform.SetParent(Root, false);
            _startersContainer = startersObj.AddComponent<RectTransform>();
            _startersContainer.anchoredPosition = new Vector2(-450f, 20f);
            _startersContainer.sizeDelta = new Vector2(500f, 600f);

            // ===== 替补列表容器 =====
            GameObject subsObj = new GameObject("SubsList");
            subsObj.transform.SetParent(Root, false);
            _subsContainer = subsObj.AddComponent<RectTransform>();
            _subsContainer.anchoredPosition = new Vector2(450f, 20f);
            _subsContainer.sizeDelta = new Vector2(500f, 500f);

            // ===== 操作提示 =====
            _hintText = UIManager.Instance.CreateText(Root, "点击首发球员，再点击替补完成替换",
                new Vector2(0f, -380f), 22);
            _hintText.alignment = TextAnchor.MiddleCenter;
            _hintText.color = new Color(0.7f, 0.75f, 0.8f);

            // ===== 底部按钮 =====
            UIManager.Instance.CreateButton(Root, "返回",
                new Vector2(-300f, -460f), new Vector2(200f, 50f), OnBackClicked);
            UIManager.Instance.CreateButton(Root, "开始比赛",
                new Vector2(300f, -460f), new Vector2(200f, 50f), OnStartMatchClicked);
        }

        /// <summary>
        /// 刷新阵容显示（标题 + 首发列表 + 替补列表）。
        /// </summary>
        private void RefreshLineup()
        {
            if (_team == null)
            {
                return;
            }

            // 标题
            _teamNameText.text = _team.TeamName;
            _formationText.text = $"阵型: {FormatFormation(_team.Formation)}  |  战术: {FormatTactic(_team.Tactic)}";

            // 清空并重建首发列表
            ClearContainer(_startersContainer);
            if (_team.Starters != null)
            {
                for (int i = 0; i < _team.Starters.Length; i++)
                {
                    int index = i; // 闭包捕获
                    PlayerData p = _team.Starters[i];
                    string label = $"{i + 1}. #{p.Number} {p.Name} ({p.Position})  OVR:{p.Overall}";
                    Button btn = UIManager.Instance.CreateButton(_startersContainer, label,
                        new Vector2(0f, 280f - i * 45f), new Vector2(480f, 40f),
                        () => OnStarterClicked(index));
                }
            }

            // 清空并重建替补列表
            ClearContainer(_subsContainer);
            if (_team.Substitutes != null)
            {
                for (int i = 0; i < _team.Substitutes.Length; i++)
                {
                    int index = i;
                    PlayerData p = _team.Substitutes[i];
                    string label = $"{i + 1}. #{p.Number} {p.Name} ({p.Position})  OVR:{p.Overall}";
                    Button btn = UIManager.Instance.CreateButton(_subsContainer, label,
                        new Vector2(0f, 220f - i * 45f), new Vector2(480f, 40f),
                        () => OnSubClicked(index));
                }
            }
        }

        /// <summary>
        /// 点击首发球员：标记为待替换。
        /// </summary>
        /// <param name="index">首发索引</param>
        private void OnStarterClicked(int index)
        {
            _pendingStarterIndex = index;
            if (_team != null && _team.Starters != null && index < _team.Starters.Length)
            {
                _hintText.text = $"已选中首发: {_team.Starters[index].Name}，请点击替补球员进行替换";
            }
            Debug.Log($"[LineupUI] 选中首发索引 {index}");
        }

        /// <summary>
        /// 点击替补球员：与待替换首发交换。
        /// </summary>
        /// <param name="index">替补索引</param>
        private void OnSubClicked(int index)
        {
            if (_pendingStarterIndex < 0)
            {
                _hintText.text = "请先点击首发球员，再点击替补完成替换";
                return;
            }

            if (_team == null || _team.Starters == null || _team.Substitutes == null)
            {
                return;
            }

            // 交换首发与替补
            PlayerData tmp = _team.Starters[_pendingStarterIndex];
            _team.Starters[_pendingStarterIndex] = _team.Substitutes[index];
            _team.Substitutes[index] = tmp;

            Debug.Log($"[LineupUI] 替换完成: 首发[{_pendingStarterIndex}] <-> 替补[{index}]");

            _pendingStarterIndex = -1;
            _hintText.text = "替换成功，点击首发球员可继续替换";
            RefreshLineup();
        }

        /// <summary>
        /// 返回上一面板（球队选择）。
        /// </summary>
        private void OnBackClicked()
        {
            Debug.Log("[LineupUI] 返回球队选择");
            UIManager.Instance.ShowPanel<TeamSelectUI>();
        }

        /// <summary>
        /// 开始比赛：设置队伍并进入比赛。
        /// </summary>
        private void OnStartMatchClicked()
        {
            Debug.Log("[LineupUI] 开始比赛");
            var matchMgr = Match.MatchManager.Instance;
            if (matchMgr != null && matchMgr.HomeTeam != null && matchMgr.AwayTeam != null)
            {
                matchMgr.StartMatch();
                // 显示 HUD
                UIManager.Instance.ShowPanel<MatchHUD>();
            }
            else
            {
                Debug.LogWarning("[LineupUI] 队伍未设置完整，无法开始比赛。");
            }
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

        /// <summary>
        /// 格式化阵型名称。
        /// </summary>
        private string FormatFormation(FormationType f)
        {
            switch (f)
            {
                case FormationType.F442: return "4-4-2";
                case FormationType.F433: return "4-3-3";
                case FormationType.F4231: return "4-2-3-1";
                default: return f.ToString();
            }
        }

        /// <summary>
        /// 格式化战术风格名称。
        /// </summary>
        private string FormatTactic(TacticStyle t)
        {
            switch (t)
            {
                case TacticStyle.Possession: return "控球";
                case TacticStyle.Counter: return "反击";
                case TacticStyle.HighPress: return "高位压迫";
                case TacticStyle.DefensiveCounter: return "防守反击";
                default: return t.ToString();
            }
        }
    }
}
