//=============================================================================
// 文件名：MatchHUD.cs
// 所属模块：UI
// 命名空间：FC26.UI
// 作用：比赛 HUD 面板。比赛进行时显示：
//       1. 比分牌（主队名 比分 - 比分 客队名）；
//       2. 计时器（MM:SS 格式，顶部居中）；
//       3. 力度条（射门/传球按住时长可视化，底部居中）；
//       4. 控制提示（键位说明，右下角）。
//       Update 中每帧刷新比分、时间、力度条。
// 依赖：MatchManager（比分/队伍）、MatchTimer（时间）、InputReader（力度）。
// 备注：HUD 不遮挡视野，控件布局在屏幕边缘。运行时构建，不依赖预制体。
//=============================================================================
using UnityEngine;
using UnityEngine.UI;
using FC26.Input;

namespace FC26.UI
{
    /// <summary>
    /// 比赛 HUD 面板。显示比分、计时、力度条与控制提示。
    /// </summary>
    public class MatchHUD : UIBase
    {
        // ===== 比分牌控件 =====
        /// <summary>主队名文本。</summary>
        private Text _homeNameText;
        /// <summary>客队名文本。</summary>
        private Text _awayNameText;
        /// <summary>比分文本（如 "2 - 1"）。</summary>
        private Text _scoreText;

        // ===== 计时器控件 =====
        /// <summary>计时器文本。</summary>
        private Text _timerText;

        // ===== 力度条控件 =====
        /// <summary>射门力度条背景。</summary>
        private Image _shootPowerBg;
        /// <summary>射门力度条前景（按比例缩放）。</summary>
        private Image _shootPowerFill;
        /// <summary>传球力度条背景。</summary>
        private Image _passPowerBg;
        /// <summary>传球力度条前景。</summary>
        private Image _passPowerFill;
        /// <summary>力度条标签。</summary>
        private Text _powerLabelText;

        // ===== 控制提示 =====
        /// <summary>控制提示文本。</summary>
        private Text _controlHintText;

        /// <summary>
        /// 构建 HUD 面板。
        /// </summary>
        public override void Build()
        {
            if (IsBuilt)
            {
                return;
            }

            base.Build();

            // HUD 不需要全屏背景（透明，不遮挡视野）
            // 仅在各区域放置控件

            BuildScoreboard();   // 顶部居中：比分牌
            BuildTimer();        // 比分牌下方：计时器
            BuildPowerBars();    // 底部居中：力度条
            BuildControlHints(); // 右下角：控制提示

            Debug.Log("[MatchHUD] HUD 构建完成。");
        }

        /// <summary>
        /// 构建比分牌（顶部居中）。
        /// </summary>
        private void BuildScoreboard()
        {
            // 比分牌背景
            UIManager.Instance.CreateImage(Root, new Color(0.05f, 0.08f, 0.12f, 0.85f),
                new Vector2(0f, 500f), new Vector2(700f, 60f));

            // 主队名
            _homeNameText = UIManager.Instance.CreateText(Root, "主队",
                new Vector2(-220f, 500f), 24);
            _homeNameText.alignment = TextAnchor.MiddleCenter;

            // 比分
            _scoreText = UIManager.Instance.CreateText(Root, "0 - 0",
                new Vector2(0f, 500f), 32);
            _scoreText.alignment = TextAnchor.MiddleCenter;
            _scoreText.color = new Color(1f, 0.85f, 0.2f);

            // 客队名
            _awayNameText = UIManager.Instance.CreateText(Root, "客队",
                new Vector2(220f, 500f), 24);
            _awayNameText.alignment = TextAnchor.MiddleCenter;
        }

        /// <summary>
        /// 构建计时器（比分牌下方）。
        /// </summary>
        private void BuildTimer()
        {
            // 计时器背景
            UIManager.Instance.CreateImage(Root, new Color(0.05f, 0.08f, 0.12f, 0.85f),
                new Vector2(0f, 440f), new Vector2(160f, 40f));

            _timerText = UIManager.Instance.CreateText(Root, "00:00",
                new Vector2(0f, 440f), 26);
            _timerText.alignment = TextAnchor.MiddleCenter;
            _timerText.color = Color.white;
        }

        /// <summary>
        /// 构建力度条（底部居中）。
        /// 射门力度条（红色）与传球力度条（绿色）并排。
        /// </summary>
        private void BuildPowerBars()
        {
            // 力度条标签
            _powerLabelText = UIManager.Instance.CreateText(Root, "",
                new Vector2(0f, -460f), 20);
            _powerLabelText.alignment = TextAnchor.MiddleCenter;
            _powerLabelText.color = new Color(0.8f, 0.85f, 0.9f);

            // 射门力度条背景（右）
            _shootPowerBg = UIManager.Instance.CreateImage(Root, new Color(0.2f, 0.2f, 0.2f, 0.8f),
                new Vector2(120f, -500f), new Vector2(200f, 20f));

            // 射门力度条前景（红色，初始宽度 0）
            _shootPowerFill = UIManager.Instance.CreateImage(Root, new Color(0.9f, 0.2f, 0.2f, 1f),
                new Vector2(120f, -500f), new Vector2(200f, 20f));
            _shootPowerFill.type = Image.Type.Filled;
            _shootPowerFill.fillMethod = Image.FillMethod.Horizontal;
            _shootPowerFill.fillAmount = 0f;

            // 传球力度条背景（左）
            _passPowerBg = UIManager.Instance.CreateImage(Root, new Color(0.2f, 0.2f, 0.2f, 0.8f),
                new Vector2(-120f, -500f), new Vector2(200f, 20f));

            // 传球力度条前景（绿色，初始宽度 0）
            _passPowerFill = UIManager.Instance.CreateImage(Root, new Color(0.2f, 0.9f, 0.3f, 1f),
                new Vector2(-120f, -500f), new Vector2(200f, 20f));
            _passPowerFill.type = Image.Type.Filled;
            _passPowerFill.fillMethod = Image.FillMethod.Horizontal;
            _passPowerFill.fillAmount = 0f;

            // 力度条说明
            Text shootLabel = UIManager.Instance.CreateText(Root, "射门(右键)",
                new Vector2(120f, -525f), 16);
            shootLabel.alignment = TextAnchor.MiddleCenter;
            shootLabel.color = new Color(0.9f, 0.5f, 0.5f);

            Text passLabel = UIManager.Instance.CreateText(Root, "传球(左键)",
                new Vector2(-120f, -525f), 16);
            passLabel.alignment = TextAnchor.MiddleCenter;
            passLabel.color = new Color(0.5f, 0.9f, 0.5f);
        }

        /// <summary>
        /// 构建控制提示（右下角）。
        /// </summary>
        private void BuildControlHints()
        {
            // 提示背景
            UIManager.Instance.CreateImage(Root, new Color(0.05f, 0.08f, 0.12f, 0.7f),
                new Vector2(780f, -380f), new Vector2(340f, 240f));

            _controlHintText = UIManager.Instance.CreateText(Root,
                "控制说明:\n" +
                "WASD - 跑动\n" +
                "鼠标左键 - 传球(按住蓄力)\n" +
                "鼠标右键 - 射门(按住蓄力)\n" +
                "Space - 直塞\n" +
                "Shift - 铲断 / Ctrl - 拼抢\n" +
                "Q/E - 切换球员\n" +
                "滚轮 - 镜头缩放\n" +
                "Esc - 暂停",
                new Vector2(780f, -380f), 16);
            _controlHintText.alignment = TextAnchor.UpperLeft;
            _controlHintText.color = new Color(0.75f, 0.8f, 0.85f);
            _controlHintText.GetComponent<RectTransform>().sizeDelta = new Vector2(320f, 220f);
        }

        /// <summary>
        /// Update：每帧刷新比分、计时、力度条。
        /// </summary>
        private void Update()
        {
            if (!IsVisible)
            {
                return;
            }

            RefreshScore();
            RefreshTimer();
            RefreshPowerBars();
        }

        /// <summary>
        /// 刷新比分与队名。
        /// </summary>
        private void RefreshScore()
        {
            var matchMgr = Match.MatchManager.Instance;
            if (matchMgr == null)
            {
                return;
            }

            if (matchMgr.HomeTeam != null && _homeNameText != null)
            {
                _homeNameText.text = matchMgr.HomeTeam.TeamName;
            }

            if (matchMgr.AwayTeam != null && _awayNameText != null)
            {
                _awayNameText.text = matchMgr.AwayTeam.TeamName;
            }

            if (_scoreText != null)
            {
                _scoreText.text = $"{matchMgr.HomeScore} - {matchMgr.AwayScore}";
            }
        }

        /// <summary>
        /// 刷新计时器。
        /// 新 MatchTimer 的 TotalMatchTime 返回比赛分钟数（float），
        /// 需手动格式化为 MM:SS（分钟:秒）。
        /// </summary>
        private void RefreshTimer()
        {
            var matchMgr = Match.MatchManager.Instance;
            if (matchMgr == null || matchMgr.MatchTimer == null)
            {
                return;
            }

            if (_timerText != null)
            {
                // TotalMatchTime 为比赛分钟数（如 12.5 表示 12 分 30 秒）
                float totalMinutes = matchMgr.MatchTimer.TotalMatchTime;
                int minutes = Mathf.FloorToInt(totalMinutes);
                int seconds = Mathf.FloorToInt((totalMinutes - minutes) * 60f);
                _timerText.text = $"{minutes:00}:{seconds:00}";
            }
        }

        /// <summary>
        /// 刷新力度条（射门/传球按住时长）。
        /// </summary>
        private void RefreshPowerBars()
        {
            var input = InputReader.Instance;
            if (input == null)
            {
                return;
            }

            // 射门力度条
            if (_shootPowerFill != null)
            {
                _shootPowerFill.fillAmount = input.ShootPowerNormalized;
            }

            // 传球力度条
            if (_passPowerFill != null)
            {
                _passPowerFill.fillAmount = input.PassPowerNormalized;
            }

            // 力度条标签
            if (_powerLabelText != null)
            {
                if (input.IsShootHeld)
                {
                    _powerLabelText.text = $"射门蓄力: {input.ShootPowerNormalized * 100f:F0}%";
                }
                else if (input.IsPassHeld)
                {
                    _powerLabelText.text = $"传球蓄力: {input.PassPowerNormalized * 100f:F0}%";
                }
                else
                {
                    _powerLabelText.text = "";
                }
            }
        }
    }
}
