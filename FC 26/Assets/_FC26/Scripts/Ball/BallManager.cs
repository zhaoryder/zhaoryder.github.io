//=============================================================================
// 文件名：BallManager.cs
// 所属模块：Ball
// 命名空间：FC26.Ball
// 作用：足球管理器单例。继承 MonoSingleton<BallManager>，全局唯一。
//       负责足球实体的创建、控球归属管理、传球/射门/直塞指令执行、
//       定位球重置，以及通过 EventBus 广播控球变化、出界、进球事件。
// 备注：BallPossessionChangedEvent 与 GoalScoredEvent 已在 FC26.Core.EventBus 中定义，
//       本文件仅补充定义 BallOutOfPlayEvent（置于 FC26.Core 命名空间以保持事件一致性），
//       避免重复定义导致编译冲突。
//=============================================================================
using UnityEngine;
using FC26.Core;

namespace FC26.Ball
{
    /// <summary>
    /// 足球管理器单例：管理足球实体生命周期、控球归属、指令执行与事件广播。
    /// </summary>
    public class BallManager : MonoSingleton<BallManager>
    {
        // ===== 控球归属 =====
        [Header("控球归属")]
        [Tooltip("当前控球队伍 ID（-1=无人控球，0=主队，1=客队）")]
        [SerializeField] private int _possessionTeamId = -1;

        [Tooltip("当前控球球员 ID（-1=无人控球）")]
        [SerializeField] private int _possessionPlayerId = -1;

        // ===== 足球引用 =====
        [Header("足球引用")]
        [Tooltip("场景中的足球实体。若为空将在 CreateBall 时创建。")]
        [SerializeField] private BallEntity _ball;

        // ===== 指令参数 =====
        [Header("指令参数")]
        [Tooltip("传球基础速度系数（power 0~1 映射到该速度）")]
        [SerializeField] private float _passMaxSpeed = 18f;

        [Tooltip("射门基础速度系数")]
        [SerializeField] private float _shootMaxSpeed = 28f;

        [Tooltip("直塞基础速度系数（低平快速）")]
        [SerializeField] private float _throughBallMaxSpeed = 24f;

        [Tooltip("射门抬球角度（度）")]
        [SerializeField] private float _shootLiftAngle = 12f;

        /// <summary>当前控球队伍 ID（-1=无人控球，0=主队，1=客队）。</summary>
        public int PossessionTeamId => _possessionTeamId;

        /// <summary>当前控球球员 ID（-1=无人控球）。</summary>
        public int PossessionPlayerId => _possessionPlayerId;

        /// <summary>足球实体引用。</summary>
        public BallEntity Ball => _ball;

        /// <summary>
        /// Awake：注册单例（基类完成），确保足球存在。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            // 若未在 Inspector 中指定足球，则运行时创建。
            if (_ball == null)
            {
                CreateBall();
            }
        }

        /// <summary>
        /// FixedUpdate：检测进球与出界，广播相应事件。
        /// 物理积分由 BallEntity 自身 FixedUpdate 完成，此处仅做规则判定。
        /// </summary>
        private void FixedUpdate()
        {
            if (_ball == null)
            {
                return;
            }

            // 进球判定（优先于出界）
            int goalType = BallPhysics.CheckGoal(_ball);
            if (goalType != 0)
            {
                HandleGoal(goalType);
                return; // 进球后本帧不再判定出界
            }

            // 出界判定
            BallPhysics.OutOfBoundsResult oob = BallPhysics.CheckOutOfBounds(_ball);
            if (oob.IsOutOfBounds)
            {
                HandleOutOfBounds(oob);
            }
        }

        // ====================================================================
        #region 足球创建

        /// <summary>
        /// 运行时创建足球 GameObject 并挂载 BallEntity。
        /// 足球默认放置在球场中心点（地面），缩放为标准足球直径（半径 0.11 米）。
        /// </summary>
        /// <returns>创建的 BallEntity</returns>
        public BallEntity CreateBall()
        {
            if (_ball != null)
            {
                return _ball;
            }

            // 创建球体图元（Unity Sphere 默认半径 0.5，直径 1）
            GameObject ballObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballObj.name = "Ball";
            ballObj.transform.SetParent(transform);

            // 缩放为标准足球直径：世界半径 0.11 → 缩放 0.22（0.5 * 0.22 = 0.11）
            ballObj.transform.localScale = Vector3.one * 0.22f;
            ballObj.transform.position = new Vector3(0f, 0.11f, 0f);

            // 赋予白色材质（URP/Lit）
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = Color.white;
            ballObj.GetComponent<Renderer>().material = mat;

            // 挂载 BallEntity（Awake 中会根据缩放配置 SphereCollider 半径）
            _ball = ballObj.AddComponent<BallEntity>();

            // 初始控球归属清零
            _possessionTeamId = -1;
            _possessionPlayerId = -1;

            return _ball;
        }

        /// <summary>
        /// 获取当前足球实体。
        /// </summary>
        /// <returns>足球实体，若不存在返回 null</returns>
        public BallEntity GetBall()
        {
            return _ball;
        }

        #endregion

        // ====================================================================
        #region 控球归属

        /// <summary>
        /// 设置控球归属并广播 BallPossessionChangedEvent。
        /// </summary>
        /// <param name="teamId">队伍 ID（-1=失去控球）</param>
        /// <param name="playerId">球员 ID（-1=失去控球）</param>
        public void SetPossession(int teamId, int playerId)
        {
            if (_possessionTeamId == teamId && _possessionPlayerId == playerId)
            {
                return;
            }

            _possessionTeamId = teamId;
            _possessionPlayerId = playerId;

            EventBus.Publish(new BallPossessionChangedEvent
            {
                TeamId = teamId,
                PlayerId = playerId
            });
        }

        /// <summary>
        /// 清除控球归属（无人控球）。
        /// </summary>
        public void ClearPossession()
        {
            SetPossession(-1, -1);
        }

        #endregion

        // ====================================================================
        #region 指令执行

        /// <summary>
        /// 执行传球：沿指定方向施加水平冲量。
        /// 传球为低平球，不抬球。
        /// </summary>
        /// <param name="direction">传球方向（世界坐标，会归一化）</param>
        /// <param name="power">力量 0~1</param>
        public void ExecutePass(Vector3 direction, float power)
        {
            if (_ball == null)
            {
                return;
            }

            power = Mathf.Clamp01(power);
            Vector3 dir = Vector3.Normalize(new Vector3(direction.x, 0f, direction.z));
            if (dir.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector3 impulse = dir * (_passMaxSpeed * power);
            _ball.ApplyImpulse(impulse);

            // 传球后失去控球
            ClearPossession();
        }

        /// <summary>
        /// 执行射门：沿指定方向施加带抬球角度的冲量。
        /// 球会被抬起一定角度，速度高于传球。
        /// </summary>
        /// <param name="direction">射门方向（世界坐标）</param>
        /// <param name="power">力量 0~1</param>
        public void ExecuteShoot(Vector3 direction, float power)
        {
            if (_ball == null)
            {
                return;
            }

            power = Mathf.Clamp01(power);
            Vector3 dir = Vector3.Normalize(new Vector3(direction.x, 0f, direction.z));
            if (dir.sqrMagnitude < 0.0001f)
            {
                return;
            }

            // 抬球角度：将水平方向向上抬起一定角度
            float liftRad = _shootLiftAngle * Mathf.Deg2Rad;
            Vector3 shotDir = new Vector3(
                dir.x * Mathf.Cos(liftRad),
                Mathf.Sin(liftRad),
                dir.z * Mathf.Cos(liftRad)).normalized;

            Vector3 impulse = shotDir * (_shootMaxSpeed * power);
            _ball.ApplyImpulse(impulse);

            ClearPossession();
        }

        /// <summary>
        /// 执行直塞：低平快速传球，速度高于普通传球且不抬球。
        /// </summary>
        /// <param name="direction">直塞方向（世界坐标）</param>
        /// <param name="power">力量 0~1</param>
        public void ExecuteThroughBall(Vector3 direction, float power)
        {
            if (_ball == null)
            {
                return;
            }

            power = Mathf.Clamp01(power);
            Vector3 dir = Vector3.Normalize(new Vector3(direction.x, 0f, direction.z));
            if (dir.sqrMagnitude < 0.0001f)
            {
                return;
            }

            // 直塞：低平（贴地）、快速
            Vector3 impulse = dir * (_throughBallMaxSpeed * power);
            _ball.ApplyImpulse(impulse);

            ClearPossession();
        }

        #endregion

        // ====================================================================
        #region 重置

        /// <summary>
        /// 中圈开球重置：将球放置到球场中心点并清零速度与控球。
        /// </summary>
        public void ResetBallToCenter()
        {
            ResetBallToPosition(new Vector3(0f, 0.11f, 0f));
        }

        /// <summary>
        /// 定位球重置：将球放置到指定位置并清零速度。
        /// 控球归属不清零，由调用方按需设置。
        /// </summary>
        /// <param name="position">重置位置</param>
        public void ResetBallToPosition(Vector3 position)
        {
            if (_ball == null)
            {
                return;
            }

            _ball.ResetBall(position);
        }

        #endregion

        // ====================================================================
        #region 事件处理

        /// <summary>
        /// 处理进球：广播 GoalScoredEvent，并将球重置到中圈。
        /// </summary>
        /// <param name="goalType">进球类型（1=主队球门被进, 2=客队球门被进）</param>
        private void HandleGoal(int goalType)
        {
            // 进球方：主队球门被进(1)→客队得分(TeamId=1)；客队球门被进(2)→主队得分(TeamId=0)
            int scoringTeamId = (goalType == 1) ? 1 : 0;

            EventBus.Publish(new GoalScoredEvent
            {
                TeamId = scoringTeamId,
                ScorerPlayerId = _possessionPlayerId,
                MatchTime = Time.time
            });

            // 进球后重置到中圈并清空控球
            ResetBallToCenter();
            ClearPossession();
        }

        /// <summary>
        /// 处理出界：广播 BallOutOfPlayEvent，并冻结球的速度。
        /// 根据控球归属确定重发球队伍；角球/球门球的类型按控球方进攻方向修正。
        /// 约定：主队进攻 +z，客队进攻 -z。
        /// </summary>
        /// <param name="oob">出界判定结果</param>
        private void HandleOutOfBounds(BallPhysics.OutOfBoundsResult oob)
        {
            int type = oob.Type;
            int restartTeamId = -1;

            if (_possessionTeamId >= 0)
            {
                // 重发球队伍：球出界后由对方发球
                restartTeamId = (_possessionTeamId == 0) ? 1 : 0;

                // 底线出界时，根据控球方进攻方向区分角球与球门球
                if (oob.Type != 0)
                {
                    bool outAtPlusZ = oob.Position.z > 0f;
                    // 主队进攻 +z，客队进攻 -z
                    bool possessorAttackingThisEnd =
                        (_possessionTeamId == 0 && outAtPlusZ) ||
                        (_possessionTeamId == 1 && !outAtPlusZ);
                    // 进攻方把球踢出对方底线 → 球门球；防守方把球踢出己方底线 → 角球
                    type = possessorAttackingThisEnd ? 2 : 1;
                }
            }

            // 冻结球的速度，避免球继续飞出场外
            if (_ball != null)
            {
                _ball.SetVelocity(Vector3.zero);
            }

            EventBus.Publish(new BallOutOfPlayEvent
            {
                Type = type,
                Position = oob.Position,
                RestartTeamId = restartTeamId
            });
        }

        #endregion
    }
}

namespace FC26.Core
{
    /// <summary>
    /// 球出界事件。由 BallManager 在球出界时广播。
    /// 注：BallPossessionChangedEvent 与 GoalScoredEvent 已在 EventBus.cs 中定义，
    /// 此处仅补充定义 BallOutOfPlayEvent（置于 FC26.Core 命名空间以保持事件一致性），
    /// 避免重复定义导致编译冲突。
    /// </summary>
    public struct BallOutOfPlayEvent
    {
        /// <summary>出界类型：0=界外球, 1=角球, 2=球门球</summary>
        public int Type;

        /// <summary>出界位置</summary>
        public Vector3 Position;

        /// <summary>重发球队伍 ID</summary>
        public int RestartTeamId;
    }
}
