using System.Collections.Generic;
using UnityEngine;

namespace FC26.Stadium
{
    /// <summary>
    /// 观众控制器单例：在看台位置生成低面数观众占位
    /// 使用 GPU 实例化（Graphics.DrawMeshInstanced）保证性能
    /// 观众数量约 2000-5000，随机颜色分布
    /// Update 中可选颜色波动模拟观众动作（性能优先时可关闭）
    /// </summary>
    public class CrowdController : MonoSingleton<CrowdController>
    {
        // ===== 观众配置 =====
        [Header("观众配置")]
        [Tooltip("每面看台的观众行数")]
        [SerializeField] private int rowsPerStand = 15;
        [Tooltip("每面看台的观众列数")]
        [SerializeField] private int colsPerStand = 50;
        [Tooltip("观众占位尺寸")]
        [SerializeField] private Vector3 crowdSize = new Vector3(0.35f, 0.5f, 0.35f);
        [Tooltip("是否启用观众波动动画")]
        [SerializeField] private bool enableWaveAnimation = true;
        [Tooltip("波动动画速度")]
        [SerializeField] private float waveSpeed = 2.0f;
        [Tooltip("波动幅度（米）")]
        [SerializeField] private float waveAmplitude = 0.08f;

        // ===== 看台布局常量 =====
        private const float StandGap = 2.0f;       // 看台与球场间距
        private const float StandDepth = 12.0f;    // 看台深度
        private const float StandHeight = 8.0f;    // 看台高度
        private const float RowHeightStep = 0.5f;  // 每行高度递增
        private const float RowDepthStep = 0.6f;   // 每行深度递增

        // ===== 运行时数据 =====
        private Mesh _crowdMesh;                           // 观众占位网格（共享 Cube）
        private List<Material> _crowdMaterials;            // 按颜色分组的材质
        private List<List<Matrix4x4>> _matricesByColor;   // 按颜色分组的变换矩阵
        private List<List<Vector3>> _basePositionsByColor; // 按颜色分组的基础位置（用于波动）
        private int _totalCrowdCount = 0;                  // 总观众数

        // GPU 实例化单次最大数量
        private const int MaxInstancesPerDraw = 1023;

        /// <summary>
        /// 构建观众群体
        /// 在四面看台上生成观众占位，按颜色分组以支持 GPU 实例化
        /// </summary>
        public void BuildCrowd()
        {
            // 清理旧数据
            ClearCrowd();

            // 获取共享 Cube 网格
            _crowdMesh = GetSharedCubeMesh();

            // 定义观众颜色调色板（8 种颜色）
            Color[] palette = new Color[]
            {
                new Color(0.85f, 0.20f, 0.20f),   // 红
                new Color(0.20f, 0.45f, 0.85f),   // 蓝
                new Color(0.20f, 0.75f, 0.35f),   // 绿
                new Color(0.90f, 0.80f, 0.25f),   // 黄
                new Color(0.85f, 0.50f, 0.15f),   // 橙
                new Color(0.70f, 0.25f, 0.75f),   // 紫
                new Color(0.60f, 0.60f, 0.65f),   // 灰
                new Color(0.95f, 0.95f, 0.95f),   // 白
            };

            // 为每种颜色创建材质（启用 GPU 实例化）
            _crowdMaterials = new List<Material>();
            for (int i = 0; i < palette.Length; i++)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.name = $"CrowdMat_{i}";
                mat.color = palette[i];
                mat.enableInstancing = true;   // 启用 GPU 实例化
                _crowdMaterials.Add(mat);
            }

            // 初始化按颜色分组的矩阵列表
            _matricesByColor = new List<List<Matrix4x4>>();
            _basePositionsByColor = new List<List<Vector3>>();
            for (int i = 0; i < palette.Length; i++)
            {
                _matricesByColor.Add(new List<Matrix4x4>());
                _basePositionsByColor.Add(new List<Vector3>());
            }

            // 在四面看台生成观众
            BuildStandCrowd(StandSide.Left);     // 左侧看台
            BuildStandCrowd(StandSide.Right);    // 右侧看台
            BuildStandCrowd(StandSide.Back);     // 后侧看台（z-）
            BuildStandCrowd(StandSide.Front);    // 前侧看台（z+）

            _totalCrowdCount = 0;
            foreach (var list in _matricesByColor)
            {
                _totalCrowdCount += list.Count;
            }

            Debug.Log($"[CrowdController] 观众总数: {_totalCrowdCount}");
        }

        /// <summary>
        /// 清理观众数据
        /// </summary>
        private void ClearCrowd()
        {
            if (_crowdMaterials != null)
            {
                foreach (var mat in _crowdMaterials)
                {
                    if (mat != null) Object.Destroy(mat);
                }
                _crowdMaterials.Clear();
            }
            _matricesByColor?.Clear();
            _basePositionsByColor?.Clear();
            _totalCrowdCount = 0;
        }

        /// <summary>
        /// 获取共享的 Cube 网格（避免重复创建）
        /// </summary>
        private Mesh GetSharedCubeMesh()
        {
            GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Mesh mesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(tempCube);
            return mesh;
        }

        /// <summary>
        /// 看台方位枚举
        /// </summary>
        private enum StandSide
        {
            Left,   // 左侧（x-）
            Right,  // 右侧（x+）
            Back,   // 后侧（z-）
            Front   // 前侧（z+）
        }

        /// <summary>
        /// 在单面看台上生成观众
        /// </summary>
        private void BuildStandCrowd(StandSide side)
        {
            float halfPitchW = StadiumBuilder.HalfWidth;
            float halfPitchL = StadiumBuilder.HalfLength;

            for (int row = 0; row < rowsPerStand; row++)
            {
                for (int col = 0; col < colsPerStand; col++)
                {
                    // 计算观众位置
                    Vector3 pos = GetCrowdPosition(side, row, col, halfPitchW, halfPitchL);

                    // 随机选择颜色索引
                    int colorIndex = Random.Range(0, _crowdMaterials.Count);

                    // 添加随机偏移使排列更自然
                    pos.x += Random.Range(-0.1f, 0.1f);
                    pos.z += Random.Range(-0.1f, 0.1f);

                    // 构建变换矩阵
                    Quaternion rot = GetCrowdRotation(side);
                    Vector3 scale = crowdSize;
                    // 随机缩放变化
                    scale.y *= Random.Range(0.85f, 1.15f);

                    Matrix4x4 matrix = Matrix4x4.TRS(pos, rot, scale);

                    _matricesByColor[colorIndex].Add(matrix);
                    _basePositionsByColor[colorIndex].Add(pos);
                }
            }
        }

        /// <summary>
        /// 计算观众在看台上的位置
        /// </summary>
        private Vector3 GetCrowdPosition(StandSide side, int row, int col,
            float halfPitchW, float halfPitchL)
        {
            float rowHeight = (row + 1) * RowHeightStep;
            float rowDepth = StandGap + (row + 1) * RowDepthStep;

            switch (side)
            {
                case StandSide.Left:
                {
                    // 左侧看台：x = -34 - rowDepth，沿 z 轴排列
                    float x = -halfPitchW - rowDepth;
                    float z = -halfPitchL + (col + 0.5f) * (StadiumBuilder.PitchLength / colsPerStand);
                    return new Vector3(x, rowHeight, z);
                }
                case StandSide.Right:
                {
                    // 右侧看台：x = 34 + rowDepth，沿 z 轴排列
                    float x = halfPitchW + rowDepth;
                    float z = -halfPitchL + (col + 0.5f) * (StadiumBuilder.PitchLength / colsPerStand);
                    return new Vector3(x, rowHeight, z);
                }
                case StandSide.Back:
                {
                    // 后侧看台：z = -52.5 - rowDepth，沿 x 轴排列
                    float z = -halfPitchL - rowDepth;
                    float x = -halfPitchW + (col + 0.5f) * (StadiumBuilder.PitchWidth / colsPerStand);
                    return new Vector3(x, rowHeight, z);
                }
                case StandSide.Front:
                {
                    // 前侧看台：z = 52.5 + rowDepth，沿 x 轴排列
                    float z = halfPitchL + rowDepth;
                    float x = -halfPitchW + (col + 0.5f) * (StadiumBuilder.PitchWidth / colsPerStand);
                    return new Vector3(x, rowHeight, z);
                }
                default:
                    return Vector3.zero;
            }
        }

        /// <summary>
        /// 获取观众朝向（面向球场中心）
        /// </summary>
        private Quaternion GetCrowdRotation(StandSide side)
        {
            switch (side)
            {
                case StandSide.Left:
                    return Quaternion.Euler(0f, 90f, 0f);   // 朝向 +x
                case StandSide.Right:
                    return Quaternion.Euler(0f, -90f, 0f);  // 朝向 -x
                case StandSide.Back:
                    return Quaternion.Euler(0f, 0f, 0f);    // 朝向 +z
                case StandSide.Front:
                    return Quaternion.Euler(0f, 180f, 0f);  // 朝向 -z
                default:
                    return Quaternion.identity;
            }
        }

        /// <summary>
        /// 每帧渲染观众（使用 GPU 实例化）
        /// 按颜色分组，每组最多 1023 个实例
        /// </summary>
        private void Update()
        {
            if (_crowdMesh == null || _crowdMaterials == null || _matricesByColor == null)
            {
                return;
            }

            float time = Time.time;

            // 按颜色分组渲染
            for (int colorIdx = 0; colorIdx < _crowdMaterials.Count; colorIdx++)
            {
                List<Matrix4x4> matrices = _matricesByColor[colorIdx];
                if (matrices.Count == 0) continue;

                Material mat = _crowdMaterials[colorIdx];
                List<Vector3> basePositions = _basePositionsByColor[colorIdx];

                // 分批渲染（每批最多 1023 个）
                int batchCount = Mathf.CeilToInt(matrices.Count / (float)MaxInstancesPerDraw);

                for (int batch = 0; batch < batchCount; batch++)
                {
                    int startIndex = batch * MaxInstancesPerDraw;
                    int count = Mathf.Min(MaxInstancesPerDraw, matrices.Count - startIndex);

                    // 构建当前批次的矩阵数组
                    Matrix4x4[] batchMatrices = new Matrix4x4[count];

                    if (enableWaveAnimation)
                    {
                        // 启用波动动画：根据位置和时间计算 Y 偏移
                        for (int i = 0; i < count; i++)
                        {
                            int globalIdx = startIndex + i;
                            Vector3 basePos = basePositions[globalIdx];

                            // 计算波动偏移（基于位置和时间的正弦波）
                            float phase = basePos.x * 0.3f + basePos.z * 0.3f + time * waveSpeed;
                            float yOffset = Mathf.Sin(phase) * waveAmplitude;

                            // 重建矩阵
                            Vector3 newPos = new Vector3(basePos.x, basePos.y + yOffset, basePos.z);
                            // 从原矩阵提取旋转和缩放
                            Quaternion rot = Quaternion.LookRotation(
                                matrices[globalIdx].GetColumn(2),
                                matrices[globalIdx].GetColumn(1));
                            Vector3 scale = new Vector3(
                                matrices[globalIdx].GetColumn(0).magnitude,
                                matrices[globalIdx].GetColumn(1).magnitude,
                                matrices[globalIdx].GetColumn(2).magnitude);

                            batchMatrices[i] = Matrix4x4.TRS(newPos, rot, scale);
                        }
                    }
                    else
                    {
                        // 不启用动画：直接复制矩阵
                        for (int i = 0; i < count; i++)
                        {
                            batchMatrices[i] = matrices[startIndex + i];
                        }
                    }

                    // 绘制实例
                    Graphics.DrawMeshInstanced(_crowdMesh, 0, mat, batchMatrices);
                }
            }
        }

        /// <summary>
        /// 获取当前观众总数
        /// </summary>
        public int GetCrowdCount()
        {
            return _totalCrowdCount;
        }

        /// <summary>
        /// 启用/禁用波动动画
        /// </summary>
        public void SetWaveAnimation(bool enabled)
        {
            enableWaveAnimation = enabled;
        }

        /// <summary>
        /// 清理资源（对象销毁时由 Unity 自动调用）
        /// </summary>
        private void OnDestroy()
        {
            ClearCrowd();
        }
    }
}
