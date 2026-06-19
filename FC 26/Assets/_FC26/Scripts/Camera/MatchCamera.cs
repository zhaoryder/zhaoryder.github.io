//=============================================================================
// 文件名：MatchCamera.cs
// 所属模块：Camera
// 命名空间：FC26.Camera
// 作用：比赛摄像机单例。继承 MonoSingleton<MatchCamera>，全局唯一。
//       跟随目标（球或控制球员），使用 Lerp 平滑插值位置；
//       鼠标滚轮缩放距离（5-30 米）与俯角（30-60 度）；
//       边界限制摄像机不超出球场范围（±40 x, ±60 z）。
// 备注：摄像机定位采用球面坐标：以目标为圆心，按俯角与距离计算偏移，
//       摄像机始终看向目标。默认跟随足球，可通过 Target 属性切换为控制球员。
//       Update 中读取 InputReader.CameraZoomDelta 调整距离与俯角。
//=============================================================================
using UnityEngine;
using FC26.Core;
using FC26.Input;
using FC26.Ball;

namespace FC26.Camera
{
    /// <summary>
    /// 比赛摄像机单例：跟随目标、平滑插值、滚轮缩放、边界限制。
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class MatchCamera : MonoSingleton<MatchCamera>
    {
        // ===== 跟随目标 =====

        [Header("跟随目标")]
        [Tooltip("跟随目标 Transform。为空时自动跟随足球。")]
        [SerializeField] private Transform _target;

        [Tooltip("目标偏移（相对目标坐标的偏移量，如看向球员腰部高度）")]
        [SerializeField] private Vector3 _targetOffset = new Vector3(0f, 1.2f, 0f);

        // ===== 距离与俯角 =====

        [Header("距离与俯角")]
        [Tooltip("当前摄像机与目标的水平距离（米）")]
        [SerializeField] private float _distance = 12f;

        [Tooltip("最小距离（米）")]
        [SerializeField] private float _minDistance = 5f;

        [Tooltip("最大距离（米）")]
        [SerializeField] private float _maxDistance = 30f;

        [Tooltip("当前俯角（度，从水平面起算）")]
        [SerializeField] private float _pitchAngle = 40f;

        [Tooltip("最小俯角（度）")]
        [SerializeField] private float _minPitch = 30f;

        [Tooltip("最大俯角（度）")]
        [SerializeField] private float _maxPitch = 60f;

        [Tooltip("滚轮缩放距离灵敏度（每单位滚轮增量对应的距离变化）")]
        [SerializeField] private float _zoomDistanceSensitivity = 3f;

        [Tooltip("滚轮缩放俯角灵敏度（每单位滚轮增量对应的角度变化）")]
        [SerializeField] private float _zoomPitchSensitivity = 6f;

        // ===== 平滑参数 =====

        [Header("平滑参数")]
        [Tooltip("位置平滑系数（0~1，越大越快跟随）")]
        [SerializeField] private float _positionLerpFactor = 0.12f;

        [Tooltip("朝向平滑系数（0~1，越大越快转向）")]
        [SerializeField] private float _rotationLerpFactor = 0.15f;

        // ===== 边界限制 =====

        [Header("边界限制")]
        [Tooltip("摄像机 x 轴边界（±，米）")]
        [SerializeField] private float _boundsX = 40f;

        [Tooltip("摄像机 z 轴边界（±，米）")]
        [SerializeField] private float _boundsZ = 60f;

        [Tooltip("摄像机最低高度（米，避免穿地）")]
        [SerializeField] private float _minHeight = 2f;

        // ===== 内部缓存 =====

        private UnityEngine.Camera _camera;

        // 摄像机偏航角（绕 Y 轴），默认 0 表示从目标后方 -z 方向看向 +z
        [Tooltip("摄像机偏航角（度，0=从 -z 看向 +z）")]
        [SerializeField] private float _yawAngle = 0f;

        /// <summary>跟随目标 Transform。设置后摄像机将跟随该目标；置空则回退到跟随足球。</summary>
        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        /// <summary>当前摄像机与目标的水平距离（米）。</summary>
        public float Distance => _distance;

        /// <summary>当前俯角（度）。</summary>
        public float PitchAngle => _pitchAngle;

        /// <summary>摄像机偏航角（度）。</summary>
        public float YawAngle => _yawAngle;

        /// <summary>
        /// Awake：调用基类完成单例注册，缓存 Camera 组件。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            _camera = GetComponent<UnityEngine.Camera>();
            if (_camera == null)
            {
                _camera = gameObject.AddComponent<UnityEngine.Camera>();
            }
        }

        /// <summary>
        /// Start：若未指定目标，尝试跟随足球。
        /// </summary>
        private void Start()
        {
            if (_target == null)
            {
                TryFollowBall();
            }

            // 初始位置直接对齐目标，避免开场镜头从原点飞入
            if (_target != null)
            {
                Vector3 initPos = ComputeCameraPosition(_target.position + _targetOffset);
                transform.position = initPos;
                transform.LookAt(_target.position + _targetOffset);
            }
        }

        /// <summary>
        /// Update：读取滚轮缩放输入，调整距离与俯角。
        /// 实际位置更新在 LateUpdate 中执行，确保在所有目标移动完成后跟随。
        /// </summary>
        private void Update()
        {
            ReadZoomInput();
        }

        /// <summary>
        /// LateUpdate：跟随目标并平滑插值位置与朝向。
        /// 使用 LateUpdate 保证在 PlayerController/Ball 更新后再跟随，避免抖动。
        /// </summary>
        private void LateUpdate()
        {
            // 目标为空时尝试跟随足球
            if (_target == null)
            {
                TryFollowBall();
            }

            if (_target == null)
            {
                return;
            }

            Vector3 targetPos = _target.position + _targetOffset;
            Vector3 desiredPos = ComputeCameraPosition(targetPos);

            // 边界限制
            desiredPos = ClampToBounds(desiredPos);

            // 平滑插值位置
            transform.position = Vector3.Lerp(transform.position, desiredPos, _positionLerpFactor);

            // 平滑朝向目标
            Quaternion desiredRot = Quaternion.LookRotation(targetPos - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, _rotationLerpFactor);
        }

        /// <summary>
        /// 读取滚轮缩放输入，调整距离与俯角。
        /// 滚轮上滚（正）→ 拉近距离 + 抬高俯角；下滚（负）→ 拉远 + 降低俯角。
        /// </summary>
        private void ReadZoomInput()
        {
            // 通过 InputReader 单例读取滚轮增量
            InputReader input = InputReader.Instance;
            if (input == null)
            {
                return;
            }

            float delta = input.CameraZoomDelta;
            if (Mathf.Abs(delta) < 1e-6f)
            {
                return;
            }

            // 滚轮上滚 delta>0 → 距离减小（拉近）；下滚 delta<0 → 距离增大（拉远）
            _distance -= delta * _zoomDistanceSensitivity;
            _distance = Mathf.Clamp(_distance, _minDistance, _maxDistance);

            // 俯角随距离联动：拉近时俯角变大（更俯视），拉远时变小（更平视）
            _pitchAngle += delta * _zoomPitchSensitivity;
            _pitchAngle = Mathf.Clamp(_pitchAngle, _minPitch, _maxPitch);
        }

        /// <summary>
        /// 根据目标位置、距离、俯角、偏航角计算摄像机期望位置。
        /// 采用球面坐标：摄像机位于目标后方（偏航角决定后方方向）与上方（俯角决定高度）。
        /// </summary>
        /// <param name="targetPos">目标世界坐标（含偏移）</param>
        /// <returns>摄像机期望世界坐标</returns>
        private Vector3 ComputeCameraPosition(Vector3 targetPos)
        {
            // 偏航角（度转弧度）：0 表示摄像机在目标 -z 方向（看向 +z）
            float yawRad = _yawAngle * Mathf.Deg2Rad;
            // 俯角（度转弧度）
            float pitchRad = _pitchAngle * Mathf.Deg2Rad;

            // 水平方向：从目标指向摄像机的水平单位向量
            // yaw=0 → 指向 -z；yaw=90 → 指向 -x；yaw=180 → 指向 +z
            float horizontalDist = _distance * Mathf.Cos(pitchRad);
            float verticalDist = _distance * Mathf.Sin(pitchRad);

            float offsetX = -Mathf.Sin(yawRad) * horizontalDist;
            float offsetZ = -Mathf.Cos(yawRad) * horizontalDist;

            Vector3 camPos = new Vector3(
                targetPos.x + offsetX,
                targetPos.y + verticalDist,
                targetPos.z + offsetZ);

            // 保证不低于最低高度
            if (camPos.y < _minHeight)
            {
                camPos.y = _minHeight;
            }

            return camPos;
        }

        /// <summary>
        /// 将摄像机位置限制在球场边界内。
        /// </summary>
        /// <param name="pos">期望位置</param>
        /// <returns>限制后的位置</returns>
        private Vector3 ClampToBounds(Vector3 pos)
        {
            pos.x = Mathf.Clamp(pos.x, -_boundsX, _boundsX);
            pos.z = Mathf.Clamp(pos.z, -_boundsZ, _boundsZ);
            return pos;
        }

        /// <summary>
        /// 尝试将跟随目标设为足球。
        /// 若 BallManager 与足球均存在，则将目标设为足球 Transform。
        /// </summary>
        private void TryFollowBall()
        {
            BallManager ballMgr = BallManager.Instance;
            if (ballMgr == null)
            {
                return;
            }

            BallEntity ball = ballMgr.GetBall();
            if (ball != null)
            {
                _target = ball.transform;
            }
        }

        /// <summary>
        /// 设置偏航角（绕 Y 轴旋转），用于切换摄像机观察方向。
        /// </summary>
        /// <param name="yaw">偏航角（度）</param>
        public void SetYawAngle(float yaw)
        {
            _yawAngle = yaw;
        }

        /// <summary>
        /// 直接设置距离与俯角（外部脚本调用，如战术视角切换）。
        /// </summary>
        /// <param name="distance">距离（米，会被 Clamp 到范围）</param>
        /// <param name="pitch">俯角（度，会被 Clamp 到范围）</param>
        public void SetDistanceAndPitch(float distance, float pitch)
        {
            _distance = Mathf.Clamp(distance, _minDistance, _maxDistance);
            _pitchAngle = Mathf.Clamp(pitch, _minPitch, _maxPitch);
        }
    }
}
