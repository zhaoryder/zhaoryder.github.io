//=============================================================================
// 文件名：PlayerController.cs
// 所属模块：Player
// 命名空间：FC26.Player
// 作用：球员控制器，管理球员移动与动作。
//       提供 Move（相对摄像机移动）、Pass/Shoot/ThroughBall（传球/射门/直塞）、
//       Tackle（铲断）、Jostle（拼抢）、Dribble（带球）等接口。
//       调用 BallManager 执行传球/射门/直塞，持有 PlayerStateMachine 引用驱动状态切换。
// 备注：移动逻辑在 FixedUpdate 中执行（物理稳定），动作触发在 Update 中响应输入。
//       能力值影响移动速度、动作成功率等参数。
//       用户控制时由 InputReader 驱动；AI 控制时由 AI 模块直接调用本类接口。
//=============================================================================
using UnityEngine;
using FC26.Ball;
using FC26.Input;

namespace FC26.Player
{
    /// <summary>
    /// 球员控制器：管理移动与动作，桥接状态机、动画与足球管理器。
    /// </summary>
    [RequireComponent(typeof(PlayerEntity))]
    [RequireComponent(typeof(PlayerStateMachine))]
    public class PlayerController : MonoBehaviour
    {
        // ===== 移动参数 =====

        [Header("移动参数")]
        [Tooltip("基础移动速度（米/秒，能力值 Speed=99 时的最高速度）")]
        [SerializeField] private float _baseMaxSpeed = 7f;

        [Tooltip("最低移动速度（米/秒，能力值 Speed=0 时）")]
        [SerializeField] private float _minMoveSpeed = 3f;

        [Tooltip("转向平滑速度（度/秒）")]
        [SerializeField] private float _rotationSpeed = 720f;

        [Tooltip("带球速度衰减系数（带球时速度乘以该系数）")]
        [SerializeField] private float _dribbleSpeedFactor = 0.85f;

        [Tooltip("铲断冲刺速度倍率")]
        [SerializeField] private float _tackleSpeedMultiplier = 1.5f;

        // ===== 动作参数 =====

        [Header("动作参数")]
        [Tooltip("铲断成功距离（米）")]
        [SerializeField] private float _tackleRange = 1.5f;

        [Tooltip("拼抢有效距离（米）")]
        [SerializeField] private float _jostleRange = 1.2f;

        [Tooltip("球员碰撞半径（米，用于球-球员碰撞）")]
        [SerializeField] private float _playerRadius = 0.4f;

        // ===== 引用 =====

        [Header("引用")]
        [SerializeField] private PlayerEntity _entity;
        [SerializeField] private PlayerStateMachine _stateMachine;
        [SerializeField] private PlayerAnimator _animator;

        // ===== 内部缓存 =====

        // 当前期望移动方向（世界空间，已归一化），由 Move 设置
        private Vector3 _moveDirection = Vector3.zero;

        // 当前移动速度（米/秒），由能力值与状态决定
        private float _currentMoveSpeed = 0f;

        // 是否带球中（由 BallManager 控球归属决定）
        private bool _hasBall = false;

        // 缓存 BallManager 引用（避免每帧 Instance 查找）
        private BallManager _ballManager;

        /// <summary>所属球员实体。</summary>
        public PlayerEntity Entity => _entity;

        /// <summary>状态机引用。</summary>
        public PlayerStateMachine StateMachine => _stateMachine;

        /// <summary>动画驱动器引用。</summary>
        public PlayerAnimator Animator => _animator;

        /// <summary>是否带球中。</summary>
        public bool HasBall => _hasBall;

        /// <summary>球员碰撞半径。</summary>
        public float PlayerRadius => _playerRadius;

        /// <summary>
        /// Unity Awake：缓存组件引用。
        /// </summary>
        private void Awake()
        {
            if (_entity == null)
            {
                _entity = GetComponent<PlayerEntity>();
            }
            if (_stateMachine == null)
            {
                _stateMachine = GetComponent<PlayerStateMachine>();
            }
            if (_animator == null)
            {
                _animator = GetComponent<PlayerAnimator>();
            }
        }

        /// <summary>
        /// Start：缓存 BallManager 引用，初始化状态机。
        /// </summary>
        private void Start()
        {
            _ballManager = BallManager.Instance;

            // 初始化状态机（传入实体与动画引用）
            if (_stateMachine != null && _entity != null)
            {
                _stateMachine.Initialize(_entity, _animator);
            }
        }

        /// <summary>
        /// Unity Update：用户控制时读取输入并驱动动作。
        /// 仅当 IsUserControlled 为 true 且非眩晕/动作锁定时响应输入。
        /// </summary>
        private void Update()
        {
            if (_entity == null || !_entity.IsUserControlled)
            {
                return;
            }

            // 眩晕状态下不响应任何输入
            if (_entity.IsStunned)
            {
                return;
            }

            ReadUserInput();
        }

        /// <summary>
        /// FixedUpdate：执行移动（物理积分）。
        /// 移动放在 FixedUpdate 保证物理稳定。
        /// </summary>
        private void FixedUpdate()
        {
            if (_entity == null)
            {
                return;
            }

            // 眩晕状态下不移动
            if (_entity.IsStunned)
            {
                _moveDirection = Vector3.zero;
                return;
            }

            UpdateMovement();
        }

        /// <summary>
        /// 读取用户输入并分发到对应动作。
        /// </summary>
        private void ReadUserInput()
        {
            InputReader input = InputReader.Instance;
            if (input == null)
            {
                return;
            }

            // ---- 移动输入 ----
            // Move 接口已由外部（如 PlayerManager）每帧调用，此处仅处理动作输入

            // ---- 直塞 ----
            if (input.ThroughBallPressed)
            {
                Vector3 dir = FC26.Camera.CameraUtility.GetDirectionFromPlayerToMouse(_entity.Position);
                ThroughBall(dir, 0.8f);
            }

            // ---- 铲断 ----
            if (input.TacklePressed)
            {
                Tackle();
            }

            // ---- 拼抢 ----
            if (input.JostlePressed)
            {
                Jostle();
            }

            // ---- 传球（松开触发）----
            // 传球/射门为力度型动作，松开时由 InputReader 触发事件；
            // 此处通过订阅 C# 事件实现（在 OnEnable 中订阅）。
        }

        /// <summary>
        /// OnEnable：订阅传球/射门松开事件。
        /// </summary>
        private void OnEnable()
        {
            InputReader input = InputReader.Instance;
            if (input != null)
            {
                input.OnPassReleased += HandlePassReleased;
                input.OnShootReleased += HandleShootReleased;
            }
        }

        /// <summary>
        /// OnDisable：取消订阅事件，避免对象销毁后仍接收回调。
        /// </summary>
        private void OnDisable()
        {
            InputReader input = InputReader.Instance;
            if (input != null)
            {
                input.OnPassReleased -= HandlePassReleased;
                input.OnShootReleased -= HandleShootReleased;
            }
        }

        /// <summary>
        /// 传球松开回调：按鼠标方向与累计力度执行传球。
        /// </summary>
        private void HandlePassReleased(float power)
        {
            if (_entity == null || !_entity.IsUserControlled || _entity.IsStunned)
            {
                return;
            }

            Vector3 dir = FC26.Camera.CameraUtility.GetDirectionFromPlayerToMouse(_entity.Position);
            Pass(dir, power);
        }

        /// <summary>
        /// 射门松开回调：按鼠标方向与累计力度执行射门。
        /// </summary>
        private void HandleShootReleased(float power)
        {
            if (_entity == null || !_entity.IsUserControlled || _entity.IsStunned)
            {
                return;
            }

            Vector3 dir = FC26.Camera.CameraUtility.GetDirectionFromPlayerToMouse(_entity.Position);
            Shoot(dir, power);
        }

        // ====================================================================
        #region 移动

        /// <summary>
        /// 移动球员：相对摄像机的世界方向移动（WASD）。
        /// 由外部每帧调用（用户控制时由 PlayerManager 调用，AI 控制时由 AI 调用）。
        /// </summary>
        /// <param name="direction">二维输入方向（x=-1左~+1右, y=-1后~+1前），相对输入空间</param>
        public void Move(Vector2 direction)
        {
            if (_entity == null || _entity.IsStunned)
            {
                _moveDirection = Vector3.zero;
                return;
            }

            // 将输入转为相对摄像机的世界方向
            _moveDirection = FC26.Camera.CameraUtility.GetMoveDirectionRelativeToCamera(direction);

            // 更新状态机：有移动输入切到 Run/Dribble，无输入切到 Idle
            UpdateMoveState();
        }

        /// <summary>
        /// 带球移动：与 Move 类似，但状态切换为 Dribble，速度受带球衰减影响。
        /// </summary>
        /// <param name="direction">二维输入方向</param>
        public void Dribble(Vector2 direction)
        {
            if (_entity == null || _entity.IsStunned)
            {
                _moveDirection = Vector3.zero;
                return;
            }

            _moveDirection = FC26.Camera.CameraUtility.GetMoveDirectionRelativeToCamera(direction);

            // 带球状态切换
            if (_moveDirection.sqrMagnitude > 1e-6f)
            {
                if (_stateMachine != null && !_stateMachine.IsActionLocked())
                {
                    _stateMachine.ChangeState(PlayerState.Dribble);
                }
            }
            else
            {
                if (_stateMachine != null && !_stateMachine.IsActionLocked())
                {
                    _stateMachine.ChangeState(PlayerState.Idle);
                }
            }
        }

        /// <summary>
        /// 根据移动输入更新状态机（Run/Idle/Dribble）。
        /// </summary>
        private void UpdateMoveState()
        {
            if (_stateMachine == null || _stateMachine.IsActionLocked())
            {
                return;
            }

            if (_moveDirection.sqrMagnitude > 1e-6f)
            {
                // 带球时优先 Dribble 状态，否则 Run
                PlayerState target = _hasBall ? PlayerState.Dribble : PlayerState.Run;
                _stateMachine.ChangeState(target);
            }
            else
            {
                _stateMachine.ChangeState(PlayerState.Idle);
            }
        }

        /// <summary>
        /// FixedUpdate 中执行实际移动：根据方向与速度更新位置，旋转朝向移动方向。
        /// </summary>
        private void UpdateMovement()
        {
            // 计算当前移动速度（受能力值与状态影响）
            _currentMoveSpeed = ComputeMoveSpeed();

            // 通知动画驱动器目标速度
            if (_animator != null)
            {
                float speedNorm = _moveDirection.sqrMagnitude > 1e-6f
                    ? _currentMoveSpeed / _baseMaxSpeed
                    : 0f;
                _animator.SetMoveSpeed(speedNorm);
            }

            // 无移动输入时直接返回
            if (_moveDirection.sqrMagnitude < 1e-6f)
            {
                return;
            }

            // 位移：position += direction * speed * dt
            Vector3 pos = transform.position;
            float dt = Time.fixedDeltaTime;
            pos += _moveDirection * _currentMoveSpeed * dt;

            // 保持地面高度（y=0，球员脚部贴地）
            pos.y = 0f;

            transform.position = pos;

            // 旋转朝向移动方向（平滑转向）
            Quaternion targetRot = Quaternion.LookRotation(_moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, _rotationSpeed * dt);
        }

        /// <summary>
        /// 计算当前移动速度（米/秒）。
        /// 基础速度由能力值 Speed 决定（线性映射到 _minMoveSpeed~_baseMaxSpeed），
        /// 带球时乘以衰减系数，铲断时乘以冲刺倍率，体能不足时进一步衰减。
        /// </summary>
        /// <returns>当前移动速度</returns>
        private float ComputeMoveSpeed()
        {
            if (_entity == null)
            {
                return 0f;
            }

            // 能力值映射：Speed 0~99 → _minMoveSpeed~_baseMaxSpeed
            float speedFactor = _entity.Speed / 99f;
            float speed = Mathf.Lerp(_minMoveSpeed, _baseMaxSpeed, speedFactor);

            // 带球衰减
            if (_hasBall)
            {
                speed *= _dribbleSpeedFactor;
            }

            // 铲断冲刺
            if (_stateMachine != null && _stateMachine.CurrentState == PlayerState.Tackle)
            {
                speed *= _tackleSpeedMultiplier;
            }

            // 体能衰减：体能低于 30 时速度线性衰减到 70%
            if (_entity.CurrentStamina < 30f)
            {
                float staminaFactor = Mathf.Lerp(0.7f, 1f, _entity.CurrentStamina / 30f);
                speed *= staminaFactor;
            }

            return speed;
        }

        #endregion

        // ====================================================================
        #region 动作

        /// <summary>
        /// 传球：沿指定方向以指定力度传球。
        /// 调用 BallManager.ExecutePass 执行球的物理飞行。
        /// </summary>
        /// <param name="direction">传球方向（世界坐标，会归一化）</param>
        /// <param name="power">力度 0~1</param>
        public void Pass(Vector3 direction, float power)
        {
            if (_entity == null || _entity.IsStunned)
            {
                return;
            }

            // 动作锁定期间不可再次传球
            if (_stateMachine != null && _stateMachine.IsActionLocked())
            {
                return;
            }

            // 方向归一化（水平面）
            Vector3 dir = new Vector3(direction.x, 0f, direction.z);
            if (dir.sqrMagnitude < 1e-6f)
            {
                // 无方向时朝球员朝向传球
                dir = _entity.Facing;
            }
            else
            {
                dir.Normalize();
            }

            // 切换到 Pass 状态
            if (_stateMachine != null)
            {
                _stateMachine.ChangeState(PlayerState.Pass);
            }

            // 调用 BallManager 执行传球
            if (_ballManager != null)
            {
                _ballManager.ExecutePass(dir, power);
            }

            // 传球后失去带球标记
            _hasBall = false;
        }

        /// <summary>
        /// 射门：沿指定方向以指定力度射门（带抬球角度）。
        /// 调用 BallManager.ExecuteShoot 执行。
        /// </summary>
        /// <param name="direction">射门方向（世界坐标）</param>
        /// <param name="power">力度 0~1</param>
        public void Shoot(Vector3 direction, float power)
        {
            if (_entity == null || _entity.IsStunned)
            {
                return;
            }

            if (_stateMachine != null && _stateMachine.IsActionLocked())
            {
                return;
            }

            Vector3 dir = new Vector3(direction.x, 0f, direction.z);
            if (dir.sqrMagnitude < 1e-6f)
            {
                dir = _entity.Facing;
            }
            else
            {
                dir.Normalize();
            }

            if (_stateMachine != null)
            {
                _stateMachine.ChangeState(PlayerState.Shoot);
            }

            if (_ballManager != null)
            {
                _ballManager.ExecuteShoot(dir, power);
            }

            _hasBall = false;
        }

        /// <summary>
        /// 直塞：低平快速传球。
        /// 调用 BallManager.ExecuteThroughBall 执行。
        /// </summary>
        /// <param name="direction">直塞方向（世界坐标）</param>
        /// <param name="power">力度 0~1</param>
        public void ThroughBall(Vector3 direction, float power)
        {
            if (_entity == null || _entity.IsStunned)
            {
                return;
            }

            if (_stateMachine != null && _stateMachine.IsActionLocked())
            {
                return;
            }

            Vector3 dir = new Vector3(direction.x, 0f, direction.z);
            if (dir.sqrMagnitude < 1e-6f)
            {
                dir = _entity.Facing;
            }
            else
            {
                dir.Normalize();
            }

            if (_stateMachine != null)
            {
                _stateMachine.ChangeState(PlayerState.Pass);
            }

            if (_ballManager != null)
            {
                _ballManager.ExecuteThroughBall(dir, power);
            }

            _hasBall = false;
        }

        /// <summary>
        /// 铲断：向朝向方向冲刺铲断。
        /// 若铲断范围内有对方控球球员，则触发对方眩晕并可能夺回控球权。
        /// </summary>
        public void Tackle()
        {
            if (_entity == null || _entity.IsStunned)
            {
                return;
            }

            if (_stateMachine != null && _stateMachine.IsActionLocked())
            {
                return;
            }

            if (_stateMachine != null)
            {
                _stateMachine.ChangeState(PlayerState.Tackle);
            }

            // 检测铲断范围内是否有球，若球在范围内则尝试夺回控球
            if (_ballManager != null && _ballManager.Ball != null)
            {
                Vector3 ballPos = _ballManager.Ball.transform.position;
                Vector3 myPos = _entity.Position;

                // 铲断方向（朝向）
                Vector3 tackleDir = _entity.Facing;

                // 计算球到球员的向量在铲断方向上的投影距离
                Vector3 toBall = ballPos - myPos;
                float forwardDist = Vector3.Dot(toBall, tackleDir);

                // 球在铲断方向前方且在范围内
                if (forwardDist > 0f && forwardDist <= _tackleRange)
                {
                    // 横向偏差
                    Vector3 lateral = toBall - tackleDir * forwardDist;
                    if (lateral.magnitude <= _playerRadius + 0.3f)
                    {
                        // 铲断成功：夺回控球权
                        _ballManager.SetPossession(_entity.TeamId, _entity.PlayerId);
                        _hasBall = true;
                    }
                }
            }
        }

        /// <summary>
        /// 拼抢/卡位：与附近对方球员进行身体对抗。
        /// 占位实现：切换状态并尝试夺球。
        /// </summary>
        public void Jostle()
        {
            if (_entity == null || _entity.IsStunned)
            {
                return;
            }

            if (_stateMachine != null && _stateMachine.IsActionLocked())
            {
                return;
            }

            if (_stateMachine != null)
            {
                _stateMachine.ChangeState(PlayerState.Jostle);
            }

            // 占位：拼抢范围内若有球则尝试夺回
            if (_ballManager != null && _ballManager.Ball != null)
            {
                Vector3 ballPos = _ballManager.Ball.transform.position;
                Vector3 myPos = _entity.Position;
                float dist = Vector3.Distance(ballPos, myPos);

                if (dist <= _jostleRange)
                {
                    _ballManager.SetPossession(_entity.TeamId, _entity.PlayerId);
                    _hasBall = true;
                }
            }
        }

        #endregion

        // ====================================================================
        #region 控球与外部接口

        /// <summary>
        /// 设置带球状态（由 BallManager 控球归属变化时调用）。
        /// </summary>
        /// <param name="hasBall">是否带球</param>
        public void SetHasBall(bool hasBall)
        {
            _hasBall = hasBall;
        }

        /// <summary>
        /// 触发眩晕状态（被铲倒时调用）。
        /// </summary>
        /// <param name="duration">眩晕持续时间（秒，<=0 使用默认）</param>
        public void Stun(float duration = 0f)
        {
            if (_entity == null)
            {
                return;
            }

            _entity.IsStunned = true;
            if (_stateMachine != null)
            {
                _stateMachine.ChangeState(PlayerState.Stunned);
            }
        }

        /// <summary>
        /// 外部注入引用（由 PlayerFactory 调用）。
        /// </summary>
        public void SetReferences(PlayerEntity entity, PlayerStateMachine stateMachine, PlayerAnimator animator)
        {
            _entity = entity;
            _stateMachine = stateMachine;
            _animator = animator;
        }

        #endregion
    }
}
