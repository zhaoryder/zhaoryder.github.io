using UnityEngine;

namespace FC26.Stadium
{
    /// <summary>
    /// 裁判 NPC 控制器：管理主裁判与两名边裁（助理裁判）的生成与位置更新。
    /// 主裁判跟随足球移动，保持在球后方 10-15 米的合理距离。
    /// 边裁沿两侧边线（x=±34）移动，跟随越位线（倒数第二防守球员的 z 位置）。
    /// 全部使用 Capsule 胶囊体占位，运行时构建，不依赖预制体。
    /// 球场尺寸：长 105 米（z 轴 -52.5~52.5），宽 68 米（x 轴 -34~34）。
    /// </summary>
    public class RefereeNPC : MonoBehaviour
    {
        // ===== 裁判配置 =====
        [Header("裁判配置")]
        [Tooltip("主裁判跟随球的距离（米，球后方）")]
        [SerializeField] private float mainFollowDistance = 12f;
        [Tooltip("主裁判身高（米）")]
        [SerializeField] private float mainRefereeHeight = 1.85f;
        [Tooltip("边裁身高（米）")]
        [SerializeField] private float linesmanHeight = 1.75f;
        [Tooltip("裁判胶囊体半径（米）")]
        [SerializeField] private float capsuleRadius = 0.22f;
        [Tooltip("位置平滑时间（秒），值越小跟随越快")]
        [SerializeField] private float smoothTime = 0.2f;
        [Tooltip("边裁沿边线的 x 坐标（绝对值，默认 34 对应边线位置）")]
        [SerializeField] private float linesmanX = 34f;

        // ===== 颜色配置 =====
        // 主裁判：黑色（经典裁判服色，与球员区分）
        private static readonly Color MainRefereeColor = new Color(0.06f, 0.06f, 0.07f);
        // 边裁：黄色（与主裁判区分）
        private static readonly Color LinesmanColor = new Color(0.95f, 0.85f, 0.15f);

        // ===== 运行时引用 =====
        private Transform _root;                // 裁判根节点
        private GameObject _mainReferee;        // 主裁判 GameObject
        private GameObject _leftLinesman;       // 左边裁 GameObject（x = -34）
        private GameObject _rightLinesman;      // 右边裁 GameObject（x = +34）

        // SmoothDamp 速度缓存（每帧由 SmoothDamp 自动更新）
        private Vector3 _mainRefVelocity;
        private Vector3 _leftLineVelocity;
        private Vector3 _rightLineVelocity;

        // 上一帧球位置（用于估算球移动方向，判定"球后方"）
        private Vector3 _lastBallPos;
        private bool _hasLastBallPos;

        // 当前"球后方"方向（持久化，避免球静止时方向丢失）
        private Vector3 _behindDir = new Vector3(0f, 0f, -1f);

        // 当前越位线 z 坐标（由外部设置，未设置时边裁跟随球 z）
        private float _offsideLineZ = 0f;
        private bool _hasOffsideLine;

        /// <summary>
        /// 构建三个裁判 GameObject（主裁判 + 左边裁 + 右边裁）。
        /// 若已构建过则先清理旧对象。
        /// </summary>
        public void Build()
        {
            // 清理旧数据
            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
            }

            // 创建裁判根节点
            GameObject rootObj = new GameObject("Referees");
            _root = rootObj.transform;

            // 创建主裁判（黑色），初始位置在中圈后方
            _mainReferee = CreateReferee("MainReferee", MainRefereeColor,
                mainRefereeHeight, _root);
            _mainReferee.transform.position = new Vector3(0f, mainRefereeHeight * 0.5f, -10f);

            // 创建左边裁（黄色，x = -34），初始位置在中线
            _leftLinesman = CreateReferee("LeftLinesman", LinesmanColor,
                linesmanHeight, _root);
            _leftLinesman.transform.position = new Vector3(-linesmanX, linesmanHeight * 0.5f, 0f);
            // 左边裁面向场内（+x 方向）
            _leftLinesman.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            // 创建右边裁（黄色，x = +34），初始位置在中线
            _rightLinesman = CreateReferee("RightLinesman", LinesmanColor,
                linesmanHeight, _root);
            _rightLinesman.transform.position = new Vector3(linesmanX, linesmanHeight * 0.5f, 0f);
            // 右边裁面向场内（-x 方向）
            _rightLinesman.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

            // 重置运行时状态
            _hasLastBallPos = false;
            _hasOffsideLine = false;
            _behindDir = new Vector3(0f, 0f, -1f);
            _mainRefVelocity = Vector3.zero;
            _leftLineVelocity = Vector3.zero;
            _rightLineVelocity = Vector3.zero;
        }

        /// <summary>
        /// 更新主裁判位置（跟随球，保持在球后方合理距离）。
        /// 同时更新边裁位置（沿边线跟随越位线或球 z 坐标）。
        /// 外部每帧调用此方法即可驱动全部裁判。
        /// </summary>
        /// <param name="ballPos">当前球的世界坐标</param>
        public void UpdatePosition(Vector3 ballPos)
        {
            if (_mainReferee == null || _leftLinesman == null || _rightLinesman == null)
            {
                return;
            }

            // 更新主裁判（跟随球）
            UpdateMainReferee(ballPos);

            // 边裁目标 z：优先使用越位线，未设置时跟随球 z
            float linesmanZ = _hasOffsideLine ? _offsideLineZ : ballPos.z;
            UpdateLinesmen(linesmanZ);

            // 记录球位置供下一帧估算方向使用
            _lastBallPos = ballPos;
            _hasLastBallPos = true;
        }

        /// <summary>
        /// 设置越位线 z 坐标（倒数第二防守球员的 z 位置）。
        /// 设置后边裁将沿边线移动到此 z 位置。
        /// </summary>
        /// <param name="z">越位线 z 坐标</param>
        public void SetOffsideLine(float z)
        {
            _offsideLineZ = z;
            _hasOffsideLine = true;
        }

        /// <summary>
        /// 清除越位线设置（边裁回退为跟随球 z 坐标）。
        /// </summary>
        public void ClearOffsideLine()
        {
            _hasOffsideLine = false;
        }

        /// <summary>
        /// 清理所有裁判对象。
        /// </summary>
        public void Clear()
        {
            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
                _root = null;
                _mainReferee = null;
                _leftLinesman = null;
                _rightLinesman = null;
            }
        }

        // ====================================================================
        #region 内部更新逻辑

        /// <summary>
        /// 更新主裁判位置：跟随球，保持在球后方 mainFollowDistance 米处。
        /// 通过球移动方向估算"后方"方向，球静止时保持上一帧方向。
        /// </summary>
        /// <param name="ballPos">当前球世界坐标</param>
        private void UpdateMainReferee(Vector3 ballPos)
        {
            // 根据球移动方向更新"后方"方向
            if (_hasLastBallPos)
            {
                Vector3 moveDir = ballPos - _lastBallPos;
                float moveDist = moveDir.magnitude;
                if (moveDist > 0.1f)
                {
                    // 球有明显移动，更新后方方向为移动反方向
                    _behindDir = -moveDir / moveDist;
                    _behindDir.y = 0f;
                    _behindDir.Normalize();
                }
            }

            // 目标位置：球后方 mainFollowDistance 米
            Vector3 targetPos = ballPos + _behindDir * mainFollowDistance;

            // 限制在球场范围内
            targetPos.x = Mathf.Clamp(targetPos.x,
                -StadiumBuilder.HalfWidth, StadiumBuilder.HalfWidth);
            targetPos.z = Mathf.Clamp(targetPos.z,
                -StadiumBuilder.HalfLength, StadiumBuilder.HalfLength);
            targetPos.y = mainRefereeHeight * 0.5f;

            // 平滑移动到目标位置
            _mainReferee.transform.position = Vector3.SmoothDamp(
                _mainReferee.transform.position, targetPos, ref _mainRefVelocity, smoothTime);

            // 主裁判面向球
            Vector3 faceDir = ballPos - _mainReferee.transform.position;
            faceDir.y = 0f;
            if (faceDir.sqrMagnitude > 0.01f)
            {
                _mainReferee.transform.rotation = Quaternion.LookRotation(faceDir);
            }
        }

        /// <summary>
        /// 更新两边裁位置：沿边线（x=±34）移动到指定 z 坐标。
        /// 边裁始终面向场内，不改变朝向。
        /// </summary>
        /// <param name="z">边裁目标 z 坐标（越位线或球 z）</param>
        private void UpdateLinesmen(float z)
        {
            // 限制 z 在球场范围内
            float clampedZ = Mathf.Clamp(z,
                -StadiumBuilder.HalfLength, StadiumBuilder.HalfLength);

            // 左边裁：x = -linesmanX，平滑移动到目标 z
            Vector3 leftTarget = new Vector3(-linesmanX, linesmanHeight * 0.5f, clampedZ);
            _leftLinesman.transform.position = Vector3.SmoothDamp(
                _leftLinesman.transform.position, leftTarget, ref _leftLineVelocity, smoothTime);

            // 右边裁：x = +linesmanX，平滑移动到目标 z
            Vector3 rightTarget = new Vector3(linesmanX, linesmanHeight * 0.5f, clampedZ);
            _rightLinesman.transform.position = Vector3.SmoothDamp(
                _rightLinesman.transform.position, rightTarget, ref _rightLineVelocity, smoothTime);
        }

        #endregion

        // ====================================================================
        #region 裁判创建

        /// <summary>
        /// 创建单个裁判 GameObject（Capsule 胶囊体占位）。
        /// Capsule 默认高 2 米、半径 0.5 米，沿 Y 轴竖直。
        /// </summary>
        /// <param name="name">物体名称</param>
        /// <param name="color">服装颜色</param>
        /// <param name="height">身高（米）</param>
        /// <param name="parent">父节点</param>
        /// <returns>裁判 GameObject</returns>
        private GameObject CreateReferee(string name, Color color, float height, Transform parent)
        {
            GameObject referee = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            referee.name = name;
            referee.transform.SetParent(parent);

            // Capsule 默认：高 2 米（沿 Y 轴），半径 0.5 米
            // 缩放至目标身高和半径
            float radiusScale = capsuleRadius / 0.5f;
            referee.transform.localScale = new Vector3(radiusScale, height * 0.5f, radiusScale);

            // 创建并赋予服装材质（URP/Lit）
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.name = name + "_Mat";
            mat.color = color;
            referee.GetComponent<Renderer>().material = mat;

            return referee;
        }

        #endregion

        /// <summary>
        /// 对象销毁时清理资源。
        /// </summary>
        private void OnDestroy()
        {
            Clear();
        }
    }
}
