//=============================================================================
// 文件名：IGameState.cs
// 所属模块：Core
// 命名空间：FC26.Core
// 作用：定义游戏状态枚举 GameState 与状态接口 IGameState。
//       GameStateMachine 会根据 GameState 创建/切换对应的 IGameState 实现对象，
//       实现状态行为与状态数据解耦。
//=============================================================================
using System;

namespace FC26.Core
{
    /// <summary>
    /// 游戏全局状态枚举。
    /// 状态切换由 GameStateMachine.SwitchTo 触发，并通过 EventBus 广播 GameStateChangedEvent。
    /// </summary>
    public enum GameState
    {
        /// <summary>主菜单：启动游戏后的初始界面</summary>
        MainMenu,

        /// <summary>球队选择：选择联赛与主客队</summary>
        TeamSelect,

        /// <summary>阵容确认：查看并调整首发/替补</summary>
        Lineup,

        /// <summary>开球：球员就位、开球倒计时</summary>
        KickOff,

        /// <summary>比赛进行中：上下半场正常比赛</summary>
        Playing,

        /// <summary>暂停：用户按 Esc 呼出暂停菜单，比赛逻辑冻结但 UI 响应</summary>
        Paused,

        /// <summary>中场休息：上半场结束后的休息阶段</summary>
        HalfTime,

        /// <summary>全场结束：比赛结束，等待进入赛后数据面板</summary>
        FullTime,

        /// <summary>赛后：显示数据面板与统计</summary>
        PostMatch
    }

    /// <summary>
    /// 游戏状态接口。每个具体状态实现该接口，由 GameStateMachine 调度。
    /// 采用轻量状态对象模式：状态机持有当前 IGameState，切换时调用 OnExit/OnEnter。
    /// </summary>
    public interface IGameState
    {
        /// <summary>当前状态枚举值（便于状态机做映射与调试）</summary>
        GameState State { get; }

        /// <summary>
        /// 进入该状态时调用。参数为上一个状态，便于做过渡动画或上下文判断。
        /// </summary>
        /// <param name="previousState">上一个状态</param>
        void OnEnter(GameState previousState);

        /// <summary>
        /// 每帧更新（仅在状态机处于该状态时调用）。
        /// 注意：暂停状态下 Update 不会被调用，由 GameManager 控制。
        /// </summary>
        void OnUpdate();

        /// <summary>
        /// 物理帧更新（按 FixedUpdate 频率调用）。
        /// </summary>
        void OnFixedUpdate();

        /// <summary>
        /// 离开该状态时调用。参数为下一个状态。
        /// </summary>
        /// <param name="nextState">下一个状态</param>
        void OnExit(GameState nextState);
    }
}
