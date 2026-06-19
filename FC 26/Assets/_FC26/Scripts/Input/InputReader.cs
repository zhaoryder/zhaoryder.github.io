using System;
using UnityEngine;
using FC26.Core;

namespace FC26.Input
{
    /// <summary>
    /// 输入读取器（单例）：每帧读取键鼠输入，向各系统广播。
    /// 仅支持键鼠，无任何手柄/Joystick 代码。
    /// 仅使用 Input.GetKey/GetKeyDown + Input.GetMouseButton + Input.GetAxis("Mouse ScrollWheel")，
    /// 不使用 InputSystem Package。
    ///
    /// 力度型动作（传球/射门）：按下开始计时，松开时按累计时长映射力度并触发事件。
    /// 只在游戏 Playing 状态下读取动作输入；暂停时只读 Esc。
    /// </summary>
    public class InputReader : MonoSingleton<InputReader>
    {
        [Header("力度条参数")]
        [Tooltip("按住达到满力度的时长（秒）")]
        [SerializeField] private float _maxHoldTime = 1.5f;

        // ===== 只读属性（供各系统每帧查询）=====

        /// <summary>移动方向（x: -1左~+1右, y: -1后~+1前），已归一化。</summary>
        public Vector2 MoveDirection { get; private set; }

        /// <summary>鼠标左键（传球）是否按住中。</summary>
        public bool IsPassHeld { get; private set; }

        /// <summary>鼠标右键（射门）是否按住中。</summary>
        public bool IsShootHeld { get; private set; }

        /// <summary>传球累计按住时长（秒）。</summary>
        public float PassHoldTime { get; private set; }

        /// <summary>射门累计按住时长（秒）。</summary>
        public float ShootHoldTime { get; private set; }

        /// <summary>传球力度归一化（0~1），供 HUD 力度条实时显示。</summary>
        public float PassPowerNormalized => Mathf.Clamp01(PassHoldTime / _maxHoldTime);

        /// <summary>射门力度归一化（0~1），供 HUD 力度条实时显示。</summary>
        public float ShootPowerNormalized => Mathf.Clamp01(ShootHoldTime / _maxHoldTime);

        /// <summary>本帧是否按下直塞。</summary>
        public bool ThroughBallPressed { get; private set; }

        /// <summary>本帧是否按下铲断。</summary>
        public bool TacklePressed { get; private set; }

        /// <summary>本帧是否按下拼抢。</summary>
        public bool JostlePressed { get; private set; }

        /// <summary>本帧是否按下切换上一名球员。</summary>
        public bool SwitchPrevPressed { get; private set; }

        /// <summary>本帧是否按下切换下一名球员。</summary>
        public bool SwitchNextPressed { get; private set; }

        /// <summary>本帧鼠标滚轮增量（正=拉近，负=拉远）。</summary>
        public float CameraZoomDelta { get; private set; }

        /// <summary>本帧是否按下暂停。</summary>
        public bool PausePressed { get; private set; }

        // ===== C# 事件（带力度参数，供直接订阅；EventBus 事件同步广播）=====

        /// <summary>传球松开事件（力度 0~1）。</summary>
        public event Action<float> OnPassReleased;

        /// <summary>射门松开事件（力度 0~1）。</summary>
        public event Action<float> OnShootReleased;

        // ===== 游戏状态控制 =====

        /// <summary>
        /// 是否处于 Playing 状态。false 时只读 Esc，其余动作输入不读取。
        /// 由 GameManager 在状态切换时调用。
        /// </summary>
        private bool _isPlaying = true;

        /// <summary>
        /// 设置游戏是否处于 Playing 状态。
        /// 进入非 Playing 状态时会归零力度计时，避免残留触发。
        /// </summary>
        public void SetGamePlaying(bool playing)
        {
            _isPlaying = playing;
            if (!playing)
            {
                ResetHoldTimers();
            }
        }

        /// <summary>
        /// Awake：调用基类完成单例初始化，并按平台加载默认键位。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            PlatformAdapter.ApplyPlatformDefaults();
            // 防止 Inspector 中误配为 0 导致力度计算除零产生 NaN
            _maxHoldTime = Mathf.Max(_maxHoldTime, 0.01f);
        }

        private void Update()
        {
            // 暂停键始终读取（无论是否 Playing）
            ReadPauseInput();

            if (!_isPlaying)
            {
                // 非 Playing 状态：只读 Esc，清空其余动作属性，归零力度计时
                ResetActionStates();
                return;
            }

            ReadMoveInput();
            ReadPassInput();
            ReadShootInput();
            ReadActionButtons();
            ReadSwitchPlayer();
            ReadCameraZoom();
        }

        /// <summary>
        /// 读取跑动输入（WASD 专用，相对输入空间）。
        /// W=前(+y), S=后(-y), A=左(-x), D=右(+x)，合成后归一化。
        /// WASD 仅用于跑动，不与任何动作键复用。
        /// </summary>
        private void ReadMoveInput()
        {
            float x = 0f;
            float y = 0f;

            if (Input.GetKey(KeyBindings.GetBinding(InputAction.MoveForward))) y += 1f;
            if (Input.GetKey(KeyBindings.GetBinding(InputAction.MoveBack))) y -= 1f;
            if (Input.GetKey(KeyBindings.GetBinding(InputAction.MoveLeft))) x -= 1f;
            if (Input.GetKey(KeyBindings.GetBinding(InputAction.MoveRight))) x += 1f;

            Vector2 dir = new Vector2(x, y);
            // 对角线输入归一化，避免对角跑动比直线快
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            MoveDirection = dir;

            // 有移动输入时广播事件
            if (dir.sqrMagnitude > 0.001f)
            {
                EventBus.Publish(new MoveInputEvent { Direction = dir });
            }
        }

        /// <summary>
        /// 读取传球输入（鼠标左键，按住时长决定力度，可长传）。
        /// 按下开始计时，松开时映射力度并触发事件。
        /// </summary>
        private void ReadPassInput()
        {
            int button = KeyBindings.GetMouseBinding(InputAction.Pass);

            if (Input.GetMouseButtonDown(button))
            {
                IsPassHeld = true;
                PassHoldTime = 0f;
            }

            if (IsPassHeld && Input.GetMouseButton(button))
            {
                PassHoldTime += Time.deltaTime;
            }

            if (IsPassHeld && Input.GetMouseButtonUp(button))
            {
                float power = Mathf.Clamp01(PassHoldTime / _maxHoldTime);
                OnPassReleased?.Invoke(power);
                EventBus.Publish(new PassEvent { Power = power });
                IsPassHeld = false;
                PassHoldTime = 0f;
            }
        }

        /// <summary>
        /// 读取射门输入（鼠标右键，按住时长决定力度）。
        /// 逻辑同传球。
        /// </summary>
        private void ReadShootInput()
        {
            int button = KeyBindings.GetMouseBinding(InputAction.Shoot);

            if (Input.GetMouseButtonDown(button))
            {
                IsShootHeld = true;
                ShootHoldTime = 0f;
            }

            if (IsShootHeld && Input.GetMouseButton(button))
            {
                ShootHoldTime += Time.deltaTime;
            }

            if (IsShootHeld && Input.GetMouseButtonUp(button))
            {
                float power = Mathf.Clamp01(ShootHoldTime / _maxHoldTime);
                OnShootReleased?.Invoke(power);
                EventBus.Publish(new ShootEvent { Power = power });
                IsShootHeld = false;
                ShootHoldTime = 0f;
            }
        }

        /// <summary>
        /// 读取动作键：直塞(Space)、铲断(Left Shift)、拼抢(Left Ctrl/Cmd)。
        /// 均为按下触发（GetKeyDown）。
        /// </summary>
        private void ReadActionButtons()
        {
            // 直塞
            ThroughBallPressed = Input.GetKeyDown(KeyBindings.GetBinding(InputAction.ThroughBall));
            if (ThroughBallPressed)
            {
                EventBus.Publish(new ThroughBallEvent());
            }

            // 铲断
            TacklePressed = Input.GetKeyDown(KeyBindings.GetBinding(InputAction.Tackle));
            if (TacklePressed)
            {
                EventBus.Publish(new TackleEvent());
            }

            // 拼抢
            JostlePressed = Input.GetKeyDown(KeyBindings.GetBinding(InputAction.Jostle));
            if (JostlePressed)
            {
                EventBus.Publish(new JostleEvent());
            }
        }

        /// <summary>
        /// 读取切换球员：Q=上一名(-1)，E=下一名(+1)。
        /// </summary>
        private void ReadSwitchPlayer()
        {
            SwitchPrevPressed = Input.GetKeyDown(KeyBindings.GetBinding(InputAction.SwitchPrev));
            if (SwitchPrevPressed)
            {
                EventBus.Publish(new SwitchPlayerEvent { Direction = -1 });
            }

            SwitchNextPressed = Input.GetKeyDown(KeyBindings.GetBinding(InputAction.SwitchNext));
            if (SwitchNextPressed)
            {
                EventBus.Publish(new SwitchPlayerEvent { Direction = 1 });
            }
        }

        /// <summary>
        /// 读取镜头缩放（鼠标滚轮）。
        /// </summary>
        private void ReadCameraZoom()
        {
            float delta = Input.GetAxis("Mouse ScrollWheel");
            CameraZoomDelta = delta;
            if (Mathf.Abs(delta) > 0.0001f)
            {
                EventBus.Publish(new CameraZoomEvent { Delta = delta });
            }
        }

        /// <summary>
        /// 读取暂停键（Esc），始终读取（无论是否 Playing）。
        /// </summary>
        private void ReadPauseInput()
        {
            PausePressed = Input.GetKeyDown(KeyBindings.GetBinding(InputAction.Pause));
            if (PausePressed)
            {
                EventBus.Publish(new PauseEvent());
            }
        }

        /// <summary>
        /// 归零力度计时（用于暂停/状态切换）。
        /// </summary>
        private void ResetHoldTimers()
        {
            IsPassHeld = false;
            IsShootHeld = false;
            PassHoldTime = 0f;
            ShootHoldTime = 0f;
        }

        /// <summary>
        /// 非 Playing 状态下清空所有动作属性（保留 PausePressed）。
        /// </summary>
        private void ResetActionStates()
        {
            MoveDirection = Vector2.zero;
            ThroughBallPressed = false;
            TacklePressed = false;
            JostlePressed = false;
            SwitchPrevPressed = false;
            SwitchNextPressed = false;
            CameraZoomDelta = 0f;
            ResetHoldTimers();
        }
    }
}
