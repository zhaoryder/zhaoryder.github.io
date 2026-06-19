//=============================================================================
// 文件名：EventBus.cs
// 所属模块：Core
// 命名空间：FC26.Core
// 作用：全局事件总线（Event Bus）。提供类型安全的 Subscribe / Unsubscribe / Publish
//       泛型 API，无装箱开销。所有跨模块通信（如进球、暂停、控球变化、状态切换）
//       均通过事件总线解耦。
// 备注：本类为静态单例（非 MonoBehaviour），生命周期与 AppDomain 一致；
//       所有方法均线程安全（使用 lock 保护内部字典）。
//       事件结构体（GameStateChangedEvent 等）统一定义在本文件末尾，便于查阅。
//=============================================================================
using System;
using System.Collections.Generic;

namespace FC26.Core
{
    /// <summary>
    /// 全局事件总线静态类。
    /// 使用泛型 API 保证类型安全、避免值类型装箱。
    /// 订阅者通过 Subscribe&lt;T&gt; 注册回调，发布者通过 Publish&lt;T&gt; 触发。
    /// </summary>
    public static class EventBus
    {
        // 内部订阅字典：以事件类型为键，存储该类型对应的回调列表。
        private static readonly Dictionary<Type, object> _subscribers = new Dictionary<Type, object>();

        // 同步锁对象，保证多线程访问安全。
        private static readonly object _lock = new object();

        /// <summary>
        /// 订阅指定类型事件。
        /// </summary>
        /// <typeparam name="T">事件类型（结构体或类）</typeparam>
        /// <param name="handler">回调委托</param>
        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null)
            {
                return;
            }

            lock (_lock)
            {
                Type key = typeof(T);
                if (_subscribers.TryGetValue(key, out var existing))
                {
                    // 将新回调合并到现有委托链。
                    _subscribers[key] = (Action<T>)Delegate.Combine((Action<T>)existing, handler);
                }
                else
                {
                    _subscribers[key] = handler;
                }
            }
        }

        /// <summary>
        /// 取消订阅指定类型事件。
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">要移除的回调委托</param>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null)
            {
                return;
            }

            lock (_lock)
            {
                Type key = typeof(T);
                if (!_subscribers.TryGetValue(key, out var existing))
                {
                    return;
                }

                var newDelegate = (Action<T>)Delegate.Remove((Action<T>)existing, handler);

                if (newDelegate == null)
                {
                    _subscribers.Remove(key);
                }
                else
                {
                    _subscribers[key] = newDelegate;
                }
            }
        }

        /// <summary>
        /// 发布指定类型事件。同步调用所有订阅者。
        /// 注意：在主线程调用，订阅者回调中不应执行耗时操作。
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        public static void Publish<T>(T eventData) where T : struct
        {
            Action<T> handlers;
            lock (_lock)
            {
                if (!_subscribers.TryGetValue(typeof(T), out var existing))
                {
                    return;
                }

                // 拷贝一份委托引用，避免回调中再次订阅/取消订阅导致字典被修改。
                handlers = (Action<T>)existing;
            }

            // 在锁外触发，避免回调中再次调用 Publish 造成死锁。
            handlers?.Invoke(eventData);
        }

        /// <summary>
        /// 清空所有订阅（场景切换或应用退出时调用，避免内存泄漏）。
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _subscribers.Clear();
            }
        }
    }

    //=========================================================================
    // 以下为全局事件结构体定义。
    // 所有跨模块通信事件集中在此声明，便于查阅与维护。
    // 约定：事件名以 Event 结尾；使用 struct（值类型）避免 GC 开销。
    //=========================================================================

    /// <summary>
    /// 游戏状态切换事件。由 GameStateMachine.SwitchTo 触发。
    /// </summary>
    public struct GameStateChangedEvent
    {
        /// <summary>切换前的状态</summary>
        public GameState OldState;

        /// <summary>切换后的状态</summary>
        public GameState NewState;
    }

    /// <summary>
    /// 进球事件。当球整体越过球门线时由裁判/物理系统发布。
    /// </summary>
    public struct GoalScoredEvent
    {
        /// <summary>得分队伍 ID（0=主队，1=客队）</summary>
        public int TeamId;

        /// <summary>进球球员 ID</summary>
        public int ScorerPlayerId;

        /// <summary>比赛时间（秒）</summary>
        public float MatchTime;
    }

    /// <summary>
    /// 比赛开始事件。由 KickOff 状态进入 Playing 时发布。
    /// </summary>
    public struct MatchStartedEvent
    {
    }

    /// <summary>
    /// 比赛结束事件。由 FullTime 状态发布。
    /// </summary>
    public struct MatchEndedEvent
    {
    }

    /// <summary>
    /// 暂停状态切换事件。用户按 Esc 或系统触发暂停时发布。
    /// </summary>
    public struct PauseToggledEvent
    {
        /// <summary>true=已暂停，false=已恢复</summary>
        public bool Paused;
    }

    /// <summary>
    /// 控球权变化事件。当球被某球员控制或失去控制时发布。
    /// </summary>
    public struct BallPossessionChangedEvent
    {
        /// <summary>当前控球队伍 ID（-1 表示无人控球）</summary>
        public int TeamId;

        /// <summary>当前控球球员 ID（-1 表示无人控球）</summary>
        public int PlayerId;
    }
}
