using UnityEngine;

namespace FC26.Input
{
    /// <summary>
    /// 跨平台适配器：检测 Windows / macOS，并按平台加载默认键位。
    /// macOS 上对易与系统冲突的键（如 Left Ctrl）提供替代键映射
    /// （拼抢键 macOS 默认 LeftCommand，Windows 默认 LeftControl）。
    /// </summary>
    public static class PlatformAdapter
    {
        /// <summary>
        /// 当前是否运行在 macOS（编辑器或打包后）。
        /// </summary>
        public static bool IsMacOS()
        {
            return Application.platform == RuntimePlatform.OSXEditor
                || Application.platform == RuntimePlatform.OSXPlayer;
        }

        /// <summary>
        /// 当前是否运行在 Windows（编辑器或打包后）。
        /// </summary>
        public static bool IsWindows()
        {
            return Application.platform == RuntimePlatform.WindowsEditor
                || Application.platform == RuntimePlatform.WindowsPlayer;
        }

        /// <summary>
        /// 启动时按平台加载默认键位到 KeyBindings。
        /// 先加载跨平台默认键位，再按平台覆盖易冲突键。
        /// 应在 InputReader.Awake 中调用，确保输入读取前键位已就绪。
        /// </summary>
        public static void ApplyPlatformDefaults()
        {
            // 1. 加载跨平台默认键位
            KeyBindings.LoadDefaults();

            // 2. 按平台覆盖拼抢键（Jostle）
            if (IsMacOS())
            {
                // macOS 上 LeftCtrl 常与系统快捷键冲突，拼抢键改用 LeftCommand
                KeyBindings.SetBinding(InputAction.Jostle, KeyCode.LeftCommand);
            }
            else
            {
                // Windows / 其他平台使用 LeftControl
                KeyBindings.SetBinding(InputAction.Jostle, KeyCode.LeftControl);
            }
        }
    }
}
