//=============================================================================
// 文件名：MatchTimer.cs
// 所属模块：Match
// 命名空间：FC26.Match
// 作用：比赛计时器单例。继承 MonoSingleton<MatchTimer>，全局唯一。
//       负责比赛时间累计、半场切换、伤停补时计算。
//       支持可配置半场时长（默认 45 分钟）与时间加速比（默认 1 现实秒=1 比赛分钟）。
//       暂停时停止计时，伤停补时基于犯规数与进球数自动计算。
// 备注：本脚本需挂载在场景中的 GameObject 上。
//       比赛时间以"比赛分钟"为单位累计，内部用秒换算。
//=============================================================================
using UnityEngine;
using FC26.Core;

namespace FC26.Match
{
    /// <summary>
    /// 比赛计时器单例：管理比赛时间累计、半场切换、伤停补时。
    /// </summary>
    public class MatchTimer : MonoSingleton<MatchTimer>
    {
        // ===== 计时配置 =====
        [Header("计时配置")]
        [Tooltip("半场时长（比赛分钟），默认 45 分钟")]
        [SerializeField] private float _halfDurationMinutes = 45f;

        [Tooltip("时间加速比：1 现实秒 = N 比赛秒。默认 60（即 1 现实秒=1 比赛分钟）")]
        [SerializeField] private float _timeScale = 60f;

        [Tooltip("伤停补时基础分钟数")]
        [SerializeField] private float _stoppageBaseMinutes = 1f;

        [Tooltip("每犯规增加的补时分钟数")]
        [SerializeField] private float _stoppagePerFoul = 0.15f;

        [Tooltip("每进球增加的补时分钟数")]
        [SerializeField] private float _stoppagePerGoal = 0.5f;

        [Tooltip("伤停补时上限（分钟）")]
        [SerializeField] private float _stoppageMaxMinutes = 5f;

        // ===== 运行时状态 =====
        // 当前半场已进行的比赛分钟数
        private float _currentTimeMinutes = 0f;

        // 当前半场（1=上半场，2=下半场）
        private int _currentHalf = 1;

        // 是否暂停
        private bool _isPaused = true;

        // 是否正在运行（已 Start 且未 Reset）
        private bool _isRunning = false;

        // 是否处于伤停补时阶段
        private bool _isStoppageTime = false;

        // 本半场已计算的伤停补时分钟数
        private float _calculatedStoppageMinutes = 0f;

        // ===== 伤停补时统计计数 =====
        private int _foulCount = 0;   // 本半场犯规数
        private int _goalCount = 0;   // 本半场进球数

        // ===== 回调 =====
        /// <summary>
        /// 半场结束回调。参数为结束的半场号（1 或 2）。
        /// 由 MatchManager 订阅以驱动半场切换流程。
        /// </summary>
        public event System.Action<int> OnHalfEnded;

        /// <summary>当前半场已进行的比赛分钟数。</summary>
        public float CurrentTime => _currentTimeMinutes;

        /// <summary>当前比赛总时间（上半场 0~45，下半场 45~90+）。</summary>
        public float TotalMatchTime => (_currentHalf == 1) ? _currentTimeMinutes : _halfDurationMinutes + _currentTimeMinutes;

        /// <summary>是否暂停。</summary>
        public bool IsPaused => _isPaused;

        /// <summary>是否正在运行。</summary>
        public bool IsRunning => _isRunning;

        /// <summary>当前半场（1=上半场，2=下半场）。</summary>
        public int CurrentHalf => _currentHalf;

        /// <summary>是否处于伤停补时阶段。</summary>
        public bool IsStoppageTime => _isStoppageTime;

        /// <summary>本半场已计算的伤停补时分钟数。</summary>
        public float StoppageMinutes => _calculatedStoppageMinutes;

        /// <summary>半场时长（分钟）。</summary>
        public float HalfDuration => _halfDurationMinutes;

        /// <summary>
        /// 启动计时器：从上半场 0 分钟开始计时。
        /// </summary>
        public void Start()
        {
            _currentTimeMinutes = 0f;
            _currentHalf = 1;
            _isPaused = false;
            _isRunning = true;
            _isStoppageTime = false;
            _calculatedStoppageMinutes = 0f;
            _foulCount = 0;
            _goalCount = 0;
        }

        /// <summary>
        /// 暂停计时。
        /// </summary>
        public void Pause()
        {
            if (_isRunning)
            {
                _isPaused = true;
            }
        }

        /// <summary>
        /// 恢复计时。
        /// </summary>
        public void Resume()
        {
            if (_isRunning)
            {
                _isPaused = false;
            }
        }

        /// <summary>
        /// 重置计时器到初始状态（未运行）。
        /// </summary>
        public void Reset()
        {
            _currentTimeMinutes = 0f;
            _currentHalf = 1;
            _isPaused = true;
            _isRunning = false;
            _isStoppageTime = false;
            _calculatedStoppageMinutes = 0f;
            _foulCount = 0;
            _goalCount = 0;
        }

        /// <summary>
        /// 进入下一半场：重置当前半场时间，切换半场号，清零补时统计。
        /// </summary>
        public void NextHalf()
        {
            _currentHalf++;
            _currentTimeMinutes = 0f;
            _isStoppageTime = false;
            _calculatedStoppageMinutes = 0f;
            _foulCount = 0;
            _goalCount = 0;
            _isPaused = false;
        }

        /// <summary>
        /// 记录一次犯规（用于伤停补时计算）。
        /// </summary>
        public void RecordFoulForStoppage()
        {
            _foulCount++;
        }

        /// <summary>
        /// 记录一次进球（用于伤停补时计算）。
        /// </summary>
        public void RecordGoalForStoppage()
        {
            _goalCount++;
        }

        /// <summary>
        /// 手动触发进入伤停补时阶段。
        /// 计算补时时长并标记进入补时。
        /// </summary>
        public void StartStoppageTime()
        {
            _calculatedStoppageMinutes = CalculateStoppageTime();
            _isStoppageTime = true;
            _currentTimeMinutes = _halfDurationMinutes; // 锁定到半场结束时间
        }

        /// <summary>
        /// Update：累计比赛时间，检测半场结束与补时结束。
        /// </summary>
        private void Update()
        {
            if (_isPaused || !_isRunning)
            {
                return;
            }

            // 累计时间：现实秒 * timeScale = 比赛秒，再 / 60 = 比赛分钟
            _currentTimeMinutes += Time.deltaTime * _timeScale / 60f;

            if (!_isStoppageTime)
            {
                // 正常比赛阶段：检查是否到达半场结束时间
                if (_currentTimeMinutes >= _halfDurationMinutes)
                {
                    // 进入伤停补时
                    StartStoppageTime();
                }
            }
            else
            {
                // 伤停补时阶段：检查是否补时结束
                float stoppageEnd = _halfDurationMinutes + _calculatedStoppageMinutes;
                if (_currentTimeMinutes >= stoppageEnd)
                {
                    // 补时结束，锁定时间并暂停
                    _currentTimeMinutes = stoppageEnd;
                    _isPaused = true;

                    // 通知半场结束
                    OnHalfEnded?.Invoke(_currentHalf);
                }
            }
        }

        /// <summary>
        /// 根据本半场犯规数与进球数计算伤停补时分钟数。
        /// 公式：基础补时 + 犯规数 * 每犯规补时 + 进球数 * 每进球补时，上限不超过 _stoppageMaxMinutes。
        /// </summary>
        /// <returns>伤停补时分钟数</returns>
        private float CalculateStoppageTime()
        {
            float stoppage = _stoppageBaseMinutes
                             + _foulCount * _stoppagePerFoul
                             + _goalCount * _stoppagePerGoal;
            return Mathf.Min(stoppage, _stoppageMaxMinutes);
        }
    }
}
