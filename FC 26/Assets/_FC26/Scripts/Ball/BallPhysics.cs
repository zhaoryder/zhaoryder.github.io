//=============================================================================
// 文件名：BallPhysics.cs
// 所属模块：Ball
// 命名空间：FC26.Ball
// 作用：足球物理静态工具类。提供碰撞判定、进球判定、出界判定、立柱反弹等
//       纯计算方法，不依赖 MonoBehaviour 生命周期，便于单元测试与复用。
// 备注：所有方法均为静态，输入 BallEntity 与坐标参数，返回判定结果结构体。
//       球场尺寸常量复用 FC26.Stadium.StadiumBuilder，保持与球场构建一致。
//=============================================================================
using UnityEngine;
using FC26.Stadium;

namespace FC26.Ball
{
    /// <summary>
    /// 足球物理静态工具类：处理碰撞判定、进球判定、出界判定、立柱反弹等。
    /// </summary>
    public static class BallPhysics
    {
        // 球门立柱半径（与 GoalAndProps.PostRadius 保持一致）
        private const float PostRadius = 0.06f;

        // 球门半宽（GoalWidth 7.32 的一半 = 3.66）
        private const float HalfGoalWidth = StadiumBuilder.GoalWidth * 0.5f;

        // 球门高度（2.44）
        private const float GoalHeight = StadiumBuilder.GoalHeight;

        // 球场半长/半宽
        private const float HalfLength = StadiumBuilder.HalfLength; // 52.5
        private const float HalfWidth = StadiumBuilder.HalfWidth;   // 34

        // 立柱/横梁反弹能量保留系数（反弹后保留的速度比例）
        private const float PostBounceRetention = 0.6f;

        /// <summary>
        /// 球员碰撞结果结构体。
        /// </summary>
        public struct CollisionResult
        {
            /// <summary>是否发生碰撞</summary>
            public bool HasCollision;

            /// <summary>碰撞法线（从球员指向球，已归一化）</summary>
            public Vector3 Normal;

            /// <summary>穿透深度（米）</summary>
            public float Penetration;

            /// <summary>碰撞点世界坐标</summary>
            public Vector3 ContactPoint;
        }

        /// <summary>
        /// 出界判定结果结构体。
        /// </summary>
        public struct OutOfBoundsResult
        {
            /// <summary>是否出界</summary>
            public bool IsOutOfBounds;

            /// <summary>出界类型：0=界外球, 1=角球, 2=球门球</summary>
            public int Type;

            /// <summary>出界位置</summary>
            public Vector3 Position;

            /// <summary>重发球队伍 ID（-1 表示需由调用方根据控球归属确定）</summary>
            public int RestartTeamId;
        }

        /// <summary>
        /// 检测足球与球员的碰撞。
        /// 将球员简化为圆柱体（半径 playerRadius，无限高），球简化为球体（半径 ball.Radius）。
        /// 当两者中心水平距离小于半径之和时判定为碰撞。
        /// </summary>
        /// <param name="ball">足球实体</param>
        /// <param name="playerPos">球员世界坐标</param>
        /// <param name="playerRadius">球员碰撞半径</param>
        /// <returns>碰撞结果结构体</returns>
        public static CollisionResult CheckCollisionWithPlayer(BallEntity ball, Vector3 playerPos, float playerRadius)
        {
            CollisionResult result = new CollisionResult();

            Vector3 ballPos = ball.transform.position;

            // 仅在水平面（x-z）上判定，忽略 y 差异（球员视为圆柱）
            Vector2 ballXZ = new Vector2(ballPos.x, ballPos.z);
            Vector2 playerXZ = new Vector2(playerPos.x, playerPos.z);

            float sumRadius = ball.Radius + playerRadius;
            Vector2 diff = ballXZ - playerXZ;
            float distSqr = diff.sqrMagnitude;

            if (distSqr >= sumRadius * sumRadius)
            {
                // 未碰撞
                result.HasCollision = false;
                return result;
            }

            float dist = Mathf.Sqrt(distSqr);
            result.HasCollision = true;
            result.Penetration = sumRadius - dist;

            if (dist > 0.0001f)
            {
                // 法线从球员指向球（水平面）
                Vector2 normalXZ = diff / dist;
                result.Normal = new Vector3(normalXZ.x, 0f, normalXZ.y);
            }
            else
            {
                // 完全重合，默认沿 +x 方向弹开
                result.Normal = Vector3.right;
            }

            // 碰撞点取球心沿法线回退半径处
            result.ContactPoint = ballPos - result.Normal * ball.Radius;

            return result;
        }

        /// <summary>
        /// 进球判定。
        /// 球场约定：主队（TeamId=0）进攻 z+ 方向球门（z=+52.5），客队（TeamId=1）进攻 z- 方向球门（z=-52.5）。
        /// 判定规则：球整体越过球门线（球心距球门线超过半径），且在球门宽度内（|x| ≤ 3.66），
        /// 且低于横梁（球心 y ≤ 2.44 + 半径容差）。
        /// </summary>
        /// <param name="ball">足球实体</param>
        /// <returns>0=无进球, 1=主队球门被进（客队得分）, 2=客队球门被进（主队得分）</returns>
        public static int CheckGoal(BallEntity ball)
        {
            Vector3 pos = ball.transform.position;
            float r = ball.Radius;

            // 球心必须在球门宽度范围内
            if (Mathf.Abs(pos.x) > HalfGoalWidth)
            {
                return 0;
            }

            // 球心高度必须低于横梁（含半径容差）
            if (pos.y > GoalHeight + r)
            {
                return 0;
            }

            // 球整体越过 z+ 球门线 → 客队球门（z+）被进 → 主队得分 → 返回 2
            if (pos.z - r > HalfLength)
            {
                return 2;
            }

            // 球整体越过 z- 球门线 → 主队球门（z-）被进 → 客队得分 → 返回 1
            if (pos.z + r < -HalfLength)
            {
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// 出界判定。
        /// 球整体越过边线（|x| - 半径 > 34）→ 界外球；
        /// 球整体越过底线（|z| - 半径 > 52.5）但未进球门（超出球门宽度或高于横梁）→ 球门球（默认）。
        /// 角球/球门球的最终归属需结合最后触球队伍，由 BallManager 根据控球归属修正；
        /// 此处 RestartTeamId 置 -1，Type 默认按位置判定。
        /// </summary>
        /// <param name="ball">足球实体</param>
        /// <returns>出界判定结果</returns>
        public static OutOfBoundsResult CheckOutOfBounds(BallEntity ball)
        {
            OutOfBoundsResult result = new OutOfBoundsResult
            {
                IsOutOfBounds = false,
                Type = 0,
                Position = ball.transform.position,
                RestartTeamId = -1
            };

            Vector3 pos = ball.transform.position;
            float r = ball.Radius;

            // 边线出界（左右两侧）→ 界外球
            if (Mathf.Abs(pos.x) - r > HalfWidth)
            {
                result.IsOutOfBounds = true;
                result.Type = 0; // 界外球
                result.Position = pos;
                return result;
            }

            // 底线出界（前后两端）
            if (Mathf.Abs(pos.z) - r > HalfLength)
            {
                // 若在球门范围内且低于横梁，则属于进球（由 CheckGoal 处理），此处不计出界
                bool inGoalWidth = Mathf.Abs(pos.x) <= HalfGoalWidth;
                bool belowBar = pos.y <= GoalHeight + r;
                if (inGoalWidth && belowBar)
                {
                    // 进球情况，不出界
                    return result;
                }

                result.IsOutOfBounds = true;
                // 默认判为球门球；若最后触球为防守方则由调用方改为角球（Type=1）
                result.Type = 2;
                result.Position = pos;
                return result;
            }

            return result;
        }

        /// <summary>
        /// 球门立柱/横梁碰撞反弹。
        /// 检测球与四根立柱（两端各两根）及两根横梁的碰撞，
        /// 若发生碰撞则反射球的速度并施加能量损失，同时修正穿透位置。
        /// 立柱位置：两端 z=±52.5，x=±3.66，从地面到 2.44 米高。
        /// 横梁位置：两端 z=±52.5，y=2.44，沿 x 方向跨 7.32 米。
        /// </summary>
        /// <param name="ball">足球实体</param>
        public static void BounceOffPost(BallEntity ball)
        {
            Vector3 pos = ball.transform.position;
            Vector3 vel = ball.Velocity;
            float r = ball.Radius;
            float collisionRadius = r + PostRadius;
            bool collided = false;

            // ---- 检测四根立柱（垂直圆柱）----
            // 仅当球心高度在立柱高度范围内（含半径容差）才检测
            if (pos.y <= GoalHeight + r && pos.y >= -r)
            {
                float[] postXs = { -HalfGoalWidth, HalfGoalWidth };
                float[] postZs = { -HalfLength, HalfLength };

                for (int zi = 0; zi < 2; zi++)
                {
                    for (int xi = 0; xi < 2; xi++)
                    {
                        float px = postXs[xi];
                        float pz = postZs[zi];

                        // 水平面距离
                        float dx = pos.x - px;
                        float dz = pos.z - pz;
                        float distSqr = dx * dx + dz * dz;

                        if (distSqr < collisionRadius * collisionRadius && distSqr > 1e-6f)
                        {
                            float dist = Mathf.Sqrt(distSqr);
                            // 法线从立柱指向球（水平面）
                            Vector3 normal = new Vector3(dx / dist, 0f, dz / dist);

                            // 将球推出穿透
                            pos += normal * (collisionRadius - dist);

                            // 反射速度（仅反射朝向立柱的速度分量）
                            float vDotN = Vector3.Dot(vel, normal);
                            if (vDotN < 0f)
                            {
                                vel = vel - (1f + PostBounceRetention) * vDotN * normal;
                            }

                            collided = true;
                        }
                    }
                }
            }

            // ---- 检测两根横梁（水平圆柱，沿 x 轴）----
            // 横梁中心 y=2.44, z=±52.5, x 范围 [-3.66, 3.66]
            float[] beamZs = { -HalfLength, HalfLength };
            for (int zi = 0; zi < 2; zi++)
            {
                float pz = beamZs[zi];

                // 球心 x 必须在横梁跨度范围内（含半径容差）
                if (Mathf.Abs(pos.x) > HalfGoalWidth + r)
                {
                    continue;
                }

                // 横梁沿 x 轴，故在 y-z 平面计算距离
                float dy = pos.y - GoalHeight;
                float dz = pos.z - pz;
                float distSqr = dy * dy + dz * dz;

                if (distSqr < collisionRadius * collisionRadius && distSqr > 1e-6f)
                {
                    float dist = Mathf.Sqrt(distSqr);
                    // 法线从横梁指向球（y-z 平面）
                    Vector3 normal = new Vector3(0f, dy / dist, dz / dist);

                    // 推出穿透
                    pos += normal * (collisionRadius - dist);

                    // 反射速度
                    float vDotN = Vector3.Dot(vel, normal);
                    if (vDotN < 0f)
                    {
                        vel = vel - (1f + PostBounceRetention) * vDotN * normal;
                    }

                    collided = true;
                }
            }

            // 若发生碰撞，写回位置与速度
            if (collided)
            {
                ball.transform.position = pos;
                ball.SetVelocity(vel);
            }
        }
    }
}
