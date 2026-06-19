//=============================================================================
// 文件名：AIDecisionCore.cs
// 所属模块：AI
// 命名空间：FC26.AI
// 作用：AI 决策核心。继承 MonoSingleton<AIDecisionCore>，全局唯一。
//       提供 MakeDecision(PlayerEntity, BallEntity, MatchContext) 入口，根据球员能力值、
//       场上局势（球权、距离球门、对手位置）与战术风格计算最优动作。
//   - 持球决策：综合射门/传球/直塞/带球四类动作的评分，选最优（受难度精度影响）。
//   - 无球跑位：根据战术风格（控球/反击/高位压迫/防守反击）与球员位置类型选择跑位点。
//   - 门将决策：沿球门线跟随球，近距离解围。
// 决策原则：传球高的优先传球，射门高的优先射门；不能无脑（有评分门槛），也不能太强（受难度限制）。
// 备注：所有评分均为 0~1 相对值，仅用于横向比较，无绝对物理含义。
//       球场尺寸复用 FC26.Stadium.StadiumBuilder（HalfLength=52.5, HalfWidth=34）。
//=============================================================================
using UnityEngine;
using FC26.Core;
using FC26.Data;
using FC26.Ball;
using FC26.Player;
using FC26.Stadium;

namespace FC26.AI
{
    /// <summary>
    /// AI 动作类型枚举。
    /// </summary>
    public enum AIActionType
    {
        /// <summary>传球</summary>
        Pass,
        /// <summary>带球</summary>
        Dribble,
        /// <summary>射门</summary>
        Shoot,
        /// <summary>直塞</summary>
        ThroughBall,
        /// <summary>移动（无球跑位）</summary>
        Move
    }

    /// <summary>
    /// AI 决策结果结构体。
    /// 对于持球动作（Pass/Shoot/ThroughBall）：TargetPosition 为目标点，Direction 为出球方向。
    /// 对于 Dribble/Move：TargetPosition 为跑位目标，Direction 为移动方向。
    /// </summary>
    public struct AIDecision
    {
        /// <summary>动作类型</summary>
        public AIActionType ActionType;

        /// <summary>目标位置（世界坐标）</summary>
        public Vector3 TargetPosition;

        /// <summary>力量 0~1（仅持球动作有效）</summary>
        public float Power;

        /// <summary>方向（归一化）：出球方向或移动方向</summary>
        public Vector3 Direction;

        /// <summary>构造便捷方法。</summary>
        public static AIDecision Create(AIActionType type, Vector3 target, float power, Vector3 direction)
        {
            return new AIDecision
            {
                ActionType = type,
                TargetPosition = target,
                Power = Mathf.Clamp01(power),
                Direction = direction
            };
        }
    }

    /// <summary>
    /// 比赛上下文。每帧由 AIController 更新，包含双方球员、球权、比分、战术与难度。
    /// 供决策核心读取，避免决策核心直接访问各管理器造成耦合。
    /// </summary>
    public class MatchContext
    {
        /// <summary>主队球员数组（TeamId=0）</summary>
        public PlayerEntity[] HomePlayers;

        /// <summary>客队球员数组（TeamId=1）</summary>
        public PlayerEntity[] AwayPlayers;

        /// <summary>足球当前世界坐标</summary>
        public Vector3 BallPosition;

        /// <summary>当前控球队伍 ID（-1=无人控球）</summary>
        public int PossessionTeamId = -1;

        /// <summary>当前控球球员 ID（-1=无人控球）</summary>
        public int PossessionPlayerId = -1;

        /// <summary>比赛时间（秒）</summary>
        public float MatchTime;

        /// <summary>主队比分</summary>
        public int HomeScore;

        /// <summary>客队比分</summary>
        public int AwayScore;

        /// <summary>AI 球队战术参数</summary>
        public TacticParams Tactic;

        /// <summary>AI 球队难度参数</summary>
        public AIDifficultyParams Difficulty;

        /// <summary>当前请求决策的球员状态（由状态机写入，供决策核心分支）</summary>
        public AIState RequestingPlayerState;
    }

    /// <summary>
    /// AI 决策核心单例。提供基于能力值与局势的动作决策。
    /// </summary>
    public class AIDecisionCore : MonoSingleton<AIDecisionCore>
    {
        // ===== 决策常量（可调） =====
        // 射门有效范围（距对方球门多少米内考虑射门）
        private const float ShootRange = 24f;
        // 直塞最大距离（超过则不直塞）
        private const float ThroughBallMaxDist = 35f;
        // 传球最大距离（超过则倾向长传但风险高）
        private const float PassMaxDist = 45f;
        // “附近对手”判定半径
        private const float NearbyRadius = 3.5f;
        // 带球前方空间判定半径
        private const float DribbleCheckRadius = 4f;

        /// <summary>
        /// 主决策入口。根据球员是否持球分流到持球/无球/门将决策。
        /// </summary>
        /// <param name="player">请求决策的球员</param>
        /// <param name="ball">足球实体</param>
        /// <param name="context">比赛上下文</param>
        /// <returns>决策结果</returns>
        public AIDecision MakeDecision(PlayerEntity player, BallEntity ball, MatchContext context)
        {
            if (player == null || context == null)
            {
                return AIDecision.Create(AIActionType.Move, player != null ? player.Position : Vector3.zero, 0f, Vector3.zero);
            }

            // 门将单独决策
            if (player.IsGoalkeeper)
            {
                return DecideGoalkeeper(player, ball, context);
            }

            // 判断是否为本队控球（球权归属本队即视为本队持球，具体持球者由 PossessionPlayerId 区分）
            bool hasBall = context.PossessionPlayerId == player.PlayerId
                           && context.PossessionTeamId == player.TeamId;

            if (hasBall)
            {
                return DecideWithBall(player, ball, context);
            }

            return DecideOffBall(player, ball, context);
        }

        // ====================================================================
        #region 持球决策

        /// <summary>
        /// 持球决策：综合射门/传球/直塞/带球评分，选最优动作。
        /// 评分受能力值、距离球门、对手压迫、战术参数影响，并受难度精度调制。
        /// </summary>
        private AIDecision DecideWithBall(PlayerEntity player, BallEntity ball, MatchContext context)
        {
            TacticParams tactic = context.Tactic;
            AIDifficultyParams diff = context.Difficulty;

            Vector3 playerPos = player.Position;
            Vector3 oppGoal = player.OppGoalPosition;
            Vector3 toGoal = oppGoal - playerPos;
            toGoal.y = 0f;
            float distToGoal = toGoal.magnitude;

            PlayerEntity[] teammates = GetTeammates(player, context);
            PlayerEntity[] opponents = GetOpponents(player, context);

            // ---- 1. 射门评分 ----
            float shootScore = 0f;
            Vector3 shootTarget = oppGoal;
            if (distToGoal <= ShootRange)
            {
                // 基础：射门能力
                shootScore = (player.Stats.Shooting / 99f) * 0.40f;
                // 距离越近越好
                shootScore += (1f - distToGoal / ShootRange) * 0.25f;
                // 角度：与进攻方向夹角越小越好（朝向球门）
                Vector3 toGoalDir = toGoal.normalized;
                float angleFactor = (Vector3.Dot(toGoalDir, player.AttackDirection) + 1f) * 0.5f;
                shootScore += angleFactor * 0.20f;
                // 对手压迫：附近对手越多越难射门
                int pressure = CountOpponentsNear(playerPos, NearbyRadius, opponents, player);
                shootScore -= pressure * 0.06f;
                shootScore = Mathf.Max(0f, shootScore);

                // 射门目标：球门中心略偏一侧（避开门将，简单启发）
                float sideOffset = (Random.value - 0.5f) * StadiumBuilder.GoalWidth * 0.6f;
                shootTarget = new Vector3(sideOffset, 0f, oppGoal.z);
            }

            // ---- 2. 传球评分 ----
            float passScore = 0f;
            PlayerEntity bestPassMate = FindBestPassTarget(player, teammates, opponents, tactic);
            if (bestPassMate != null)
            {
                // 基础：传球能力
                passScore = (player.Stats.Passing / 99f) * 0.30f;
                // 持球者受压迫程度：越受压迫越倾向传球
                int pressureOnBall = CountOpponentsNear(playerPos, NearbyRadius, opponents, player);
                passScore += pressureOnBall * 0.08f;
                // 队友前压程度：队友比持球者更靠近对方球门则加分
                float mateDistToGoal = HorizontalDist(bestPassMate.Position, oppGoal);
                float advancement = distToGoal - mateDistToGoal; // 正值=队友更靠前
                passScore += Mathf.Clamp01(advancement / 20f) * 0.20f;
                // 队友空当：附近无对手则加分
                int matePressure = CountOpponentsNear(bestPassMate.Position, NearbyRadius, opponents, bestPassMate);
                passScore += (1f - Mathf.Clamp01(matePressure / 3f)) * 0.15f;
                passScore = Mathf.Clamp01(passScore);
            }

            // ---- 3. 直塞评分 ----
            float throughScore = 0f;
            PlayerEntity bestThroughMate = FindBestThroughTarget(player, teammates, opponents);
            if (bestThroughMate != null)
            {
                // 基础：传球能力 × 战术冒险度
                throughScore = (player.Stats.Passing / 99f) * 0.20f * (0.5f + tactic.PassRisk);
                // 直塞目标在防线身后且有空当
                float throughDist = HorizontalDist(playerPos, bestThroughMate.Position);
                throughScore += Mathf.Clamp01(throughDist / ThroughBallMaxDist) * 0.15f;
                int matePressure = CountOpponentsNear(bestThroughMate.Position, NearbyRadius, opponents, bestThroughMate);
                throughScore += (1f - Mathf.Clamp01(matePressure / 3f)) * 0.15f;
                throughScore = Mathf.Clamp01(throughScore);
            }

            // ---- 4. 带球评分 ----
            float dribbleScore = 0f;
            {
                dribbleScore = (player.Stats.Speed / 99f) * 0.25f;
                // 前方空间：前方无对手则倾向带球
                Vector3 aheadPos = playerPos + player.AttackDirection * DribbleCheckRadius;
                int aheadPressure = CountOpponentsNear(aheadPos, DribbleCheckRadius, opponents, player);
                dribbleScore += (1f - Mathf.Clamp01(aheadPressure / 2f)) * 0.20f;
                // 距球门越近越倾向带球突破
                dribbleScore += (1f - Mathf.Clamp01(distToGoal / StadiumBuilder.HalfLength)) * 0.10f;
                dribbleScore = Mathf.Clamp01(dribbleScore);
            }

            // ---- 5. 选择最优动作（受难度精度影响） ----
            // 精度高：选最高分；精度低：有概率随机选次优，模拟失误
            AIActionType chosenType;
            if (Random.value <= diff.DecisionAccuracy)
            {
                chosenType = PickBest(shootScore, passScore, throughScore, dribbleScore);
            }
            else
            {
                // 失误：在四个动作中随机选一个（射门仅在射程内可选）
                chosenType = PickRandomValid(distToGoal <= ShootRange);
            }

            // ---- 6. 生成决策 ----
            float skill = diff.SkillMultiplier;
            switch (chosenType)
            {
                case AIActionType.Shoot:
                {
                    Vector3 dir = (shootTarget - playerPos);
                    dir.y = 0f;
                    float power = Mathf.Clamp(0.55f + (player.Stats.Shooting / 99f) * 0.45f, 0.5f, 1f) * skill;
                    return AIDecision.Create(AIActionType.Shoot, shootTarget, power, dir.normalized);
                }
                case AIActionType.Pass:
                {
                    if (bestPassMate != null)
                    {
                        Vector3 dir = bestPassMate.Position - playerPos;
                        dir.y = 0f;
                        float d = dir.magnitude;
                        // 力量按距离映射，近距轻传、远距大力
                        float power = Mathf.Clamp(0.35f + d / PassMaxDist * 0.6f, 0.3f, 1f) * skill;
                        return AIDecision.Create(AIActionType.Pass, bestPassMate.Position, power, dir.normalized);
                    }
                    // 无合适传球目标则回退带球
                    return BuildDribbleDecision(player, oppGoal, skill);
                }
                case AIActionType.ThroughBall:
                {
                    if (bestThroughMate != null)
                    {
                        // 直塞目标：队友前方一点（跑动接应点）
                        Vector3 lead = bestThroughMate.Position + player.AttackDirection * 3f;
                        Vector3 dir = lead - playerPos;
                        dir.y = 0f;
                        float power = Mathf.Clamp(0.6f + dir.magnitude / ThroughBallMaxDist * 0.4f, 0.5f, 1f) * skill;
                        return AIDecision.Create(AIActionType.ThroughBall, lead, power, dir.normalized);
                    }
                    return BuildDribbleDecision(player, oppGoal, skill);
                }
                default:
                    return BuildDribbleDecision(player, oppGoal, skill);
            }
        }

        /// <summary>构造带球决策：朝对方球门方向带球。</summary>
        private AIDecision BuildDribbleDecision(PlayerEntity player, Vector3 oppGoal, float skill)
        {
            Vector3 dir = oppGoal - player.Position;
            dir.y = 0f;
            // 带球目标：前方 5 米处
            Vector3 target = player.Position + dir.normalized * 5f;
            return AIDecision.Create(AIActionType.Dribble, target, 0f, dir.normalized);
        }

        /// <summary>从四项评分中选最高分对应的动作类型。</summary>
        private AIActionType PickBest(float shoot, float pass, float through, float dribble)
        {
            float max = shoot;
            AIActionType best = AIActionType.Shoot;
            if (pass > max) { max = pass; best = AIActionType.Pass; }
            if (through > max) { max = through; best = AIActionType.ThroughBall; }
            if (dribble > max) { max = dribble; best = AIActionType.Dribble; }
            // 评分全为 0 时默认带球（避免站立不动）
            if (max <= 0.01f)
            {
                return AIActionType.Dribble;
            }
            return best;
        }

        /// <summary>失误时随机选一个合法动作（射门仅在射程内可选）。</summary>
        private AIActionType PickRandomValid(bool canShoot)
        {
            // 权重：带球 40%、传球 35%、直塞 15%、射门 10%（仅射程内）
            float r = Random.value;
            if (canShoot && r < 0.10f) return AIActionType.Shoot;
            if (r < 0.25f) return AIActionType.ThroughBall;
            if (r < 0.60f) return AIActionType.Pass;
            return AIActionType.Dribble;
        }

        #endregion

        // ====================================================================
        #region 无球跑位决策

        /// <summary>
        /// 无球跑位决策：根据球员状态（Attack/Support/Defend/Mark）与战术选择跑位目标。
        /// 状态由 AIStateMachine 写入 context.RequestingPlayerState。
        /// </summary>
        private AIDecision DecideOffBall(PlayerEntity player, BallEntity ball, MatchContext context)
        {
            TacticParams tactic = context.Tactic;
            Vector3 ballPos = context.BallPosition;
            AIState state = context.RequestingPlayerState;

            Vector3 target;
            switch (state)
            {
                case AIState.Attack:
                    // 进攻跑位：前压到对方半场空当，或追抢 loose ball
                    if (context.PossessionTeamId < 0)
                    {
                        // 无人控球：最近的去抢球（目标=球的位置）
                        target = ballPos;
                    }
                    else
                    {
                        target = ComputeAttackRun(player, ballPos, tactic);
                    }
                    break;

                case AIState.Support:
                    // 支援跑位：在持球者附近提供接应点，拉开宽度
                    target = ComputeSupportPosition(player, ballPos, tactic);
                    break;

                case AIState.Mark:
                    // 盯人/压迫：逼近球权持有者
                    target = ComputePressPosition(player, context, tactic);
                    break;

                case AIState.Defend:
                    // 防守跑位：回到防线位置，根据防线高度前压或回收
                    target = ComputeDefensivePosition(player, ballPos, tactic);
                    break;

                default:
                    target = player.HomePosition;
                    break;
            }

            // 限制目标在球场范围内
            target = ClampToPitch(target);
            Vector3 dir = target - player.Position;
            dir.y = 0f;
            return AIDecision.Create(AIActionType.Move, target, 0f, dir.normalized);
        }

        /// <summary>进攻跑位：沿进攻方向前压，结合宽度拉开，寻找对方防线身前空当。</summary>
        private Vector3 ComputeAttackRun(PlayerEntity player, Vector3 ballPos, TacticParams tactic)
        {
            Vector3 home = player.HomePosition;
            Vector3 attackDir = player.AttackDirection;

            // 前压距离：进攻倾向越高压得越上
            float pushUp = Mathf.Lerp(8f, 20f, tactic.AttackTendency);
            Vector3 target = home + attackDir * pushUp;

            // 宽度利用：边路球员拉开，中路球员内收
            float widthSign = Mathf.Sign(home.x);
            if (Mathf.Abs(home.x) > 15f)
            {
                target.x += widthSign * Mathf.Lerp(2f, 6f, tactic.WidthUsage);
            }

            // 跟随球的横向位置（边锋套边/内切简化为跟随球）
            target.x = Mathf.Lerp(target.x, ballPos.x * 0.5f, 0.3f);
            return target;
        }

        /// <summary>支援跑位：在持球者侧后方提供接应点，保持适当距离与宽度。</summary>
        private Vector3 ComputeSupportPosition(PlayerEntity player, Vector3 ballPos, TacticParams tactic)
        {
            Vector3 home = player.HomePosition;
            Vector3 attackDir = player.AttackDirection;

            // 接应点：持球者后方偏侧 6-10 米
            float backOffset = Mathf.Lerp(10f, 6f, tactic.Tempo);
            Vector3 support = ballPos - attackDir * backOffset;

            // 拉开宽度
            float widthSign = Mathf.Sign(home.x);
            if (Mathf.Abs(widthSign) < 0.01f) widthSign = 1f;
            support.x += widthSign * Mathf.Lerp(3f, 8f, tactic.WidthUsage);

            // 向归位位置靠拢（避免跑离职责区域过远）
            support = Vector3.Lerp(support, home, 0.35f);
            return support;
        }

        /// <summary>防守跑位：根据防线高度回到防守位置，并朝球的方向收缩。</summary>
        private Vector3 ComputeDefensivePosition(PlayerEntity player, Vector3 ballPos, TacticParams tactic)
        {
            Vector3 home = player.HomePosition;
            Vector3 ownGoal = player.OwnGoalPosition;
            Vector3 attackDir = player.AttackDirection;

            // 防线高度：越高（接近 1）防线越靠上（向中场推进），越低越回收（贴近本方球门）
            // 防线 z = 本方球门 z + 进攻方向 × (防线高度 × 半场长度 × 0.7)
            float halfLen = StadiumBuilder.HalfLength;
            float defLineZ = ownGoal.z + attackDir.z * (tactic.DefensiveLineHeight * halfLen * 0.7f);

            // 目标 z：在归位 z 与防线 z 之间取值（保持阵型）
            float targetZ = Mathf.Lerp(home.z, defLineZ, 0.6f);

            // 横向：朝球的方向适度收缩（防止被拉开）
            float targetX = Mathf.Lerp(home.x, ballPos.x * 0.5f, 0.4f);

            return new Vector3(targetX, home.y, targetZ);
        }

        /// <summary>盯人/压迫跑位：逼近对方持球者，压迫强度越高贴得越近。</summary>
        private Vector3 ComputePressPosition(PlayerEntity player, MatchContext context, TacticParams tactic)
        {
            // 找到对方持球者位置
            Vector3 carrierPos = context.BallPosition; // 默认逼近球
            PlayerEntity carrier = FindPlayerById(context, context.PossessionPlayerId);
            if (carrier != null)
            {
                carrierPos = carrier.Position;
            }

            // 压迫距离：强度越高贴得越近
            float pressDist = Mathf.Lerp(2.5f, 1.0f, tactic.PressIntensity);
            Vector3 toCarrier = carrierPos - player.Position;
            toCarrier.y = 0f;
            if (toCarrier.sqrMagnitude < 0.01f)
            {
                return carrierPos;
            }
            // 站在持球者前方 pressDist 米处（拦截传球路线）
            Vector3 target = carrierPos - toCarrier.normalized * pressDist;
            return target;
        }

        #endregion

        // ====================================================================
        #region 门将决策

        /// <summary>
        /// 门将决策：沿球门线横向跟随球，球近距离时出击解围。
        /// </summary>
        private AIDecision DecideGoalkeeper(PlayerEntity player, BallEntity ball, MatchContext context)
        {
            Vector3 ballPos = context.BallPosition;
            Vector3 ownGoal = player.OwnGoalPosition;

            // 门将基础位置：球门线前方 2 米（朝场地内部）
            // AttackDirection 指向对方球门（即场地内部），故 ownGoal + AttackDirection * 2
            // 将门将放在本方球门前 2 米处，而非球门后方。
            // 主队：ownGoal=(0,0,-52.5) + (0,0,+2) = (0,0,-50.5) ✓
            // 客队：ownGoal=(0,0,+52.5) + (0,0,-2) = (0,0,+50.5) ✓
            Vector3 basePos = ownGoal + player.AttackDirection * 2f;

            // 横向跟随球的 x（限制在球门宽度内）
            float goalHalf = StadiumBuilder.GoalWidth * 0.5f;
            float targetX = Mathf.Clamp(ballPos.x, -goalHalf, goalHalf);

            Vector3 target = new Vector3(targetX, 0f, basePos.z);

            // 球距本方球门很近且无人控球或对方控球时，出击
            float distBallToGoal = HorizontalDist(ballPos, ownGoal);
            if (distBallToGoal < 8f && context.PossessionTeamId != player.TeamId)
            {
                // 出击扑球
                target = Vector3.Lerp(target, ballPos, 0.6f);
            }

            target = ClampToPitch(target);
            Vector3 dir = target - player.Position;
            dir.y = 0f;

            // 若球在脚下且本方控球，则大脚解围（传球到前场）
            if (context.PossessionPlayerId == player.PlayerId)
            {
                Vector3 clearTarget = player.OppGoalPosition;
                Vector3 clearDir = clearTarget - player.Position;
                clearDir.y = 0f;
                return AIDecision.Create(AIActionType.Pass, clearTarget, 1f, clearDir.normalized);
            }

            return AIDecision.Create(AIActionType.Move, target, 0f, dir.normalized);
        }

        #endregion

        // ====================================================================
        #region 辅助查询

        /// <summary>获取同队球员数组（按 TeamId 匹配，返回数组引用，无分配）。</summary>
        private PlayerEntity[] GetTeammates(PlayerEntity player, MatchContext context)
        {
            return player.TeamId == 0 ? context.HomePlayers : context.AwayPlayers;
        }

        /// <summary>获取对方球员数组。</summary>
        private PlayerEntity[] GetOpponents(PlayerEntity player, MatchContext context)
        {
            return player.TeamId == 0 ? context.AwayPlayers : context.HomePlayers;
        }

        /// <summary>统计指定位置附近（水平面）的对方球员数量，排除指定球员。</summary>
        private int CountOpponentsNear(Vector3 pos, float radius, PlayerEntity[] opponents, PlayerEntity exclude)
        {
            if (opponents == null) return 0;
            int count = 0;
            float rSqr = radius * radius;
            for (int i = 0; i < opponents.Length; i++)
            {
                PlayerEntity p = opponents[i];
                if (p == null || p == exclude) continue;
                Vector3 d = p.Position - pos;
                d.y = 0f;
                if (d.sqrMagnitude < rSqr) count++;
            }
            return count;
        }

        /// <summary>
        /// 寻找最佳传球目标：前压且空当最大的同队队友。
        /// 评分 = 前压程度 × 空当程度，返回评分最高者。
        /// </summary>
        private PlayerEntity FindBestPassTarget(PlayerEntity player, PlayerEntity[] teammates,
            PlayerEntity[] opponents, TacticParams tactic)
        {
            if (teammates == null) return null;

            PlayerEntity best = null;
            float bestScore = -1f;
            Vector3 oppGoal = player.OppGoalPosition;
            float myDistToGoal = HorizontalDist(player.Position, oppGoal);

            for (int i = 0; i < teammates.Length; i++)
            {
                PlayerEntity mate = teammates[i];
                if (mate == null || mate == player || mate.IsGoalkeeper) continue;

                Vector3 matePos = mate.Position;
                float dist = HorizontalDist(player.Position, matePos);
                // 距离过近或过远不传
                if (dist < 3f || dist > PassMaxDist) continue;

                // 前压程度：队友比持球者更靠近对方球门则加分
                float mateDistToGoal = HorizontalDist(matePos, oppGoal);
                float advancement = myDistToGoal - mateDistToGoal;
                if (advancement < -5f) continue; // 队友明显靠后，不优先

                float advanceScore = Mathf.Clamp01((advancement + 5f) / 25f);

                // 空当程度：附近对手越少越好
                int pressure = CountOpponentsNear(matePos, NearbyRadius, opponents, mate);
                float openScore = 1f - Mathf.Clamp01(pressure / 3f);

                // 传球路线上是否有对手（简化：检查中点附近）
                Vector3 mid = (player.Position + matePos) * 0.5f;
                int midBlock = CountOpponentsNear(mid, 2.5f, opponents, null);
                float laneScore = 1f - Mathf.Clamp01(midBlock / 2f);

                float score = advanceScore * 0.4f + openScore * 0.35f + laneScore * 0.25f;
                // 战术冒险度影响：低冒险更选安全（laneScore 权重高），高冒险更选前压
                score = Mathf.Lerp(score, advanceScore, tactic.PassRisk * 0.3f);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = mate;
                }
            }
            return best;
        }

        /// <summary>
        /// 寻找最佳直塞目标：处于对方防线身前、有前插空当的队友。
        /// 直塞目标为队友前方跑动接应点。
        /// </summary>
        private PlayerEntity FindBestThroughTarget(PlayerEntity player, PlayerEntity[] teammates,
            PlayerEntity[] opponents)
        {
            if (teammates == null) return null;

            PlayerEntity best = null;
            float bestScore = -1f;
            Vector3 oppGoal = player.OppGoalPosition;

            for (int i = 0; i < teammates.Length; i++)
            {
                PlayerEntity mate = teammates[i];
                if (mate == null || mate == player || mate.IsGoalkeeper) continue;

                Vector3 matePos = mate.Position;
                float dist = HorizontalDist(player.Position, matePos);
                if (dist < 8f || dist > ThroughBallMaxDist) continue;

                // 队友前方 3 米处是否空当（直塞落点）
                Vector3 lead = matePos + player.AttackDirection * 3f;
                int leadPressure = CountOpponentsNear(lead, NearbyRadius, opponents, mate);
                if (leadPressure > 1) continue; // 落点有人，不直塞

                // 队友距对方球门距离（越近越有威胁）
                float mateDistToGoal = HorizontalDist(matePos, oppGoal);
                float threatScore = 1f - Mathf.Clamp01(mateDistToGoal / StadiumBuilder.HalfLength);

                float openScore = 1f - Mathf.Clamp01(leadPressure / 2f);

                float score = threatScore * 0.5f + openScore * 0.5f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = mate;
                }
            }
            return best;
        }

        /// <summary>根据 PlayerId 在上下文中查找球员。</summary>
        private PlayerEntity FindPlayerById(MatchContext context, int playerId)
        {
            if (playerId < 0) return null;
            PlayerEntity[] arr = context.HomePlayers;
            if (arr != null)
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    if (arr[i] != null && arr[i].PlayerId == playerId) return arr[i];
                }
            }
            arr = context.AwayPlayers;
            if (arr != null)
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    if (arr[i] != null && arr[i].PlayerId == playerId) return arr[i];
                }
            }
            return null;
        }

        /// <summary>水平面距离（忽略 y）。</summary>
        private float HorizontalDist(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>将目标坐标限制在球场范围内（含少量边距）。</summary>
        private Vector3 ClampToPitch(Vector3 pos)
        {
            float maxX = StadiumBuilder.HalfWidth - 1f;
            float maxZ = StadiumBuilder.HalfLength - 1f;
            pos.x = Mathf.Clamp(pos.x, -maxX, maxX);
            pos.z = Mathf.Clamp(pos.z, -maxZ, maxZ);
            pos.y = 0f;
            return pos;
        }

        #endregion
    }

    /// <summary>
    /// AI 动作事件。由 AIController 在执行决策时通过 EventBus 发布，
    /// 供 UI（动作提示）、音频（踢球音效）、回放等模块订阅。
    /// 注：EventBus 要求事件为 struct，故定义为值类型。
    /// </summary>
    public struct AIActionEvent
    {
        /// <summary>执行动作的球员 ID</summary>
        public int PlayerId;

        /// <summary>动作类型（对应 AIActionType 的 int 值）</summary>
        public int ActionType;

        /// <summary>目标位置</summary>
        public Vector3 Target;

        /// <summary>力量 0~1</summary>
        public float Power;
    }
}
