//=============================================================================
// 文件名：PlayerAnimator.cs
// 所属模块：Player
// 命名空间：FC26.Player
// 作用：基于状态机参数驱动 Animator 的动画驱动器。
//       将 PlayerState 映射为 Animator 参数（Speed/IsDribbling/IsTackling/IsJostling/IsStunned
//       及触发器 PassTrigger/ShootTrigger），无第三方动画资源依赖。
//       若 Animator 或参数不存在则安全跳过，不报错。
// 备注：本脚本不依赖 AnimatorController 资源文件，运行时通过参数名写入；
//       若场景中未配置 AnimatorController，写入操作将被忽略（Unity 内部安全处理）。
//       提供占位动画事件回调，供后续接入真实动画时扩展。
//=============================================================================
using UnityEngine;

namespace FC26.Player
{
    /// <summary>
    /// 球员动画驱动器：将逻辑状态映射为 Animator 参数。
    /// </summary>
    [RequireComponent(typeof(PlayerEntity))]
    public class PlayerAnimator : MonoBehaviour
    {
        // ===== Animator 参数名常量 =====
        // 集中定义参数名，便于统一修改与避免拼写错误。

        private const string ParamSpeed = "Speed";             // 移动速度（Float, 0~1）
        private const string ParamIsDribbling = "IsDribbling"; // 是否带球（Bool）
        private const string ParamIsTackling = "IsTackling";   // 是否铲断中（Bool）
        private const string ParamIsJostling = "IsJostling";   // 是否拼抢中（Bool）
        private const string ParamIsStunned = "IsStunned";     // 是否眩晕（Bool）
        private const string ParamPassTrigger = "PassTrigger"; // 传球触发器（Trigger）
        private const string ParamShootTrigger = "ShootTrigger"; // 射门触发器（Trigger）

        // ===== 引用 =====

        [Header("引用")]
        [Tooltip("目标 Animator（若为空将自动从子物体获取）")]
        [SerializeField] private Animator _animator;

        [Tooltip("所属球员实体")]
        [SerializeField] private PlayerEntity _entity;

        // ===== 内部缓存 =====

        // 当前移动速度归一化（0~1），由 PlayerController 每帧更新
        private float _currentSpeed = 0f;

        // 目标移动速度（用于平滑插值）
        private float _targetSpeed = 0f;

        // 速度平滑时间
        [Tooltip("速度参数平滑时间（秒）")]
        [SerializeField] private float _speedSmoothTime = 0.1f;

        private float _speedVelocity = 0f;

        /// <summary>
        /// Unity Awake：缓存引用。
        /// </summary>
        private void Awake()
        {
            if (_entity == null)
            {
                _entity = GetComponent<PlayerEntity>();
            }
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }
        }

        /// <summary>
        /// Unity Update：平滑速度参数。
        /// 每帧将当前速度向目标速度插值，写入 Animator 的 Speed 参数。
        /// </summary>
        private void Update()
        {
            if (_animator == null)
            {
                return;
            }

            // 平滑速度
            _currentSpeed = Mathf.SmoothDamp(_currentSpeed, _targetSpeed, ref _speedVelocity, _speedSmoothTime);

            // 写入 Speed 参数（若参数不存在则安全跳过）
            if (HasParameter(ParamSpeed))
            {
                _animator.SetFloat(ParamSpeed, _currentSpeed);
            }
        }

        /// <summary>
        /// 设置当前状态：将 PlayerState 映射为 Animator 参数。
        /// 由 PlayerStateMachine.ChangeState 触发。
        /// </summary>
        /// <param name="state">目标状态</param>
        public void SetState(PlayerState state)
        {
            if (_animator == null)
            {
                return;
            }

            // 先重置所有 Bool 参数（互斥状态）
            SetBoolSafe(ParamIsDribbling, false);
            SetBoolSafe(ParamIsTackling, false);
            SetBoolSafe(ParamIsJostling, false);
            SetBoolSafe(ParamIsStunned, false);

            switch (state)
            {
                case PlayerState.Idle:
                    _targetSpeed = 0f;
                    break;

                case PlayerState.Run:
                    _targetSpeed = 1f;
                    break;

                case PlayerState.Dribble:
                    _targetSpeed = 0.8f;
                    SetBoolSafe(ParamIsDribbling, true);
                    break;

                case PlayerState.Pass:
                    _targetSpeed = 0f;
                    SetTriggerSafe(ParamPassTrigger);
                    break;

                case PlayerState.Shoot:
                    _targetSpeed = 0f;
                    SetTriggerSafe(ParamShootTrigger);
                    break;

                case PlayerState.Tackle:
                    _targetSpeed = 0f;
                    SetBoolSafe(ParamIsTackling, true);
                    break;

                case PlayerState.Jostle:
                    _targetSpeed = 0f;
                    SetBoolSafe(ParamIsJostling, true);
                    break;

                case PlayerState.Stunned:
                    _targetSpeed = 0f;
                    SetBoolSafe(ParamIsStunned, true);
                    break;
            }
        }

        /// <summary>
        /// 设置目标移动速度（0~1），由 PlayerController 在 Move 时调用。
        /// 实际写入 Animator 由 Update 平滑插值完成。
        /// </summary>
        /// <param name="speed">目标速度归一化（0~1）</param>
        public void SetMoveSpeed(float speed)
        {
            _targetSpeed = Mathf.Clamp01(speed);
        }

        // ===== 安全写入方法 =====
        // 以下方法在写入前检查参数是否存在，避免运行时警告。

        /// <summary>
        /// 判断 Animator 是否存在指定名称的参数。
        /// </summary>
        private bool HasParameter(string paramName)
        {
            if (_animator == null)
            {
                return false;
            }

            // 遍历 AnimatorController 的参数列表查找
            AnimatorControllerParameter[] parameters = _animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == paramName)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 安全设置 Bool 参数（参数不存在则跳过）。
        /// </summary>
        private void SetBoolSafe(string paramName, bool value)
        {
            if (HasParameter(paramName))
            {
                _animator.SetBool(paramName, value);
            }
        }

        /// <summary>
        /// 安全触发 Trigger 参数（参数不存在则跳过）。
        /// </summary>
        private void SetTriggerSafe(string paramName)
        {
            if (HasParameter(paramName))
            {
                _animator.SetTrigger(paramName);
            }
        }

        // ===== 占位动画事件回调 =====
        // 以下方法供 Animator 动画事件（Animation Event）调用。
        // 在没有真实动画资源时为空实现，接入真实动画后在 Animator 窗口绑定对应帧即可。

        /// <summary>动画事件：传球动作触球瞬间（用于实际触发球飞行）。</summary>
        public void OnPassContact()
        {
            // 占位：实际由 PlayerController 在调用 Pass 时直接触发 BallManager，
            // 此处保留供动画驱动型实现使用。
        }

        /// <summary>动画事件：射门动作触球瞬间。</summary>
        public void OnShootContact()
        {
            // 占位：同上。
        }

        /// <summary>动画事件：铲断动作到达最低点。</summary>
        public void OnTackleReach()
        {
            // 占位：可用于判定铲断是否成功。
        }

        /// <summary>动画事件：动作结束。</summary>
        public void OnActionEnd()
        {
            // 占位：可用于通知状态机动作播放完毕。
        }
    }
}
