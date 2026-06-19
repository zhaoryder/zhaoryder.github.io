//=============================================================================
// 文件名：GameStateMachine.cs
// 所属模块：Core
// 命名空间：FC26.Core
// 作用：游戏状态机。持有当前 IGameState，提供 SwitchTo(GameState) 切换方法。
//       切换时通过 EventBus 广播 GameStateChangedEvent，供 UI、音频、AI 等模块响应。
// 备注：本类为普通 C# 类（非 MonoBehaviour），由 GameManager 持有并驱动 OnUpdate/OnFixedUpdate。
//       各状态实现类（MainMenuState 等）由其他模块提供；本类仅负责调度，
//       未注册实现的状态会以 NullState 占位，避免空引用。
//=============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FC26.Core
{
    /// <summary>
    /// 游戏状态机。负责状态切换与生命周期回调分发。
    /// </summary>
    public class GameStateMachine
    {
        // 当前激活的状态对象。
        private IGameState _currentState;

        // 状态工厂字典：以 GameState 枚举为键，存储对应的工厂方法（返回 IGameState 实例）。
        // 使用工厂方法而非直接实例，便于在切换时按需创建（部分状态需运行时上下文）。
        private readonly Dictionary<GameState, Func<IGameState>> _stateFactories = new Dictionary<GameState, Func<IGameState>>();

        /// <summary>
        /// 当前状态枚举值。若状态机未启动，返回 MainMenu（默认初始值）。
        /// </summary>
        public GameState CurrentStateEnum => _currentState?.State ?? GameState.MainMenu;

        /// <summary>
        /// 当前状态对象（可能为 null，调用方需判空）。
        /// </summary>
        public IGameState CurrentState => _currentState;

        /// <summary>
        /// 注册状态工厂。在 GameManager 初始化阶段调用，将各模块提供的状态实现注册进来。
        /// </summary>
        /// <param name="state">状态枚举</param>
        /// <param name="factory">工厂方法，返回该状态的实例</param>
        public void RegisterState(GameState state, Func<IGameState> factory)
        {
            if (factory == null)
            {
                Debug.LogWarning($"[GameStateMachine] 注册状态 {state} 的工厂为 null，已忽略。");
                return;
            }

            _stateFactories[state] = factory;
        }

        /// <summary>
        /// 启动状态机，进入初始状态。
        /// </summary>
        /// <param name="initialState">初始状态</param>
        public void Start(GameState initialState)
        {
            SwitchTo(initialState);
        }

        /// <summary>
        /// 切换到指定状态。
        /// 流程：旧状态 OnExit -> 创建/获取新状态 -> 新状态 OnEnter -> 广播 GameStateChangedEvent。
        /// </summary>
        /// <param name="newState">目标状态</param>
        public void SwitchTo(GameState newState)
        {
            GameState oldState = CurrentStateEnum;

            // 同状态不重复切换（避免无谓事件）。
            if (_currentState != null && _currentState.State == newState)
            {
                return;
            }

            // 旧状态退出。
            if (_currentState != null)
            {
                try
                {
                    _currentState.OnExit(newState);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GameStateMachine] 旧状态 {oldState} OnExit 异常: {e}");
                }
            }

            // 创建/获取新状态实例。
            IGameState nextState = CreateState(newState);

            _currentState = nextState;

            // 新状态进入。
            if (nextState != null)
            {
                try
                {
                    nextState.OnEnter(oldState);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GameStateMachine] 新状态 {newState} OnEnter 异常: {e}");
                }
            }

            // 广播状态切换事件，供 UI、音频、AI 等模块响应。
            EventBus.Publish(new GameStateChangedEvent
            {
                OldState = oldState,
                NewState = newState
            });

            Debug.Log($"[GameStateMachine] 状态切换: {oldState} -> {newState}");
        }

        /// <summary>
        /// 每帧更新。由 GameManager.Update 调用。仅在非暂停状态下分发。
        /// </summary>
        public void Update()
        {
            if (_currentState == null)
            {
                return;
            }

            try
            {
                _currentState.OnUpdate();
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameStateMachine] {_currentState.State} OnUpdate 异常: {e}");
            }
        }

        /// <summary>
        /// 物理帧更新。由 GameManager.FixedUpdate 调用。仅在非暂停状态下分发。
        /// </summary>
        public void FixedUpdate()
        {
            if (_currentState == null)
            {
                return;
            }

            try
            {
                _currentState.OnFixedUpdate();
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameStateMachine] {_currentState.State} OnFixedUpdate 异常: {e}");
            }
        }

        /// <summary>
        /// 根据枚举创建状态实例。若未注册工厂，则返回 null 并告警。
        /// </summary>
        private IGameState CreateState(GameState state)
        {
            if (_stateFactories.TryGetValue(state, out var factory))
            {
                try
                {
                    return factory();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GameStateMachine] 创建状态 {state} 实例异常: {e}");
                    return null;
                }
            }

            Debug.LogWarning($"[GameStateMachine] 状态 {state} 未注册工厂，将进入空状态。请各模块在 GameManager 初始化时调用 RegisterState。");
            return null;
        }
    }
}
