//=============================================================================
// 文件名：BallEntity.cs
// 所属模块：Ball
// 命名空间：FC26.Ball
// 作用：足球实体脚本，挂载在足球 GameObject 上。
//       持有 Rigidbody 与 SphereCollider（运行时 AddComponent），
//       在 FixedUpdate 中自实现轻量物理：重力积分、速度积分、草地摩擦衰减、
//       地面反弹、球门立柱/横梁碰撞反弹。不依赖第三方物理插件。
// 备注：本脚本通过 Velocity 字段驱动运动，Rigidbody 设为 kinematic，
//       由本脚本完全控制足球行为，避免 Unity 内置物理与自实现物理相互干扰。
//=============================================================================
using UnityEngine;

namespace FC26.Ball
{
    /// <summary>
    /// 足球实体：负责单颗足球的物理表现与状态。
    /// 通过 Velocity/Spin 字段自实现物理积分，Rigidbody 仅作碰撞查询辅助。
    /// </summary>
    public class BallEntity : MonoBehaviour
    {
        // ===== 物理参数 =====
        [Header("物理参数")]
        [Tooltip("当前线速度（米/秒）")]
        [SerializeField] private Vector3 _velocity = Vector3.zero;

        [Tooltip("当前角速度（旋转，弧度/秒）")]
        [SerializeField] private Vector3 _spin = Vector3.zero;

        [Tooltip("草地摩擦系数（每帧水平速度衰减系数，0~1）")]
        [SerializeField] private float _friction = 0.98f;

        [Tooltip("弹性系数（反弹时保留的速度比例，0~1）")]
        [SerializeField] private float _restitution = 0.5f;

        [Tooltip("足球半径（米）")]
        [SerializeField] private float _radius = 0.11f;

        // ===== 内部缓存 =====
        private Rigidbody _rigidbody;
        private SphereCollider _collider;

        // 重力加速度（米/秒²）
        private const float Gravity = -9.81f;

        // 速度阈值：低于此值视为静止，避免无限微小弹跳
        private const float RestThreshold = 0.05f;

        // 空中飞行时的轻微空气阻力系数
        private const float AirDrag = 0.999f;

        /// <summary>当前线速度（米/秒）。</summary>
        public Vector3 Velocity => _velocity;

        /// <summary>当前角速度（弧度/秒）。</summary>
        public Vector3 Spin => _spin;

        /// <summary>草地摩擦系数。</summary>
        public float Friction => _friction;

        /// <summary>弹性系数。</summary>
        public float Restitution => _restitution;

        /// <summary>足球半径（世界空间，米）。</summary>
        public float Radius => _radius;

        /// <summary>关联的 Rigidbody（运行时创建）。</summary>
        public Rigidbody Rigidbody => _rigidbody;

        /// <summary>关联的 SphereCollider（运行时创建）。</summary>
        public SphereCollider Collider => _collider;

        /// <summary>
        /// Unity Awake 回调：运行时创建 Rigidbody 与 SphereCollider，
        /// 并配置为 kinematic，由本脚本完全控制运动。
        /// </summary>
        private void Awake()
        {
            EnsureComponents();
        }

        /// <summary>
        /// 确保 Rigidbody 与 SphereCollider 存在并正确配置。
        /// 若已存在则复用，否则 AddComponent 创建。
        /// 碰撞体半径根据世界半径与当前缩放反推，保证世界空间碰撞半径 == _radius。
        /// </summary>
        private void EnsureComponents()
        {
            // ---- Rigidbody ----
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
                if (_rigidbody == null)
                {
                    _rigidbody = gameObject.AddComponent<Rigidbody>();
                }
            }
            // 设为 kinematic：由本脚本通过 transform 控制位置，
            // 避免 Unity 内置物理对足球施加额外力或重力。
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // ---- SphereCollider ----
            if (_collider == null)
            {
                _collider = GetComponent<SphereCollider>();
                if (_collider == null)
                {
                    _collider = gameObject.AddComponent<SphereCollider>();
                }
            }
            // 根据世界半径与当前缩放反推本地碰撞体半径，
            // 保证世界空间碰撞半径 == _radius（假设均匀缩放且父节点缩放为 1）。
            float scale = transform.localScale.x;
            if (scale <= 0f)
            {
                scale = 1f;
            }
            _collider.radius = _radius / scale;
            _collider.isTrigger = false;
        }

        /// <summary>
        /// FixedUpdate：自实现轻量物理积分。
        /// 顺序：
        ///   1) 重力积分（修改速度）
        ///   2) 球门立柱/横梁碰撞反弹（基于当前位置，修改速度并修正穿透）
        ///   3) 速度积分位移
        ///   4) 地面反弹与草地摩擦（基于新位置）
        ///   5) 写回位置
        /// 在 FixedUpdate 中执行保证物理稳定。
        /// </summary>
        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // 1) 重力积分：仅对垂直速度施加重力
            _velocity.y += Gravity * dt;

            // 2) 球门立柱/横梁碰撞反弹（可能修改 _velocity 与 transform.position）
            BallPhysics.BounceOffPost(this);

            // 3) 速度积分：根据当前速度更新位置
            Vector3 pos = transform.position + _velocity * dt;

            // 4) 地面反弹与草地摩擦
            // 当球心高度低于半径时，认为触地
            if (pos.y < _radius)
            {
                pos.y = _radius;

                // 垂直方向反弹：仅当向下运动时反弹
                if (_velocity.y < 0f)
                {
                    _velocity.y = -_velocity.y * _restitution;

                    // 反弹后若垂直速度过小，直接归零，避免无限微小弹跳
                    if (Mathf.Abs(_velocity.y) < RestThreshold)
                    {
                        _velocity.y = 0f;
                    }
                }

                // 草地摩擦：水平速度衰减（每帧乘以摩擦系数）
                _velocity.x *= _friction;
                _velocity.z *= _friction;

                // 水平速度过小则归零，视为静止
                if (Mathf.Abs(_velocity.x) < RestThreshold)
                {
                    _velocity.x = 0f;
                }
                if (Mathf.Abs(_velocity.z) < RestThreshold)
                {
                    _velocity.z = 0f;
                }
            }
            else
            {
                // 空中飞行时施加轻微空气阻力（更真实）
                _velocity *= AirDrag;
            }

            // 5) 写回位置
            transform.position = pos;
        }

        // ====================================================================
        #region 外部接口

        /// <summary>
        /// 施加持续力（加速度效果）。在下一帧物理积分中生效。
        /// 实现为速度增量：Δv = force * fixedDeltaTime。
        /// </summary>
        /// <param name="force">力向量（单位：米/秒²，按加速度理解）</param>
        public void ApplyForce(Vector3 force)
        {
            _velocity += force * Time.fixedDeltaTime;
        }

        /// <summary>
        /// 施加冲量（瞬时速度变化）。立即改变速度。
        /// </summary>
        /// <param name="impulse">冲量向量（单位：米/秒，直接叠加到速度）</param>
        public void ApplyImpulse(Vector3 impulse)
        {
            _velocity += impulse;
        }

        /// <summary>
        /// 直接设置足球世界坐标（瞬移，不经过物理积分）。
        /// 用于定位球重置等场景。
        /// </summary>
        /// <param name="position">目标世界坐标</param>
        public void SetPosition(Vector3 position)
        {
            Vector3 pos = position;
            // 保证不低于地面
            if (pos.y < _radius)
            {
                pos.y = _radius;
            }
            transform.position = pos;
        }

        /// <summary>
        /// 重置足球到指定位置并清零速度与旋转。
        /// 用于中圈开球、定位球、进球后重置等。
        /// </summary>
        /// <param name="position">重置位置</param>
        public void ResetBall(Vector3 position)
        {
            SetPosition(position);
            _velocity = Vector3.zero;
            _spin = Vector3.zero;
        }

        /// <summary>
        /// 直接设置速度（供物理工具类反弹计算后写回使用）。
        /// </summary>
        /// <param name="velocity">新速度</param>
        public void SetVelocity(Vector3 velocity)
        {
            _velocity = velocity;
        }

        /// <summary>
        /// 直接设置角速度（旋转）。
        /// </summary>
        /// <param name="spin">新角速度</param>
        public void SetSpin(Vector3 spin)
        {
            _spin = spin;
        }

        #endregion
    }
}
