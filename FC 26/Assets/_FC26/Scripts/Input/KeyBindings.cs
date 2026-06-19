using System.Collections.Generic;
using UnityEngine;

namespace FC26.Input
{
    /// <summary>
    /// 输入动作枚举：定义游戏中所有可绑定的输入动作。
    /// W/A/S/D 仅用于跑动（MoveForward/MoveBack/MoveLeft/MoveRight），不与任何动作键复用，避免键位冲突。
    /// </summary>
    public enum InputAction
    {
        // ===== 跑动（WASD 专用，不与动作键复用）=====
        MoveForward, // 向前跑动（默认 W）
        MoveBack,    // 向后跑动（默认 S）
        MoveLeft,    // 向左跑动（默认 A）
        MoveRight,   // 向右跑动（默认 D）

        // ===== 动作键 =====
        Pass,        // 传球（鼠标左键，按住时长决定力度，可长传）
        Shoot,       // 射门（鼠标右键，按住时长决定力度）
        ThroughBall, // 直塞（默认 Space）
        Tackle,      // 铲断（默认 Left Shift）
        Jostle,      // 拼抢（Windows 默认 LeftControl，macOS 默认 LeftCommand）

        // ===== 切换球员 =====
        SwitchPrev,  // 切换上一名球员（默认 Q）
        SwitchNext,  // 切换下一名球员（默认 E）

        // ===== 镜头与系统 =====
        CameraZoom,  // 镜头缩放（鼠标滚轮，无 KeyCode 绑定）
        Pause        // 暂停（默认 Esc）
    }

    /// <summary>
    /// 键位绑定中心：维护运行时可重绑定的键位字典。
    /// 键盘动作使用 KeyCode 字典；鼠标动作（传球/射门）使用鼠标按键索引字典。
    /// 鼠标按键常量：左键 0 / 右键 1 / 中键 2（与 Unity Input.GetMouseButton 的 button 参数一致）。
    /// </summary>
    public static class KeyBindings
    {
        // ===== 鼠标按键常量 =====
        public const int MouseButtonLeft = 0;   // 鼠标左键
        public const int MouseButtonRight = 1;  // 鼠标右键
        public const int MouseButtonMiddle = 2; // 鼠标中键

        // 键盘键位字典（InputAction -> KeyCode），运行时可重绑定
        private static readonly Dictionary<InputAction, KeyCode> _keyBindings =
            new Dictionary<InputAction, KeyCode>();

        // 鼠标按键字典（InputAction -> 鼠标按键索引），运行时可重绑定
        private static readonly Dictionary<InputAction, int> _mouseBindings =
            new Dictionary<InputAction, int>();

        /// <summary>
        /// 静态构造：首次访问时加载跨平台默认键位。
        /// </summary>
        static KeyBindings()
        {
            LoadDefaults();
        }

        /// <summary>
        /// 加载跨平台默认键位（WASD 跑动 + 标准动作键）。
        /// 注意：拼抢键(Jostle)的跨平台差异由 PlatformAdapter.ApplyPlatformDefaults() 覆盖。
        /// </summary>
        public static void LoadDefaults()
        {
            _keyBindings.Clear();
            _mouseBindings.Clear();

            // 跑动（WASD 专用，不与动作键复用）
            _keyBindings[InputAction.MoveForward] = KeyCode.W;
            _keyBindings[InputAction.MoveBack] = KeyCode.S;
            _keyBindings[InputAction.MoveLeft] = KeyCode.A;
            _keyBindings[InputAction.MoveRight] = KeyCode.D;

            // 动作键
            _keyBindings[InputAction.ThroughBall] = KeyCode.Space;
            _keyBindings[InputAction.Tackle] = KeyCode.LeftShift;
            _keyBindings[InputAction.Jostle] = KeyCode.LeftControl; // 跨平台默认，macOS 由 PlatformAdapter 改为 LeftCommand
            _keyBindings[InputAction.SwitchPrev] = KeyCode.Q;
            _keyBindings[InputAction.SwitchNext] = KeyCode.E;
            _keyBindings[InputAction.Pause] = KeyCode.Escape;

            // 鼠标按键动作（Pass=左键，Shoot=右键）
            _mouseBindings[InputAction.Pass] = MouseButtonLeft;
            _mouseBindings[InputAction.Shoot] = MouseButtonRight;

            // CameraZoom 使用鼠标滚轮，无需 KeyCode 或鼠标按键绑定
        }

        // ===== 键盘键位 get/set =====

        /// <summary>
        /// 获取某动作绑定的键盘按键；若未绑定返回 KeyCode.None。
        /// </summary>
        public static KeyCode GetBinding(InputAction action)
        {
            return _keyBindings.TryGetValue(action, out KeyCode key) ? key : KeyCode.None;
        }

        /// <summary>
        /// 重绑定键盘动作到指定 KeyCode。
        /// </summary>
        public static void SetBinding(InputAction action, KeyCode key)
        {
            _keyBindings[action] = key;
        }

        // ===== 鼠标按键 get/set =====

        /// <summary>
        /// 获取某动作绑定的鼠标按键索引（0=左/1=右/2=中）；若未绑定返回 -1。
        /// </summary>
        public static int GetMouseBinding(InputAction action)
        {
            return _mouseBindings.TryGetValue(action, out int button) ? button : -1;
        }

        /// <summary>
        /// 重绑定鼠标动作到指定鼠标按键索引。
        /// </summary>
        public static void SetMouseBinding(InputAction action, int button)
        {
            _mouseBindings[action] = button;
        }

        /// <summary>
        /// 判断某动作是否为鼠标按键动作。
        /// </summary>
        public static bool IsMouseAction(InputAction action)
        {
            return _mouseBindings.ContainsKey(action);
        }
    }
}
