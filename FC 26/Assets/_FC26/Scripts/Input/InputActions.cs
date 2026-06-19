using UnityEngine;

namespace FC26.Input
{
    /// <summary>
    /// 移动输入数据：归一化后的二维方向（x=左右, y=前后）。
    /// 由 InputReader 每帧填充，相对输入空间（非世界空间）；
    /// 实际摄像机相对变换由 PlayerController 完成。
    /// </summary>
    public struct MoveInput
    {
        /// <summary>移动方向（x: -1左 ~ +1右, y: -1后 ~ +1前），已归一化。</summary>
        public Vector2 Direction;
    }

    /// <summary>
    /// 动作输入数据：动作类型 + 力度（0~1）。
    /// 用于力度型动作（传球/射门）松开时的快照。
    /// </summary>
    public struct ActionInput
    {
        /// <summary>动作类型。</summary>
        public InputAction Action;
        /// <summary>力度（0~1），由按住时长映射。</summary>
        public float Power;
    }

    // ====================================================================
    // 事件定义：通过 FC26.Core.EventBus 广播，供各系统订阅。
    // 全部为 struct（值类型），避免 GC 装箱。
    // ====================================================================

    /// <summary>移动输入事件：每帧有移动输入时广播。</summary>
    public struct MoveInputEvent
    {
        /// <summary>移动方向（已归一化）。</summary>
        public Vector2 Direction;
    }

    /// <summary>传球事件：鼠标左键松开时触发，携带力度。</summary>
    public struct PassEvent
    {
        /// <summary>力度（0~1）。</summary>
        public float Power;
    }

    /// <summary>射门事件：鼠标右键松开时触发，携带力度。</summary>
    public struct ShootEvent
    {
        /// <summary>力度（0~1）。</summary>
        public float Power;
    }

    /// <summary>直塞事件：按下 Space 时触发。</summary>
    public struct ThroughBallEvent { }

    /// <summary>铲断事件：按下 Left Shift 时触发。</summary>
    public struct TackleEvent { }

    /// <summary>拼抢事件：按下 Left Ctrl/Left Command 时触发。</summary>
    public struct JostleEvent { }

    /// <summary>切换球员事件：Direction=-1 上一名，+1 下一名。</summary>
    public struct SwitchPlayerEvent
    {
        /// <summary>切换方向：-1 上一名，+1 下一名。</summary>
        public int Direction;
    }

    /// <summary>镜头缩放事件：滚轮滚动时触发，携带增量。</summary>
    public struct CameraZoomEvent
    {
        /// <summary>滚轮增量（正=拉近，负=拉远）。</summary>
        public float Delta;
    }

    /// <summary>暂停事件：按下 Esc 时触发。</summary>
    public struct PauseEvent { }
}
