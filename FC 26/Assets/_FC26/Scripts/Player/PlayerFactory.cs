//=============================================================================
// 文件名：PlayerFactory.cs
// 所属模块：Player
// 命名空间：FC26.Player
// 作用：球员工厂单例。继承 MonoSingleton<PlayerFactory>，全局唯一。
//       负责运行时创建场上球员 GameObject（用 Capsule 占位），
//       挂载 PlayerEntity/PlayerController/PlayerStateMachine/PlayerAnimator 组件，
//       并按 Formations 阵型坐标生成整队球员。
// 备注：主队球门在 z=-52.5（进攻 +z），客队球门在 z=+52.5（进攻 -z）。
//       归一化坐标转世界坐标：worldX = normalizedX * 34, worldZ = normalizedY * 52.5。
//       客队坐标需镜像翻转 z（worldZ = -normalizedY * 52.5），使其进攻方向朝 -z。
//       所有 GameObject 运行时构建，不依赖预制体。
//=============================================================================
using UnityEngine;
using FC26.Core;
using FC26.Data;

namespace FC26.Player
{
    /// <summary>
    /// 球员工厂单例：运行时创建场上球员 GameObject 与组件。
    /// </summary>
    public class PlayerFactory : MonoSingleton<PlayerFactory>
    {
        // ===== 球场尺寸常量（与 StadiumBuilder 保持一致）=====

        // 球场半宽（x 轴方向，米）
        private const float HalfWidth = 34f;
        // 球场半长（z 轴方向，米）
        private const float HalfLength = 52.5f;

        // ===== 球员外观参数 =====

        [Header("球员外观参数")]
        [Tooltip("球员胶囊体半径（米）")]
        [SerializeField] private float _capsuleRadius = 0.35f;

        [Tooltip("球员胶囊体高度（米）")]
        [SerializeField] private float _capsuleHeight = 1.8f;

        [Tooltip("主队球衣颜色")]
        [SerializeField] private Color _homeColor = new Color(0.2f, 0.4f, 0.9f);

        [Tooltip("客队球衣颜色")]
        [SerializeField] private Color _awayColor = new Color(0.9f, 0.2f, 0.2f);

        [Tooltip("门将球衣颜色")]
        [SerializeField] private Color _gkColor = new Color(0.9f, 0.8f, 0.1f);

        [Tooltip("用户控制球员高亮颜色")]
        [SerializeField] private Color _userHighlightColor = new Color(0f, 1f, 0f);

        // ===== 容器 =====

        [Header("容器")]
        [Tooltip("主队球员父节点（为空则自动创建）")]
        [SerializeField] private Transform _homeTeamRoot;

        [Tooltip("客队球员父节点（为空则自动创建）")]
        [SerializeField] private Transform _awayTeamRoot;

        // ===== 内部缓存 =====

        // URP/Lit Shader 缓存（避免每次创建材质都查找）
        private Shader _urpLitShader;

        /// <summary>
        /// Awake：调用基类完成单例注册，缓存 Shader，确保容器存在。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            // 缓存 URP Lit Shader
            _urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (_urpLitShader == null)
            {
                // URP 不可用时回退到标准 Shader
                _urpLitShader = Shader.Find("Standard");
            }

            // 确保队伍容器存在
            EnsureTeamRoots();
        }

        /// <summary>
        /// 确保主队与客队球员父节点存在。
        /// </summary>
        private void EnsureTeamRoots()
        {
            if (_homeTeamRoot == null)
            {
                GameObject homeObj = new GameObject("HomeTeam_Players");
                homeObj.transform.SetParent(transform);
                _homeTeamRoot = homeObj.transform;
            }

            if (_awayTeamRoot == null)
            {
                GameObject awayObj = new GameObject("AwayTeam_Players");
                awayObj.transform.SetParent(transform);
                _awayTeamRoot = awayObj.transform;
            }
        }

        // ====================================================================
        #region 单个球员创建

        /// <summary>
        /// 在场上创建单个球员 GameObject。
        /// 使用 Capsule 图元占位，挂载 PlayerEntity/PlayerController/PlayerStateMachine/PlayerAnimator，
        /// 根据球员位置（GK/DF/MF/FW）与队伍分配球衣颜色。
        /// </summary>
        /// <param name="data">球员静态数据</param>
        /// <param name="position">世界坐标出生位置</param>
        /// <param name="teamId">队伍 ID（0=主队，1=客队）</param>
        /// <param name="isUserControlled">是否用户控制</param>
        /// <returns>创建的 PlayerEntity 引用</returns>
        public PlayerEntity CreatePlayerOnField(PlayerData data, Vector3 position, int teamId, bool isUserControlled)
        {
            if (data == null)
            {
                Debug.LogWarning("[PlayerFactory] PlayerData 为空，无法创建球员。");
                return null;
            }

            // ---- 创建 GameObject ----
            // 使用 Capsule 图元占位（无需预制体）
            GameObject playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObj.name = $"Player_{data.Number}_{data.Name}";

            // 设置父节点
            Transform parent = (teamId == 0) ? _homeTeamRoot : _awayTeamRoot;
            playerObj.transform.SetParent(parent);

            // 设置位置与缩放
            // Capsule 默认高度 2（半径 0.5，高度 2），缩放到目标高度
            float scaleY = _capsuleHeight / 2f;
            playerObj.transform.localScale = new Vector3(
                _capsuleRadius / 0.5f, // x 缩放（默认半径 0.5）
                scaleY,                // y 缩放
                _capsuleRadius / 0.5f  // z 缩放
            );
            // Capsule 中心在原点，需上移使脚部贴地（y = height/2）
            position.y = _capsuleHeight * 0.5f;
            playerObj.transform.position = position;

            // ---- 移除默认碰撞器（球员碰撞由逻辑处理，避免物理干扰）----
            Collider defaultCollider = playerObj.GetComponent<Collider>();
            if (defaultCollider != null)
            {
                Destroy(defaultCollider);
            }

            // ---- 设置球衣颜色 ----
            Renderer renderer = playerObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(_urpLitShader);
                mat.color = GetPlayerColor(data, teamId);
                renderer.material = mat;
            }

            // ---- 挂载组件 ----
            // 顺序：PlayerEntity -> PlayerStateMachine -> PlayerAnimator -> PlayerController
            // PlayerController 使用 RequireComponent 确保依赖存在。
            PlayerEntity entity = playerObj.AddComponent<PlayerEntity>();
            PlayerStateMachine stateMachine = playerObj.AddComponent<PlayerStateMachine>();
            PlayerAnimator animator = playerObj.AddComponent<PlayerAnimator>();
            PlayerController controller = playerObj.AddComponent<PlayerController>();

            // ---- 初始化组件 ----
            entity.Initialize(data, teamId, isUserControlled);
            stateMachine.Initialize(entity, animator);
            controller.SetReferences(entity, stateMachine, animator);

            // ---- 用户控制球员高亮（绿色标记）----
            if (isUserControlled)
            {
                MarkUserControlled(playerObj);
            }

            // ---- 朝向：主队朝 +z（进攻方向），客队朝 -z ----
            Vector3 facing = (teamId == 0) ? Vector3.forward : Vector3.back;
            playerObj.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);

            return entity;
        }

        /// <summary>
        /// 根据球员位置与队伍获取球衣颜色。
        /// 门将使用专用颜色，其余按主/客队颜色。
        /// </summary>
        private Color GetPlayerColor(PlayerData data, int teamId)
        {
            if (data.Position == PlayerPosition.GK)
            {
                return _gkColor;
            }
            return (teamId == 0) ? _homeColor : _awayColor;
        }

        /// <summary>
        /// 为用户控制球员添加高亮标记（在头顶添加一个小球体）。
        /// </summary>
        private void MarkUserControlled(GameObject playerObj)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "UserMarker";
            marker.transform.SetParent(playerObj.transform);
            marker.transform.localScale = Vector3.one * 0.25f;
            // 标记位于球员头顶上方
            marker.transform.localPosition = new Vector3(0f, 1.3f, 0f);

            // 移除标记的碰撞器
            Collider markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null)
            {
                Destroy(markerCollider);
            }

            // 绿色发光材质
            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(_urpLitShader);
                mat.color = _userHighlightColor;
                renderer.material = mat;
            }
        }

        #endregion

        // ====================================================================
        #region 整队创建

        /// <summary>
        /// 按 Formations 阵型坐标在场上生成整队球员。
        /// 主队（isHome=true）球门在 z=-52.5，进攻方向 +z，坐标直接映射：
        ///   worldX = normalizedX * 34, worldZ = normalizedY * 52.5。
        /// 客队（isHome=false）球门在 z=+52.5，进攻方向 -z，坐标镜像翻转 z：
        ///   worldX = normalizedX * 34, worldZ = -normalizedY * 52.5。
        /// </summary>
        /// <param name="teamData">球队数据（含阵型与首发 11 人）</param>
        /// <param name="isHome">是否主队（true=主队, false=客队）</param>
        /// <param name="isUserControlled">是否用户控制</param>
        /// <returns>创建的 PlayerEntity 数组（与 Starters 顺序一致）</returns>
        public PlayerEntity[] CreateTeamOnField(TeamData teamData, bool isHome, bool isUserControlled)
        {
            if (teamData == null)
            {
                Debug.LogWarning("[PlayerFactory] TeamData 为空，无法创建球队。");
                return null;
            }

            if (teamData.Starters == null || teamData.Starters.Length == 0)
            {
                Debug.LogWarning($"[PlayerFactory] 球队 {teamData.TeamName} 无首发球员数据。");
                return null;
            }

            // 获取阵型坐标
            Vector2[] formationPositions = Formations.GetPositions(teamData.Formation);
            if (formationPositions == null || formationPositions.Length == 0)
            {
                Debug.LogWarning($"[PlayerFactory] 阵型 {teamData.Formation} 无坐标数据。");
                return null;
            }

            int teamId = isHome ? 0 : 1;
            int playerCount = Mathf.Min(teamData.Starters.Length, formationPositions.Length);
            PlayerEntity[] createdPlayers = new PlayerEntity[playerCount];

            for (int i = 0; i < playerCount; i++)
            {
                PlayerData playerData = teamData.Starters[i];
                if (playerData == null)
                {
                    Debug.LogWarning($"[PlayerFactory] 球队 {teamData.TeamName} 首发 {i} 号球员数据为空，跳过。");
                    continue;
                }

                Vector2 normalizedPos = formationPositions[i];

                // 归一化坐标转世界坐标
                float worldX = normalizedPos.x * HalfWidth;
                float worldZ;

                if (isHome)
                {
                    // 主队：直接映射（normalizedY=-1 → z=-52.5 本方球门，normalizedY=+1 → z=+52.5 对方球门）
                    worldZ = normalizedPos.y * HalfLength;
                }
                else
                {
                    // 客队：镜像翻转 z（normalizedY=-1 → z=+52.5 本方球门，normalizedY=+1 → z=-52.5 对方球门）
                    worldZ = -normalizedPos.y * HalfLength;
                }

                Vector3 worldPos = new Vector3(worldX, 0f, worldZ);

                // 创建球员
                createdPlayers[i] = CreatePlayerOnField(playerData, worldPos, teamId, isUserControlled);
            }

            Debug.Log($"[PlayerFactory] 球队 {teamData.TeamName}（{(isHome ? "主队" : "客队")}）已创建 {playerCount} 名球员。");
            return createdPlayers;
        }

        #endregion

        // ====================================================================
        #region 清理

        /// <summary>
        /// 清理指定队伍的所有球员 GameObject。
        /// 用于比赛结束或重开时回收资源。
        /// </summary>
        /// <param name="isHome">是否主队</param>
        public void ClearTeam(bool isHome)
        {
            Transform root = isHome ? _homeTeamRoot : _awayTeamRoot;
            if (root == null)
            {
                return;
            }

            // 倒序销毁子物体（避免遍历时修改集合）
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 清理所有球员（主队与客队）。
        /// </summary>
        public void ClearAllTeams()
        {
            ClearTeam(true);
            ClearTeam(false);
        }

        #endregion
    }
}
