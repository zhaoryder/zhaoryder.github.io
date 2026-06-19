//=============================================================================
// 文件名：PlayerStateMachine.cs
// 所属模块：Player
// 命名空间：FC26.Player
// 作用：球员动作状态机。维护球员当前动作状态（Idle/Run/Dribble/Pass/Shoot/Tackle/Jostle/Stunned），
//       提供状态切换接口 ChangeState，并在 Update 中根据当前状态执行对应逻辑（如动画参数刷新、
//       状态超时自动回到 Idle）。
// 备注：本状态机为轻量自实现，不依赖 Animator 状态机，便于 AI 与用户控制共用。
//       动画参数的实际写入由 PlayerAnimator 完成，本类仅维护逻辑状态。
//=============================================================================
using UnityEngine;

namespace FC26.Player
{
    /// <summary>
    /// 球员动作状态枚举。
    /// </summary>
    public enum PlayerState
    {
        Idle,    // 站立待机
        Run,     // 跑动（无球）
        Dribble, // 带球跑动
        Pass,    // 传球动作（短暂状态，完成后自动回到 Idle/Run）
        Shoot,   // 射门动作（短暂状态）
        Tackle,  // 铲断动作（短暂状态，可能进入 Stunned）
        Jostle,  // 拼抢/卡位
        Stunned  // 眩晕（被铲倒后短暂无法动作）
    }

    /// <summary>
    /// 球员动作状态机：维护状态切换与状态内逻辑。
    /// </summary>
    public class PlayerStateMachine : MonoBehaviour
    {
        // ===== 配置参数 =====

        [Header("状态时长（秒）")]
        [Tooltip("传球动作持续时间")]
        [SerializeField] private float _passDuration = 0.3f;

        [Tooltip("射门动作持续时间")]
        [SerializeField] private float _shootDuration = 0.5f;

        [Tooltip("铲断动作持续时间")]
        [SerializeField] private float _tackleDuration = 0.6f;

        [Tooltip("拼抢动作持续时间")]
        [SerializeField] private float _jostleDuration = 0.4f;

        [Tooltip("眩晕状态持续时间（被铲倒后）")]
        [SerializeField] private float _stunnedDuration = 1.2f;

        // ===== 状态 =====

        /// <summary>当前状态。</summary>
        public PlayerState CurrentState { get; private set; } = PlayerState.Idle;

        /// <summary>当前状态已持续时间（秒）。</summary>
        public float StateTimer { get; private set; } = 0f;

        /// <summary>上一个状态（用于状态恢复参考）。</summary>
        public PlayerState PreviousState { get; private set; } = PlayerState.Idle;

        // ===== 回调 =====

        /// <summary>状态切换回调（参数：旧状态, 新状态）。供 PlayerAnimator 等订阅。</summary>
        public event System.Action<PlayerState, PlayerState> OnStateChanged;

        // ===== 内部缓存 =====

        private PlayerEntity _entity;
        private PlayerAnimator _animator;

        /// <summary>
        /// 初始化状态机：缓存引用。
        /// </summary>
        /// <param name="entity">所属球员实体</param>
        /// <param name="animator">动画驱动器（可为空）</param>
        public void Initialize(PlayerEntity entity, PlayerAnimator animator)
        {
            _entity = entity;
            _animator = animator;
        }

        /// <summary>
        /// Unity Update：根据当前状态执行逻辑。
        /// 短暂状态（Pass/Shoot/Tackle/Jostle/Stunned）在超时后自动回到 Idle。
        /// </summary>
        private void Update()
        {
            if (_entity == null)
            {
                return;
            }

            StateTimer += Time.deltaTime;

            switch (CurrentState)
            {
                case PlayerState.Pass:
                    // 传球动作完成后回到 Idle
                    if (StateTimer >= _passDuration)
                    {
                        ChangeState(PlayerState.Idle);
                    }
                    break;

                case PlayerState.Shoot:
                    // 射门动作完成后回到 Idle
                    if (StateTimer >= _shootDuration)
                    {
                        ChangeState(PlayerState.Idle);
                    }
                    break;

                case PlayerState.Tackle:
                    // 铲断动作完成后回到 Idle
                    if (StateTimer >= _tackleDuration)
                    {
                        ChangeState(PlayerState.Idle);
                    }
                    break;

                case PlayerState.Jostle:
                    // 拼抢动作完成后回到 Idle
                    if (StateTimer >= _jostleDuration)
                    {
                        ChangeState(PlayerState.Idle);
                    }
                    break;

                case PlayerState.Stunned:
                    // 眩晕结束后回到 Idle 并清除眩晕标记
                    if (StateTimer >= _stunnedDuration)
                    {
                        _entity.IsStunned = false;
                        ChangeState(PlayerState.Idle);
                    }
                    break;

                // Idle/Run/Dribble 为持续状态，由外部输入驱动切换，此处不做超时处理
                case PlayerState.Idle:
                case PlayerState.Run:
                case PlayerState.Dribble:
                    break;
            }
        }

        /// <summary>
        /// 切换状态。
        /// 若新旧状态相同则不触发切换（避免重复刷新动画）。
        /// 切换时会重置状态计时器，记录上一个状态，并触发 OnStateChanged 回调。
        /// </summary>
        /// <param name="newState">目标状态</param>
        public void ChangeState(PlayerState newState)
        {
            if (CurrentState == newState)
            {
                return;
            }

            PreviousState = CurrentState;
            CurrentState = newState;
            StateTimer = 0f;

            // 眩晕状态进入时标记实体
            if (newState == PlayerState.Stunned && _entity != null)
            {
                _entity.IsStunned = true;
            }

            // 触发回调，通知动画驱动器刷新参数
            OnStateChanged?.Invoke(PreviousState, newState);

            // 同步动画参数
            if (_animator != null)
            {
                _animator.SetState(newState);
            }
        }

        /// <summary>
        /// 强制重置到 Idle 状态（不清除眩晕标记，由眩晕计时自然恢复）。
        /// 用于中断当前动作（如失去控球权）。
        /// </summary>
        public void ResetToIdle()
        {
            if (CurrentState == PlayerState.Stunned)
            {
                // 眩晕状态不可被外部中断
                return;
            }
            ChangeState(PlayerState.Idle);
        }

        /// <summary>
        /// 判断当前是否处于动作锁定状态（无法接受新动作输入）。
        /// Pass/Shoot/Tackle/Jostle/Stunned 期间视为锁定。
        /// </summary>
        /// <returns>true=锁定中，false=可接受新动作</returns>
        public bool IsActionLocked()
        {
            switch (CurrentState)
            {
                case PlayerState.Pass:
                case PlayerState.Shoot:
                case PlayerState.Tackle:
                case PlayerState.Jostle:
                case PlayerState.Stunned:
                    return true;
                default:
                    return false;
            }
        }
    }
}
