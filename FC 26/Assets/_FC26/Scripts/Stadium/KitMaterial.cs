using UnityEngine;
using FC26.Data;

namespace FC26.Stadium
{
    /// <summary>
    /// 球衣材质工具类：静态方法集合，用于生成球队球衣材质。
    /// 支持 URP/Lit 材质生成、球衣颜色冲突检测（HSV 距离）、批量生成主/客/门将球衣。
    /// 全部方法为静态，无需实例化。
    /// </summary>
    public static class KitMaterial
    {
        // ===== 配置常量 =====
        // 条纹纹理分辨率（像素）
        private const int TextureSize = 256;
        // 竖条纹数量
        private const int StripeCount = 8;
        // 颜色冲突判定阈值（HSV 距离小于此值视为冲突）
        private const float ConflictThreshold = 0.15f;
        // 低饱和度判定阈值（低于此值视为灰色，色相无意义）
        private const float LowSaturationThreshold = 0.15f;

        /// <summary>
        /// 创建球衣材质（URP/Lit，竖条纹纹理）。
        /// primary 为主色，secondary 为副色，交替形成竖条纹。
        /// 若两色相同则为纯色材质。
        /// </summary>
        /// <param name="primary">主色</param>
        /// <param name="secondary">副色</param>
        /// <returns>URP/Lit 材质</returns>
        public static Material CreateKitMaterial(Color primary, Color secondary)
        {
            // 创建竖条纹纹理
            Texture2D tex = CreateStripedTexture(primary, secondary);

            // 创建 URP/Lit 材质并赋值
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.name = "KitMaterial";
            mat.SetTexture("_BaseMap", tex);
            mat.color = Color.white;
            return mat;
        }

        /// <summary>
        /// 检测两支球队的球衣颜色是否冲突（HSV 距离过小）。
        /// 返回 true 表示颜色过于相近，需要更换球衣。
        /// HSV 距离计算：色相差权重 0.6，饱和度差权重 0.2，明度差权重 0.2。
        /// 当两色都接近灰色（低饱和度）时，仅比较明度。
        /// </summary>
        /// <param name="home">主队球衣颜色</param>
        /// <param name="away">客队球衣颜色</param>
        /// <returns>true=冲突，false=不冲突</returns>
        public static bool CheckColorConflict(Color home, Color away)
        {
            // RGB 转 HSV（Unity 内置，h/s/v 均为 0~1）
            Color.RGBToHSV(home, out float h1, out float s1, out float v1);
            Color.RGBToHSV(away, out float h2, out float s2, out float v2);

            // 色相差（环形，0~0.5）
            float hueDiff = Mathf.Abs(h1 - h2);
            hueDiff = Mathf.Min(hueDiff, 1f - hueDiff);

            // 饱和度差（0~1）
            float satDiff = Mathf.Abs(s1 - s2);
            // 明度差（0~1）
            float valDiff = Mathf.Abs(v1 - v2);

            float distance;
            if (s1 < LowSaturationThreshold && s2 < LowSaturationThreshold)
            {
                // 两色都接近灰色：色相无意义，仅比较明度
                distance = valDiff;
            }
            else
            {
                // 加权 HSV 距离（色相权重最大，符合人眼对色相敏感的特性）
                distance = Mathf.Sqrt(
                    hueDiff * hueDiff * 0.6f +
                    satDiff * satDiff * 0.2f +
                    valDiff * valDiff * 0.2f);
            }

            return distance < ConflictThreshold;
        }

        /// <summary>
        /// 为主/客/门将生成球衣材质。
        /// 返回数组顺序：[0]主队主场衣、[1]客队客场衣、[2]主队门将衣、[3]客队门将衣。
        /// 若主队主场色与客队主场色冲突，客队改用其客场色。
        /// </summary>
        /// <param name="home">主队数据</param>
        /// <param name="away">客队数据</param>
        /// <returns>4 个球衣材质</returns>
        public static Material[] GenerateKits(TeamData home, TeamData away)
        {
            Material[] kits = new Material[4];

            // [0] 主队主场衣：使用主队主场色，副色取深色变体形成条纹
            kits[0] = CreateKitMaterial(home.HomeColor, Darken(home.HomeColor));

            // [1] 客队客场衣：默认使用客队主场色，若与主队冲突则改用客队客场色
            Color awayColor = away.HomeColor;
            if (CheckColorConflict(home.HomeColor, away.HomeColor))
            {
                awayColor = away.AwayColor;
            }
            kits[1] = CreateKitMaterial(awayColor, Darken(awayColor));

            // [2] 主队门将衣：使用主队门将色
            kits[2] = CreateKitMaterial(home.GKColor, Darken(home.GKColor));

            // [3] 客队门将衣：使用客队门将色
            kits[3] = CreateKitMaterial(away.GKColor, Darken(away.GKColor));

            return kits;
        }

        // ====================================================================
        #region 内部工具方法

        /// <summary>
        /// 创建竖条纹 Texture2D。
        /// 沿 x 轴交替排列 primary 和 secondary，形成竖条纹效果。
        /// </summary>
        /// <param name="primary">主色</param>
        /// <param name="secondary">副色</param>
        /// <returns>条纹纹理</returns>
        private static Texture2D CreateStripedTexture(Color primary, Color secondary)
        {
            Texture2D tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGB24, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            int stripeWidth = TextureSize / StripeCount;
            Color[] pixels = new Color[TextureSize * TextureSize];

            // 沿 x 轴交替条纹（竖条纹）
            for (int x = 0; x < TextureSize; x++)
            {
                Color c = ((x / stripeWidth) % 2 == 0) ? primary : secondary;
                for (int y = 0; y < TextureSize; y++)
                {
                    pixels[y * TextureSize + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// 将颜色变暗（用于生成副色条纹，增加球衣层次感）。
        /// </summary>
        /// <param name="c">原始颜色</param>
        /// <param name="factor">变暗系数（0~1，默认 0.7）</param>
        /// <returns>变暗后的颜色</returns>
        private static Color Darken(Color c, float factor = 0.7f)
        {
            return new Color(c.r * factor, c.g * factor, c.b * factor, c.a);
        }

        #endregion
    }
}
