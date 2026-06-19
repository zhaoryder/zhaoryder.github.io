//=============================================================================
// 文件名：ProjectSetupNote.cs
// 所属模块：Settings
// 命名空间：FC26.Settings
// 作用：注释型脚本（不参与运行时逻辑），说明 FC 26 工程的 URP 资产、Quality、
//       Tag/Layer 配置要求。开发者首次打开工程时按本文件说明配置即可。
// 备注：本脚本不包含任何可执行逻辑，仅作为工程配置备忘。
//       使用 EditorOnly 标签或 #if UNITY_EDITOR 包裹可避免打包进发布版本。
//=============================================================================
#if UNITY_EDITOR
using UnityEngine;

namespace FC26.Settings
{
    /// <summary>
    /// 工程配置说明（注释型脚本）。
    /// 首次打开工程时，按本类内注释逐项配置 Unity 工程。
    /// </summary>
    public class ProjectSetupNote : MonoBehaviour
    {
        //================================================================================
        // 一、Unity 版本要求
        //================================================================================
        // - Unity 2022 LTS（推荐 2022.3.x 最新补丁版本）
        // - 渲染管线：Universal Render Pipeline (URP) 3D
        // - 颜色空间：Linear（Edit > Project Settings > Player > Color Space = Linear）
        // - API 兼容级别：.NET Standard 2.1 或 .NET 4.x
        //================================================================================


        //================================================================================
        // 二、URP 资产配置
        //================================================================================
        // 1. 安装 URP 包：Window > Package Manager > Universal RP（推荐 14.x 版本）
        // 2. 创建 URP Asset：
        //    - 在 Assets/_FC26/Settings 下右键 > Create > Rendering > URP Asset (with Universal Renderer)
        //    - 命名为 FC26_URPAsset
        // 3. 将该 URP Asset 拖入 Project Settings > Graphics > Scriptable Render Pipeline Settings
        // 4. URP Asset 推荐参数（PC 端 60FPS 目标）：
        //    - Rendering Path: Forward
        //    - Depth Texture: On
        //    - Opaque Texture: Off
        //    - MSAA: 4x
        //    - HDR: On
        //    - Post Processing: On
        //    - Shadow Distance: 80
        //    - Cascade Count: 2
        //    - Shadow Atlas: 2048
        // 5. Universal Renderer 资产：
        //    - Filtering > Opaque Layer Mask: Everything
        //    - Filtering > Transparent Layer Mask: Everything
        //    - Shadows > Transparent: Off
        //================================================================================


        //================================================================================
        // 三、Quality 设置
        //================================================================================
        // Edit > Project Settings > Quality
        // - 删除多余 Quality Level，仅保留 "Medium" 与 "High"（PC 端）
        // - Medium：用于低端 PC，关闭后处理、阴影距离 50
        // - High：默认等级，开启后处理、阴影距离 80、MSAA 4x
        // - VSync Count: Don't Sync（由 Application.targetFrameRate = 60 控制）
        //================================================================================


        //================================================================================
        // 四、Layer 配置（必须包含以下 Layer，索引可调整但建议按此顺序）
        //================================================================================
        // Edit > Project Settings > Tags and Layers
        // Built-in Layer 0~7 保留默认。
        // User Layer（从 8 开始）配置如下：
        //   8  : Field       （草坪、标线、球门等场地物体）
        //   9  : Player      （球员）
        //   10 : Ball        （足球）
        //   11 : Goal        （球门触发器，用于进球判定）
        //   12 : UI          （UI 元素，Canvas 独立 EventCamera）
        //   13 : Referee     （裁判 NPC）
        //   14 : Crowd       （观众 NPC）
        //   15 : Prop        （场边道具）
        //
        // 注意：Layer 索引一旦在工程中确定，不要随意更改，否则会影响物理碰撞矩阵与摄像机 Culling Mask。
        //================================================================================


        //================================================================================
        // 五、Tag 配置
        //================================================================================
        // Edit > Project Settings > Tags and Layers > Tags
        // 建议添加以下 Tag：
        //   - GameManager     （挂载 GameManager 的 GameObject）
        //   - Bootstrapper    （挂载 SceneBootstrapper 的 GameObject）
        //   - MainCamera      （主摄像机）
        //   - Ball            （足球）
        //   - Player_Home     （主队球员根节点）
        //   - Player_Away     （客队球员根节点）
        //   - Goal_Home       （主队球门）
        //   - Goal_Away       （客队球门）
        //================================================================================


        //================================================================================
        // 六、物理碰撞矩阵（Physics Layer Collision Matrix）
        //================================================================================
        // Edit > Project Settings > Physics > Layer Collision Matrix
        // 推荐配置（取消勾选表示不碰撞）：
        //   - Field  vs Field   : 取消（场地物体互不碰撞）
        //   - Field  vs Player  : 勾选（球员踩在草地上，但通常用射线检测而非物理碰撞）
        //   - Field  vs Ball    : 勾选（球与地面反弹）
        //   - Player vs Player  : 勾选（球员间铲断/拼抢碰撞）
        //   - Player vs Ball    : 勾选（球员触球）
        //   - Ball   vs Goal    : 勾选（进球判定，Goal 为触发器）
        //   - UI     vs *       : 全部取消（UI 不参与物理）
        //   - Crowd  vs *       : 全部取消（观众不参与物理）
        //================================================================================


        //================================================================================
        // 七、输入系统配置
        //================================================================================
        // 本工程使用 Unity 旧版 Input Manager（Input.GetAxis 等），不依赖 Input System 包。
        // 若已安装 Input System 包，请在 Player Settings > Active Input Handling 选择 "Both"。
        // 键位说明详见 RunInstructions.cs。
        //================================================================================


        //================================================================================
        // 八、场景配置
        //================================================================================
        // 1. 在 Assets/_FC26/Scenes 下创建场景：
        //    - Boot.unity      （启动场景，仅挂载 GameManager，加载后跳转 Match）
        //    - Match.unity     （比赛场景，挂载 SceneBootstrapper 与 GameManager）
        // 2. Build Settings 中将 Boot 场景拖到 Scenes In Build 列表第 0 位。
        // 3. Match 场景中需挂载以下组件：
        //    - GameManager (MonoSingleton<GameManager>)
        //    - SceneBootstrapper
        //    - EventSystem（UI 交互必需）
        //================================================================================


        //================================================================================
        // 九、其他建议
        //================================================================================
        // - 在 Project Settings > Player > Resolution and Presentation:
        //     Fullscreen Mode = Windowed，默认分辨率 1920x1080
        // - Application.targetFrameRate = 60（在 GameManager 或 Boot 脚本中设置）
        // - Time.fixedDeltaTime = 0.01667（60Hz 物理），Edit > Project Settings > Time
        // - 草地材质使用 URP/Lit Shader，启用 GPU Instancing
        //================================================================================
    }
}
#endif
