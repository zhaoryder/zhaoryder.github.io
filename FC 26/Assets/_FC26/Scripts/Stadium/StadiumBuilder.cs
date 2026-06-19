using UnityEngine;

namespace FC26.Stadium
{
    /// <summary>
    /// 球场构建器单例：运行时构建整个球场（草坪、条纹材质、白色标线）
    /// 不依赖预制体，全部通过 GameObject.CreatePrimitive + 代码生成材质
    /// 球场尺寸：长 105 米（z 轴），宽 68 米（x 轴）
    /// </summary>
    public class StadiumBuilder : MonoSingleton<StadiumBuilder>
    {
        // ===== 球场尺寸常量（单位：米）=====
        public const float PitchLength = 105f;          // 球场长度（z 轴方向，-52.5 ~ 52.5）
        public const float PitchWidth = 68f;            // 球场宽度（x 轴方向，-34 ~ 34）
        public const float HalfLength = 52.5f;          // 半场长度
        public const float HalfWidth = 34f;             // 半场宽度

        public const float GoalHeight = 2.44f;          // 球门高度
        public const float GoalWidth = 7.32f;           // 球门宽度
        public const float PenaltyAreaWidth = 40.32f;   // 禁区宽度
        public const float PenaltyAreaDepth = 16.5f;    // 禁区深度（从底线向场内）
        public const float GoalAreaWidth = 18.32f;      // 小禁区宽度
        public const float GoalAreaDepth = 5.5f;        // 小禁区深度
        public const float CenterCircleRadius = 9.15f;  // 中圈半径
        public const float PenaltySpotDist = 11f;       // 点球点距底线距离
        public const float CornerArcRadius = 1f;        // 角球弧半径

        public const float LineWidth = 0.12f;           // 标线宽度
        public const float LineY = 0.02f;               // 标线 Y 坐标（略高于草坪避免 z-fighting）
        public const float LineThickness = 0.02f;       // 标线厚度（Cube 的高度）

        // 球场根节点
        private Transform _root;

        /// <summary>
        /// 构建整个球场
        /// 外部调用此方法触发完整球场构建
        /// </summary>
        public void Build()
        {
            // 若已构建过则先清理
            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
            }

            // 创建球场根节点
            GameObject rootObj = new GameObject("Stadium");
            _root = rootObj.transform;

            BuildGrass();   // 构建草坪（含条纹材质）
            BuildLines();   // 构建所有白色标线
        }

        /// <summary>
        /// 获取球场根节点（供其他模块挂载子物体）
        /// </summary>
        public Transform GetRoot()
        {
            return _root;
        }

        // ====================================================================
        #region 草坪构建

        /// <summary>
        /// 构建草坪平面（带条纹材质）
        /// 使用 Unity 内置 Plane 图元，缩放至 105x68 米
        /// </summary>
        private void BuildGrass()
        {
            // 创建平面（Unity 默认 Plane 是 10x10 米，需要缩放）
            GameObject grass = GameObject.CreatePrimitive(PrimitiveType.Plane);
            grass.name = "Grass";
            grass.transform.SetParent(_root);
            // Plane 默认 10x10，缩放到 105x68
            grass.transform.localScale = new Vector3(PitchWidth / 10f, 1f, PitchLength / 10f);
            grass.transform.position = Vector3.zero;

            // 生成条纹材质并赋值
            Material grassMat = CreateStripedGrassMaterial();
            grass.GetComponent<Renderer>().material = grassMat;
        }

        /// <summary>
        /// 创建条纹草坪材质（程序化生成 Texture2D，交替深浅绿色条纹）
        /// 条纹沿 z 轴方向（球场长度方向）交替
        /// </summary>
        private Material CreateStripedGrassMaterial()
        {
            // 条纹纹理尺寸
            int textureSize = 1024;
            int stripeCount = 18;                       // 18 条条纹（符合真实球场视觉）
            int stripeHeight = textureSize / stripeCount;

            // 创建纹理
            Texture2D tex = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            // 深浅绿色定义
            Color darkGreen = new Color(0.18f, 0.45f, 0.15f);   // 深绿
            Color lightGreen = new Color(0.35f, 0.62f, 0.22f);  // 浅绿

            // 逐像素填充条纹
            Color[] pixels = new Color[textureSize * textureSize];
            for (int y = 0; y < textureSize; y++)
            {
                // 沿 y 方向（对应纹理 V 轴，映射到世界 z 轴）交替条纹
                int stripeIndex = y / stripeHeight;
                Color color = (stripeIndex % 2 == 0) ? darkGreen : lightGreen;
                for (int x = 0; x < textureSize; x++)
                {
                    pixels[y * textureSize + x] = color;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            // 创建 URP/Lit 材质
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.name = "GrassMaterial";
            mat.SetTexture("_BaseMap", tex);
            mat.color = Color.white;
            return mat;
        }

        #endregion

        // ====================================================================
        #region 标线构建

        /// <summary>
        /// 构建所有白色标线
        /// 包括：边界线、中线、中圈、中点、禁区、小禁区、点球点、点球弧、角球弧
        /// </summary>
        private void BuildLines()
        {
            Transform linesParent = new GameObject("Lines").transform;
            linesParent.SetParent(_root);

            // 创建共享的白色标线材质
            Material lineMat = CreateLineMaterial();

            BuildBoundaryLines(linesParent, lineMat);       // 边界线（4 条边）
            BuildCenterLine(linesParent, lineMat);          // 中线
            BuildCenterCircle(linesParent, lineMat);        // 中圈
            BuildCenterSpot(linesParent, lineMat);          // 中点
            BuildPenaltyArea(linesParent, lineMat, 1);      // z+ 端禁区
            BuildPenaltyArea(linesParent, lineMat, -1);     // z- 端禁区
            BuildGoalArea(linesParent, lineMat, 1);         // z+ 端小禁区
            BuildGoalArea(linesParent, lineMat, -1);        // z- 端小禁区
            BuildPenaltySpot(linesParent, lineMat, 1);      // z+ 端点球点
            BuildPenaltySpot(linesParent, lineMat, -1);     // z- 端点球点
            BuildPenaltyArc(linesParent, lineMat, 1);       // z+ 端点球弧
            BuildPenaltyArc(linesParent, lineMat, -1);      // z- 端点球弧
            BuildCornerArcs(linesParent, lineMat);          // 四角角球弧
        }

        /// <summary>
        /// 创建白色标线材质（URP/Lit，纯白色）
        /// </summary>
        private Material CreateLineMaterial()
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.name = "LineMaterial";
            mat.color = Color.white;
            return mat;
        }

        /// <summary>
        /// 构建边界线（球场四周 4 条边）
        /// </summary>
        private void BuildBoundaryLines(Transform parent, Material mat)
        {
            // 两条边线（沿 z 轴，x = ±34）
            CreateStraightLine("Boundary_Left",
                new Vector3(-HalfWidth, LineY, -HalfLength),
                new Vector3(-HalfWidth, LineY, HalfLength), parent, mat);
            CreateStraightLine("Boundary_Right",
                new Vector3(HalfWidth, LineY, -HalfLength),
                new Vector3(HalfWidth, LineY, HalfLength), parent, mat);

            // 两条底线（沿 x 轴，z = ±52.5）
            CreateStraightLine("Boundary_Back",
                new Vector3(-HalfWidth, LineY, -HalfLength),
                new Vector3(HalfWidth, LineY, -HalfLength), parent, mat);
            CreateStraightLine("Boundary_Front",
                new Vector3(-HalfWidth, LineY, HalfLength),
                new Vector3(HalfWidth, LineY, HalfLength), parent, mat);
        }

        /// <summary>
        /// 构建中线（从左到右，z = 0）
        /// </summary>
        private void BuildCenterLine(Transform parent, Material mat)
        {
            CreateStraightLine("CenterLine",
                new Vector3(-HalfWidth, LineY, 0f),
                new Vector3(HalfWidth, LineY, 0f), parent, mat);
        }

        /// <summary>
        /// 构建中圈（圆心在原点，半径 9.15 米）
        /// </summary>
        private void BuildCenterCircle(Transform parent, Material mat)
        {
            CreateCircleLine("CenterCircle", Vector3.zero, CenterCircleRadius, parent, mat, 64);
        }

        /// <summary>
        /// 构建中点（原点处的小圆点）
        /// </summary>
        private void BuildCenterSpot(Transform parent, Material mat)
        {
            CreateSpot("CenterSpot", new Vector3(0f, LineY, 0f), parent, mat, 0.2f);
        }

        /// <summary>
        /// 构建禁区（sign=1 为 z+ 端，sign=-1 为 z- 端）
        /// 禁区宽 40.32，深 16.5
        /// </summary>
        private void BuildPenaltyArea(Transform parent, Material mat, int sign)
        {
            float z = HalfLength * sign;            // 底线 z 坐标
            float zInner = z - PenaltyAreaDepth * sign;  // 禁区前沿 z 坐标
            float halfPAW = PenaltyAreaWidth * 0.5f;

            // 三条线：两条侧线 + 一条前沿线
            CreateStraightLine($"PenaltyArea_Side_L_{sign}",
                new Vector3(-halfPAW, LineY, z),
                new Vector3(-halfPAW, LineY, zInner), parent, mat);
            CreateStraightLine($"PenaltyArea_Side_R_{sign}",
                new Vector3(halfPAW, LineY, z),
                new Vector3(halfPAW, LineY, zInner), parent, mat);
            CreateStraightLine($"PenaltyArea_Front_{sign}",
                new Vector3(-halfPAW, LineY, zInner),
                new Vector3(halfPAW, LineY, zInner), parent, mat);
        }

        /// <summary>
        /// 构建小禁区（sign=1 为 z+ 端，sign=-1 为 z- 端）
        /// 小禁区宽 18.32，深 5.5
        /// </summary>
        private void BuildGoalArea(Transform parent, Material mat, int sign)
        {
            float z = HalfLength * sign;            // 底线 z 坐标
            float zInner = z - GoalAreaDepth * sign;     // 小禁区前沿 z 坐标
            float halfGAW = GoalAreaWidth * 0.5f;

            // 三条线：两条侧线 + 一条前沿线
            CreateStraightLine($"GoalArea_Side_L_{sign}",
                new Vector3(-halfGAW, LineY, z),
                new Vector3(-halfGAW, LineY, zInner), parent, mat);
            CreateStraightLine($"GoalArea_Side_R_{sign}",
                new Vector3(halfGAW, LineY, z),
                new Vector3(halfGAW, LineY, zInner), parent, mat);
            CreateStraightLine($"GoalArea_Front_{sign}",
                new Vector3(-halfGAW, LineY, zInner),
                new Vector3(halfGAW, LineY, zInner), parent, mat);
        }

        /// <summary>
        /// 构建点球点（距底线 11 米）
        /// </summary>
        private void BuildPenaltySpot(Transform parent, Material mat, int sign)
        {
            float z = (HalfLength - PenaltySpotDist) * sign;
            CreateSpot($"PenaltySpot_{sign}", new Vector3(0f, LineY, z), parent, mat, 0.2f);
        }

        /// <summary>
        /// 构建点球弧（点球点为圆心，半径 9.15，只画禁区前沿外的弧线）
        /// </summary>
        private void BuildPenaltyArc(Transform parent, Material mat, int sign)
        {
            float z = (HalfLength - PenaltySpotDist) * sign;   // 点球点 z 坐标
            Vector3 center = new Vector3(0f, LineY, z);

            // 禁区前沿 z 坐标
            float zInner = (HalfLength - PenaltyAreaDepth) * sign;

            // 计算弧线的起始和结束角度
            // 点球点到禁区前沿的距离
            float distToEdge = Mathf.Abs(z - zInner);
            // 弧线与禁区前沿交点处的角度（从点球点向禁区前沿方向为 0 度）
            float halfAngle = Mathf.Acos(distToEdge / CenterCircleRadius) * Mathf.Rad2Deg;

            // 弧线朝向场内（远离底线方向）
            float baseAngle = (sign > 0) ? 180f : 0f;  // z+ 端弧线朝向 z- 方向，z- 端朝向 z+ 方向
            float arcStart = baseAngle - halfAngle;
            float arcEnd = baseAngle + halfAngle;

            CreateCircleLine($"PenaltyArc_{sign}", center, CenterCircleRadius, parent, mat, 32, arcStart, arcEnd);
        }

        /// <summary>
        /// 构建四角角球弧（半径 1 米）
        /// </summary>
        private void BuildCornerArcs(Transform parent, Material mat)
        {
            // 四个角的位置和弧线朝向
            // 左后角 (-34, -52.5)：弧线朝向场内（+x, +z 方向），即 0° ~ 90°
            CreateCircleLine("CornerArc_BL",
                new Vector3(-HalfWidth, LineY, -HalfLength), CornerArcRadius,
                parent, mat, 16, 0f, 90f);

            // 右后角 (34, -52.5)：弧线朝向场内（-x, +z 方向），即 90° ~ 180°
            CreateCircleLine("CornerArc_BR",
                new Vector3(HalfWidth, LineY, -HalfLength), CornerArcRadius,
                parent, mat, 16, 90f, 180f);

            // 左前角 (-34, 52.5)：弧线朝向场内（+x, -z 方向），即 270° ~ 360°
            CreateCircleLine("CornerArc_FL",
                new Vector3(-HalfWidth, LineY, HalfLength), CornerArcRadius,
                parent, mat, 16, 270f, 360f);

            // 右前角 (34, 52.5)：弧线朝向场内（-x, -z 方向），即 180° ~ 270°
            CreateCircleLine("CornerArc_FR",
                new Vector3(HalfWidth, LineY, HalfLength), CornerArcRadius,
                parent, mat, 16, 180f, 270f);
        }

        #endregion

        // ====================================================================
        #region 标线创建工具方法

        /// <summary>
        /// 创建一条直标线（使用扁平 Cube）
        /// 自动计算中点、长度和旋转角度
        /// </summary>
        /// <param name="name">物体名称</param>
        /// <param name="start">起点世界坐标</param>
        /// <param name="end">终点世界坐标</param>
        /// <param name="parent">父节点</param>
        /// <param name="mat">标线材质</param>
        private void CreateStraightLine(string name, Vector3 start, Vector3 end, Transform parent, Material mat)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = name;
            line.transform.SetParent(parent);

            // 计算中点位置
            Vector3 mid = (start + end) * 0.5f;
            line.transform.position = mid;

            // 计算长度和方向
            Vector3 dir = end - start;
            float length = dir.magnitude;
            line.transform.localScale = new Vector3(LineWidth, LineThickness, length);

            // 计算旋转角度（绕 Y 轴，使 Cube 的 z 轴对齐方向）
            float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            line.transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // 赋予材质
            line.GetComponent<Renderer>().material = mat;
        }

        /// <summary>
        /// 创建圆形/弧形标线（使用 LineRenderer）
        /// </summary>
        /// <param name="name">物体名称</param>
        /// <param name="center">圆心世界坐标</param>
        /// <param name="radius">半径</param>
        /// <param name="parent">父节点</param>
        /// <param name="mat">标线材质</param>
        /// <param name="segments">圆弧分段数（越大越圆滑）</param>
        /// <param name="arcStart">弧线起始角度（度，0=+x 方向，逆时针）</param>
        /// <param name="arcEnd">弧线结束角度（度）</param>
        private void CreateCircleLine(string name, Vector3 center, float radius, Transform parent,
            Material mat, int segments, float arcStart = 0f, float arcEnd = 360f)
        {
            GameObject circleObj = new GameObject(name);
            circleObj.transform.SetParent(parent);
            circleObj.transform.position = center;

            LineRenderer lr = circleObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            // 完整圆则闭合
            bool isFullCircle = (arcEnd - arcStart >= 360f - 0.01f);
            lr.loop = isFullCircle;
            lr.widthMultiplier = LineWidth;
            lr.material = mat;
            lr.numCornerVertices = 4;
            lr.numCapVertices = 4;
            lr.startColor = Color.white;
            lr.endColor = Color.white;

            // 计算点位
            int pointCount = isFullCircle ? segments : segments + 1;
            lr.positionCount = pointCount;

            float arcRange = arcEnd - arcStart;
            for (int i = 0; i < pointCount; i++)
            {
                float angle = (arcStart + arcRange * i / segments) * Mathf.Deg2Rad;
                // 在局部坐标系中计算位置（圆心已在 center，但 useWorldSpace=false 时位置是相对父节点）
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * radius,
                    LineY - center.y,   // 相对于圆心的 y 偏移
                    Mathf.Sin(angle) * radius
                );
                lr.SetPosition(i, pos);
            }
        }

        /// <summary>
        /// 创建一个圆点标记（点球点、中点等）
        /// </summary>
        /// <param name="name">物体名称</param>
        /// <param name="position">世界坐标</param>
        /// <param name="parent">父节点</param>
        /// <param name="mat">标线材质</param>
        /// <param name="radius">圆点半径</param>
        private void CreateSpot(string name, Vector3 position, Transform parent, Material mat, float radius)
        {
            GameObject spot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spot.name = name;
            spot.transform.SetParent(parent);
            spot.transform.position = position;
            // Cylinder 默认 1x1x1（高度沿 y 轴），缩放为扁平圆点
            spot.transform.localScale = new Vector3(radius * 2f, LineThickness * 0.5f, radius * 2f);
            spot.GetComponent<Renderer>().material = mat;
        }

        #endregion
    }
}
