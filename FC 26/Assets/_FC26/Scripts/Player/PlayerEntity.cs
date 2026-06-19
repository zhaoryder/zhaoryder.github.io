//=============================================================================
// 文件名：PlayerEntity.cs
// 所属模块：Player
// 命名空间：FC26.Player
// 作用：球员实体。挂载于球员 GameObject，承载球员身份、能力值、位置类型与
//       阵型归位坐标等运行时状态。供 AI、输入、动画等模块统一读取球员信息。
// 备注：本类为 MonoBehaviour，Position 属性直接取自 transform.position。
//       进攻方向约定：主队(TeamId=0)进攻 +z，客队(TeamId=1)进攻 -z。
//       球场尺寸常量复用 FC26.Stadium.StadiumBuilder（HalfLength=52.5, HalfWidth=34）。
//       PlayerId 约定：teamId * 100 + playerIndex（与 MatchManager 一致），
//       亦兼容 PlayerData.Number 作为球衣号码写入。
//=============================================================================
using UnityEngine;
using FC26.Data;
using FC26.Stadium;

namespace FC26.Player
{
    /// <summary>
    /// 球员实体：承载球员身份、能力与位置信息。
    /// 通过 PlayerId 唯一标识，TeamId 区分主队(0)/客队(1)。
    /// </summary>
    public class PlayerEntity : MonoBehaviour
    {
        // ===== 身份信息 =====
        [Header("身份信息")]
        [Tooltip("球员唯一 ID（约定 teamId*100+playerIndex，用于控球归属标识）")]
        public int PlayerId;

        [Tooltip("所属队伍 ID（0=主队，1=客队）")]
        public int TeamId;

        [Tooltip("场上位置类型 GK/DF/MF/FW")]
        public PlayerPosition PositionType;

        // ===== 能力值 =====
        [Header("能力值 0-99")]
        [Tooltip("五维能力值（速度/传球/射门/防守/体能）")]
        public Ability Stats;

        // ===== 归位坐标 =====
        [Header("阵型归位")]
        [Tooltip("阵型归位世界坐标（由布阵系统在开球时写入，AI 跑位以此为基础）")]
        public Vector3 HomePosition;

        // ===== 运行时状态 =====
        [Header("运行时状态")]
        [Tooltip("是否用户控制（true=InputReader 驱动，false=AI 驱动）")]
        [SerializeField] private bool _isUserControlled = false;

        [Tooltip("是否处于眩晕状态（被铲倒后短暂无法动作，由状态机自动清除）")]
        [SerializeField] private bool _isStunned = false;

        [Tooltip("当前体能值（0~99，随比赛进行消耗，影响移动速度）")]
        [SerializeField] private float _currentStamina = 99f;

        /// <summary>球员当前世界坐标（取自 transform，避免冗余字段）。</summary>
        public Vector3 Position => transform.position;

        /// <summary>是否为门将。</summary>
        public bool IsGoalkeeper => PositionType == PlayerPosition.GK;

        /// <summary>
        /// 是否用户控制（可读写）。
        /// true 时由 InputReader 驱动动作，false 时由 AI 模块驱动。
        /// </summary>
        public bool IsUserControlled
        {
            get => _isUserControlled;
            set => _isUserControlled = value;
        }

        /// <summary>
        /// 是否处于眩晕状态（可读写）。
        /// 由 PlayerController.Stun 触发为 true，由 PlayerStateMachine 在眩晕超时后清除。
        /// </summary>
        public bool IsStunned
        {
            get => _isStunned;
            set => _isStunned = value;
        }

        /// <summary>
        /// 速度能力值（0~99，浮点）。
        /// 供 PlayerController.ComputeMoveSpeed 计算移动速度使用。
        /// </summary>
        public float Speed => Stats.Speed;

        /// <summary>
        /// 当前体能值（0~99）。
        /// 初始为 Stats.Stamina，随比赛进行消耗，低于 30 时影响移动速度。
        /// 可由外部（如体能消耗系统）写入。
        /// </summary>
        public float CurrentStamina
        {
            get => _currentStamina;
            set => _currentStamina = Mathf.Clamp(value, 0f, 99f);
        }

        /// <summary>
        /// 球员朝向（世界空间水平方向，归一化）。
        /// 取自 transform.forward，供 PlayerController 在无方向输入时作为默认出球方向。
        /// </summary>
        public Vector3 Facing => transform.forward;

        /// <summary>
        /// 进攻方向（归一化向量）：主队沿 +z 进攻，客队沿 -z 进攻。
        /// </summary>
        public Vector3 AttackDirection => TeamId == 0 ? Vector3.forward : Vector3.back;

        /// <summary>本方球门中心世界坐标（球门线中点，y=0）。</summary>
        public Vector3 OwnGoalPosition => TeamId == 0
            ? new Vector3(0f, 0f, -StadiumBuilder.HalfLength)
            : new Vector3(0f, 0f, StadiumBuilder.HalfLength);

        /// <summary>对方球门中心世界坐标（球门线中点，y=0）。</summary>
        public Vector3 OppGoalPosition => TeamId == 0
            ? new Vector3(0f, 0f, StadiumBuilder.HalfLength)
            : new Vector3(0f, 0f, -StadiumBuilder.HalfLength);

        /// <summary>
        /// 初始化球员实体（由 PlayerFactory.CreatePlayerOnField 调用）。
        /// 从 PlayerData 读取身份与能力值，设置队伍与控制模式，并初始化体能。
        /// </summary>
        /// <param name="data">球员静态数据（含姓名、号码、位置、能力值）</param>
        /// <param name="teamId">队伍 ID（0=主队，1=客队）</param>
        /// <param name="isUserControlled">是否用户控制</param>
        public void Initialize(PlayerData data, int teamId, bool isUserControlled)
        {
            if (data == null)
            {
                Debug.LogWarning("[PlayerEntity] Initialize 失败：PlayerData 为空。");
                return;
            }

            // 球员 ID：使用球衣号码作为唯一标识（与 BallManager 控球归属匹配）
            PlayerId = data.Number;
            TeamId = teamId;
            PositionType = data.Position;
            Stats = data.Stats;
            IsUserControlled = isUserControlled;

            // 初始体能满值（按能力值）
            _currentStamina = Stats.Stamina;
        }
    }
}
