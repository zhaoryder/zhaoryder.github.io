using UnityEngine;

namespace FC26.Stadium
{
    /// <summary>
    /// 球门与场边道具构建器：运行时构建球门、角旗、广告牌、简易看台外壳
    /// 不依赖预制体，全部通过 GameObject.CreatePrimitive + 代码生成材质
    /// </summary>
    public class GoalAndProps : MonoBehaviour
    {
        // ===== 道具尺寸常量 =====
        private const float PostRadius = 0.06f;        // 球门立柱半径
        private const float CrossbarRadius = 0.06f;    // 横梁半径
        private const float NetHeight = 2.44f;         // 球网高度
        private const float NetDepth = 2.0f;           // 球网深度（向场外延伸）
        private const float FlagPoleHeight = 1.5f;     // 角旗杆高度
        private const float FlagPoleRadius = 0.03f;    // 角旗杆半径
        private const float FlagSize = 0.4f;           // 角旗布大小
        private const float BoardHeight = 1.0f;        // 广告牌高度
        private const float BoardThickness = 0.1f;     // 广告牌厚度
        private const float StandHeight = 8.0f;        // 看台高度
        private const float StandDepth = 12.0f;        // 看台深度
        private const float StandGap = 2.0f;           // 看台与球场间距

        // 道具根节点
        private Transform _propsRoot;

        /// <summary>
        /// 构建全部道具（球门、角旗、广告牌、看台外壳）
        /// </summary>
        public void BuildAll()
        {
            // 创建道具根节点
            GameObject rootObj = new GameObject("Props");
            _propsRoot = rootObj.transform;

            BuildGoals();           // 构建两端球门
            BuildCornerFlags();     // 构建四角角旗
            BuildBoards();          // 构建场边广告牌
            BuildStadiumShell();    // 构建简易看台外壳
        }

        /// <summary>
        /// 获取道具根节点（供其他模块访问）
        /// </summary>
        public Transform GetRoot()
        {
            return _propsRoot;
        }

        // ====================================================================
        #region 球门构建

        /// <summary>
        /// 构建两端球门（z+ 端和 z- 端）
        /// 球门高 2.44 米，宽 7.32 米，位于 z = ±52.5
        /// </summary>
        public void BuildGoals()
        {
            if (_propsRoot == null)
            {
                _propsRoot = new GameObject("Props").transform;
            }

            // z+ 端球门（朝向 z- 方向）
            BuildSingleGoal(1, "Goal_Front");
            // z- 端球门（朝向 z+ 方向）
            BuildSingleGoal(-1, "Goal_Back");
        }

        /// <summary>
        /// 构建单个球门
        /// </summary>
        /// <param name="sign">1=z+端，-1=z-端</param>
        /// <param name="name">球门名称</param>
        private void BuildSingleGoal(int sign, string name)
        {
            GameObject goalObj = new GameObject(name);
            goalObj.transform.SetParent(_propsRoot);

            float z = StadiumBuilder.HalfLength * sign;    // 球门底线 z 坐标
            float halfGoalW = StadiumBuilder.GoalWidth * 0.5f;
            float goalH = StadiumBuilder.GoalHeight;

            // 创建白色球门材质
            Material postMat = CreateColorMaterial("GoalPostMat", Color.white);

            // ---- 两根立柱 ----
            // 左立柱
            GameObject leftPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leftPost.name = "Post_Left";
            leftPost.transform.SetParent(goalObj.transform);
            leftPost.transform.position = new Vector3(-halfGoalW, goalH * 0.5f, z);
            leftPost.transform.localScale = new Vector3(PostRadius * 2f, goalH * 0.5f, PostRadius * 2f);
            leftPost.GetComponent<Renderer>().material = postMat;

            // 右立柱
            GameObject rightPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rightPost.name = "Post_Right";
            rightPost.transform.SetParent(goalObj.transform);
            rightPost.transform.position = new Vector3(halfGoalW, goalH * 0.5f, z);
            rightPost.transform.localScale = new Vector3(PostRadius * 2f, goalH * 0.5f, PostRadius * 2f);
            rightPost.GetComponent<Renderer>().material = postMat;

            // ---- 横梁 ----
            GameObject crossbar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crossbar.name = "Crossbar";
            crossbar.transform.SetParent(goalObj.transform);
            crossbar.transform.position = new Vector3(0f, goalH, z);
            // Cylinder 默认沿 y 轴，旋转 90 度使其沿 x 轴
            crossbar.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            crossbar.transform.localScale = new Vector3(
                CrossbarRadius * 2f,
                StadiumBuilder.GoalWidth * 0.5f,
                CrossbarRadius * 2f);
            crossbar.GetComponent<Renderer>().material = postMat;

            // ---- 球网 ----
            BuildGoalNet(goalObj.transform, sign, z, halfGoalW, goalH);
        }

        /// <summary>
        /// 构建球门网（使用半透明白色 Quads）
        /// 包括：顶部、左右两侧、后部
        /// </summary>
        private void BuildGoalNet(Transform parent, int sign, float z, float halfGoalW, float goalH)
        {
            // 球网材质（半透明白色）
            Material netMat = CreateNetMaterial();

            float netZ = z + NetDepth * sign;  // 球网后部 z 坐标

            // ---- 后部球网 ----
            GameObject backNet = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backNet.name = "Net_Back";
            backNet.transform.SetParent(parent);
            backNet.transform.position = new Vector3(0f, goalH * 0.5f, netZ);
            // Quad 默认朝向 +z，根据 sign 旋转使其朝向场内
            backNet.transform.rotation = Quaternion.Euler(0f, (sign > 0) ? 180f : 0f, 0f);
            backNet.transform.localScale = new Vector3(StadiumBuilder.GoalWidth, goalH, 1f);
            backNet.GetComponent<Renderer>().material = netMat;

            // ---- 顶部球网 ----
            GameObject topNet = GameObject.CreatePrimitive(PrimitiveType.Quad);
            topNet.name = "Net_Top";
            topNet.transform.SetParent(parent);
            topNet.transform.position = new Vector3(0f, goalH, z + NetDepth * 0.5f * sign);
            // 旋转使其水平（朝上）
            topNet.transform.rotation = Quaternion.Euler((sign > 0) ? -90f : 90f, 0f, 0f);
            topNet.transform.localScale = new Vector3(StadiumBuilder.GoalWidth, NetDepth, 1f);
            topNet.GetComponent<Renderer>().material = netMat;

            // ---- 左侧球网 ----
            GameObject leftNet = GameObject.CreatePrimitive(PrimitiveType.Quad);
            leftNet.name = "Net_Left";
            leftNet.transform.SetParent(parent);
            leftNet.transform.position = new Vector3(-halfGoalW, goalH * 0.5f, z + NetDepth * 0.5f * sign);
            // 旋转使其朝向 +x 方向
            leftNet.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            leftNet.transform.localScale = new Vector3(NetDepth, goalH, 1f);
            leftNet.GetComponent<Renderer>().material = netMat;

            // ---- 右侧球网 ----
            GameObject rightNet = GameObject.CreatePrimitive(PrimitiveType.Quad);
            rightNet.name = "Net_Right";
            rightNet.transform.SetParent(parent);
            rightNet.transform.position = new Vector3(halfGoalW, goalH * 0.5f, z + NetDepth * 0.5f * sign);
            // 旋转使其朝向 -x 方向
            rightNet.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            rightNet.transform.localScale = new Vector3(NetDepth, goalH, 1f);
            rightNet.GetComponent<Renderer>().material = netMat;
        }

        #endregion

        // ====================================================================
        #region 角旗构建

        /// <summary>
        /// 构建四角角旗
        /// 角旗位于球场四个角（±34, 0, ±52.5）
        /// </summary>
        public void BuildCornerFlags()
        {
            if (_propsRoot == null)
            {
                _propsRoot = new GameObject("Props").transform;
            }

            // 四个角的位置
            Vector3[] corners = new Vector3[]
            {
                new Vector3(-StadiumBuilder.HalfWidth, 0f, -StadiumBuilder.HalfLength),  // 左后角
                new Vector3(StadiumBuilder.HalfWidth, 0f, -StadiumBuilder.HalfLength),   // 右后角
                new Vector3(-StadiumBuilder.HalfWidth, 0f, StadiumBuilder.HalfLength),   // 左前角
                new Vector3(StadiumBuilder.HalfWidth, 0f, StadiumBuilder.HalfLength),    // 右前角
            };

            string[] names = { "CornerFlag_BL", "CornerFlag_BR", "CornerFlag_FL", "CornerFlag_FR" };

            for (int i = 0; i < 4; i++)
            {
                BuildSingleCornerFlag(corners[i], names[i]);
            }
        }

        /// <summary>
        /// 构建单个角旗（旗杆 + 旗布）
        /// </summary>
        private void BuildSingleCornerFlag(Vector3 position, string name)
        {
            GameObject flagObj = new GameObject(name);
            flagObj.transform.SetParent(_propsRoot);
            flagObj.transform.position = position;

            // 旗杆材质（白色）
            Material poleMat = CreateColorMaterial("PoleMat", Color.white);
            // 旗布材质（红色）
            Material flagMat = CreateColorMaterial("FlagMat", new Color(0.9f, 0.15f, 0.15f));

            // ---- 旗杆 ----
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(flagObj.transform);
            pole.transform.position = position + new Vector3(0f, FlagPoleHeight * 0.5f, 0f);
            pole.transform.localScale = new Vector3(FlagPoleRadius * 2f, FlagPoleHeight * 0.5f, FlagPoleRadius * 2f);
            pole.GetComponent<Renderer>().material = poleMat;

            // ---- 旗布 ----
            GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Quad);
            flag.name = "Flag";
            flag.transform.SetParent(flagObj.transform);
            flag.transform.position = position + new Vector3(FlagSize * 0.5f, FlagPoleHeight - FlagSize * 0.5f, 0f);
            flag.transform.localScale = new Vector3(FlagSize, FlagSize, 1f);
            flag.GetComponent<Renderer>().material = flagMat;
        }

        #endregion

        // ====================================================================
        #region 广告牌构建

        /// <summary>
        /// 构建场边广告牌占位（简单彩色 Quad）
        /// 沿四周边线外侧排列
        /// </summary>
        public void BuildBoards()
        {
            if (_propsRoot == null)
            {
                _propsRoot = new GameObject("Props").transform;
            }

            Transform boardsParent = new GameObject("Boards").transform;
            boardsParent.SetParent(_propsRoot);

            float boardOffset = 1.5f;   // 广告牌距边线距离
            float boardWidth = 4.0f;    // 单块广告牌宽度
            float boardY = BoardHeight * 0.5f;

            // 广告牌颜色循环
            Color[] boardColors = new Color[]
            {
                new Color(0.9f, 0.2f, 0.2f),   // 红
                new Color(0.2f, 0.4f, 0.9f),   // 蓝
                new Color(0.2f, 0.8f, 0.3f),   // 绿
                new Color(0.9f, 0.8f, 0.2f),   // 黄
                new Color(0.9f, 0.5f, 0.1f),   // 橙
            };

            int colorIndex = 0;

            // ---- 两侧边线广告牌（沿 z 轴排列）----
            int sideCount = Mathf.FloorToInt(StadiumBuilder.PitchLength / boardWidth);
            float sideStartZ = -StadiumBuilder.PitchLength * 0.5f + boardWidth * 0.5f;

            for (int i = 0; i < sideCount; i++)
            {
                float z = sideStartZ + i * boardWidth;
                Color color = boardColors[colorIndex % boardColors.Length];
                colorIndex++;

                // 左侧（x = -34 - offset）
                CreateBoard(boardsParent,
                    new Vector3(-StadiumBuilder.HalfWidth - boardOffset, boardY, z),
                    Quaternion.Euler(0f, 90f, 0f),
                    boardWidth, BoardHeight, color, $"Board_Left_{i}");

                // 右侧（x = 34 + offset）
                CreateBoard(boardsParent,
                    new Vector3(StadiumBuilder.HalfWidth + boardOffset, boardY, z),
                    Quaternion.Euler(0f, -90f, 0f),
                    boardWidth, BoardHeight, color, $"Board_Right_{i}");
            }

            // ---- 两端底线广告牌（沿 x 轴排列）----
            int endCount = Mathf.FloorToInt(StadiumBuilder.PitchWidth / boardWidth);
            float endStartX = -StadiumBuilder.PitchWidth * 0.5f + boardWidth * 0.5f;

            for (int i = 0; i < endCount; i++)
            {
                float x = endStartX + i * boardWidth;
                Color color = boardColors[colorIndex % boardColors.Length];
                colorIndex++;

                // z- 端
                CreateBoard(boardsParent,
                    new Vector3(x, boardY, -StadiumBuilder.HalfLength - boardOffset),
                    Quaternion.Euler(0f, 0f, 0f),
                    boardWidth, BoardHeight, color, $"Board_Back_{i}");

                // z+ 端
                CreateBoard(boardsParent,
                    new Vector3(x, boardY, StadiumBuilder.HalfLength + boardOffset),
                    Quaternion.Euler(0f, 180f, 0f),
                    boardWidth, BoardHeight, color, $"Board_Front_{i}");
            }
        }

        /// <summary>
        /// 创建单块广告牌
        /// </summary>
        private void CreateBoard(Transform parent, Vector3 pos, Quaternion rot,
            float width, float height, Color color, string name)
        {
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = name;
            board.transform.SetParent(parent);
            board.transform.position = pos;
            board.transform.rotation = rot;
            board.transform.localScale = new Vector3(width, height, BoardThickness);
            board.GetComponent<Renderer>().material = CreateColorMaterial($"BoardMat_{name}", color);
        }

        #endregion

        // ====================================================================
        #region 看台外壳构建

        /// <summary>
        /// 构建简易看台外壳（环形低面数结构）
        /// 四面看台围绕球场，呈梯形斜面
        /// </summary>
        public void BuildStadiumShell()
        {
            if (_propsRoot == null)
            {
                _propsRoot = new GameObject("Props").transform;
            }

            Transform standParent = new GameObject("StadiumShell").transform;
            standParent.SetParent(_propsRoot);

            // 看台材质（深灰色）
            Material standMat = CreateColorMaterial("StandMat", new Color(0.25f, 0.25f, 0.28f));

            float pitchW = StadiumBuilder.PitchWidth;
            float pitchL = StadiumBuilder.PitchLength;
            float offset = StandGap;        // 看台与球场间距
            float halfW = pitchW * 0.5f + offset;
            float halfL = pitchL * 0.5f + offset;

            // ---- 两侧看台（沿 z 轴方向）----
            BuildSingleStand(standParent, "Stand_Left",
                new Vector3(-halfW - StandDepth * 0.5f, StandHeight * 0.5f, 0f),
                new Vector3(StandDepth, StandHeight, pitchL + offset * 2f),
                standMat);
            BuildSingleStand(standParent, "Stand_Right",
                new Vector3(halfW + StandDepth * 0.5f, StandHeight * 0.5f, 0f),
                new Vector3(StandDepth, StandHeight, pitchL + offset * 2f),
                standMat);

            // ---- 两端看台（沿 x 轴方向）----
            BuildSingleStand(standParent, "Stand_Back",
                new Vector3(0f, StandHeight * 0.5f, -halfL - StandDepth * 0.5f),
                new Vector3(pitchW + offset * 2f, StandHeight, StandDepth),
                standMat);
            BuildSingleStand(standParent, "Stand_Front",
                new Vector3(0f, StandHeight * 0.5f, halfL + StandDepth * 0.5f),
                new Vector3(pitchW + offset * 2f, StandHeight, StandDepth),
                standMat);

            // ---- 看台顶部斜面（增加立体感）----
            Material topMat = CreateColorMaterial("StandTopMat", new Color(0.18f, 0.18f, 0.20f));
            BuildStandRoof(standParent, "Roof_Left",
                new Vector3(-halfW - StandDepth, 0f, 0f), halfL + offset, 1, topMat);
            BuildStandRoof(standParent, "Roof_Right",
                new Vector3(halfW + StandDepth, 0f, 0f), halfL + offset, -1, topMat);
            BuildStandRoof(standParent, "Roof_Back",
                new Vector3(0f, 0f, -halfL - StandDepth), halfW + offset, 1, topMat, true);
            BuildStandRoof(standParent, "Roof_Front",
                new Vector3(0f, 0f, halfL + StandDepth), halfW + offset, -1, topMat, true);
        }

        /// <summary>
        /// 构建单面看台（Cube 占位）
        /// </summary>
        private void BuildSingleStand(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stand.name = name;
            stand.transform.SetParent(parent);
            stand.transform.position = pos;
            stand.transform.localScale = scale;
            stand.GetComponent<Renderer>().material = mat;
        }

        /// <summary>
        /// 构建看台顶部斜面（使用 Quad，倾斜一定角度）
        /// </summary>
        private void BuildStandRoof(Transform parent, string name, Vector3 basePos,
            float length, int dir, Material mat, bool isEnd = false)
        {
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Quad);
            roof.name = name;
            roof.transform.SetParent(parent);

            if (!isEnd)
            {
                // 侧边看台顶部（沿 z 轴方向延伸）
                roof.transform.position = new Vector3(basePos.x + StandDepth * 0.5f * dir,
                    StandHeight + StandHeight * 0.3f, basePos.z);
                roof.transform.rotation = Quaternion.Euler(0f, 90f, 60f * dir);
                roof.transform.localScale = new Vector3(StandHeight * 1.2f, length * 2f, 1f);
            }
            else
            {
                // 端部看台顶部（沿 x 轴方向延伸）
                roof.transform.position = new Vector3(basePos.x,
                    StandHeight + StandHeight * 0.3f, basePos.z + StandDepth * 0.5f * dir);
                roof.transform.rotation = Quaternion.Euler(60f * dir, 0f, 0f);
                roof.transform.localScale = new Vector3(length * 2f, StandHeight * 1.2f, 1f);
            }
            roof.GetComponent<Renderer>().material = mat;
        }

        #endregion

        // ====================================================================
        #region 材质工具方法

        /// <summary>
        /// 创建纯色材质（URP/Lit）
        /// </summary>
        /// <param name="name">材质名称</param>
        /// <param name="color">材质颜色</param>
        private Material CreateColorMaterial(string name, Color color)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.name = name;
            mat.color = color;
            return mat;
        }

        /// <summary>
        /// 创建球网材质（半透明白色）
        /// </summary>
        private Material CreateNetMaterial()
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.name = "NetMaterial";
            mat.color = new Color(1f, 1f, 1f, 0.35f);
            // 设置为透明渲染模式
            mat.SetFloat("_Surface", 1);        // 1 = Transparent
            mat.SetFloat("_Blend", 0);          // 0 = Alpha
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return mat;
        }

        #endregion
    }
}
