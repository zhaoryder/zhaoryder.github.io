using UnityEngine;

namespace FC26.Data
{
    /// <summary>
    /// 阵型坐标表。
    /// 坐标系：球场归一化坐标。
    ///   x：左右方向，-1（左路边线）~ 1（右路边线）。
    ///   y：前后方向，-1（本方球门线）~ 1（对方球门线）。
    ///   主队球门在 y = -1，进攻方向为 +y。
    /// 每个阵型提供 11 个首发位置，顺序固定为：
    ///   [GK, 后卫线(从左到右), 中场线(从左到右/后到前), 前锋线(从左到右)]。
    /// 该顺序需与 TeamData.Starters 数组顺序一一对应。
    /// </summary>
    public static class Formations
    {
        /// <summary>
        /// 4-4-2 阵型坐标。
        /// 顺序：GK, LB, LCB, RCB, RB, LM, LCM, RCM, RM, LST, RST
        /// </summary>
        public static readonly Vector2[] F442 = new Vector2[]
        {
            new Vector2(0.00f, -0.92f),  // GK  门将
            new Vector2(-0.70f, -0.55f), // LB  左后卫
            new Vector2(-0.25f, -0.60f), // LCB 左中卫
            new Vector2(0.25f, -0.60f),  // RCB 右中卫
            new Vector2(0.70f, -0.55f),  // RB  右后卫
            new Vector2(-0.70f, -0.05f), // LM  左前卫
            new Vector2(-0.25f, -0.10f), // LCM 左中前卫
            new Vector2(0.25f, -0.10f),  // RCM 右中前卫
            new Vector2(0.70f, -0.05f),  // RM  右前卫
            new Vector2(-0.25f, 0.40f),  // LST 左前锋
            new Vector2(0.25f, 0.40f)    // RST 右前锋
        };

        /// <summary>
        /// 4-3-3 阵型坐标。
        /// 顺序：GK, LB, LCB, RCB, RB, LCM, CM, RCM, LW, ST, RW
        /// </summary>
        public static readonly Vector2[] F433 = new Vector2[]
        {
            new Vector2(0.00f, -0.92f),  // GK  门将
            new Vector2(-0.70f, -0.55f), // LB  左后卫
            new Vector2(-0.25f, -0.60f), // LCB 左中卫
            new Vector2(0.25f, -0.60f),  // RCB 右中卫
            new Vector2(0.70f, -0.55f),  // RB  右后卫
            new Vector2(-0.30f, -0.15f), // LCM 左中前卫
            new Vector2(0.00f, -0.05f),  // CM  中前卫（拖后组织）
            new Vector2(0.30f, -0.15f),  // RCM 右中前卫
            new Vector2(-0.60f, 0.40f),  // LW  左边锋
            new Vector2(0.00f, 0.50f),   // ST  中锋
            new Vector2(0.60f, 0.40f)    // RW  右边锋
        };

        /// <summary>
        /// 4-2-3-1 阵型坐标。
        /// 顺序：GK, LB, LCB, RCB, RB, LDM, RDM, LAM, CAM, RAM, ST
        /// </summary>
        public static readonly Vector2[] F4231 = new Vector2[]
        {
            new Vector2(0.00f, -0.92f),  // GK  门将
            new Vector2(-0.70f, -0.55f), // LB  左后卫
            new Vector2(-0.25f, -0.60f), // LCB 左中卫
            new Vector2(0.25f, -0.60f),  // RCB 右中卫
            new Vector2(0.70f, -0.55f),  // RB  右后卫
            new Vector2(-0.25f, -0.25f), // LDM 左后腰
            new Vector2(0.25f, -0.25f),  // RDM 右后腰
            new Vector2(-0.60f, 0.20f),  // LAM 左前腰
            new Vector2(0.00f, 0.25f),   // CAM 中前腰
            new Vector2(0.60f, 0.20f),   // RAM 右前腰
            new Vector2(0.00f, 0.50f)    // ST  单前锋
        };

        /// <summary>
        /// 根据阵型类型返回 11 个首发位置的归一化坐标。
        /// 若传入未知枚举值，默认回退到 4-4-2。
        /// </summary>
        public static Vector2[] GetPositions(FormationType formation)
        {
            switch (formation)
            {
                case FormationType.F442: return F442;
                case FormationType.F433: return F433;
                case FormationType.F4231: return F4231;
                default: return F442;
            }
        }
    }
}
