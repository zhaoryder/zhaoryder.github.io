//=============================================================================
// 文件名：SceneBootstrapper.cs
// 所属模块：Core
// 命名空间：FC26.Core
// 作用：场景装配器。挂在场景根节点上，Awake 时按顺序调用各模块 Builder 在运行时
//       构建球场、球员、UI、摄像机等（不依赖预制体）。
//       通过反射或服务定位器获取各 Builder，缺失模块时 try-catch 包裹避免崩溃。
// 依赖说明（运行时由其他模块提供，缺失时仅告警不崩溃）：
//   - StadiumBuilder      （Stadium 模块）  构建球场草坪、看台、球门
//   - CrowdController     （Stadium 模块）  构建观众 NPC
//   - PlayerFactory       （Player 模块）   构建两队球员
//   - UIManager           （UI 模块）       构建 HUD 与菜单
//   - CameraRig           （Camera 模块）   构建比赛摄像机
//   - BallController      （Ball 模块）     构建足球
//   - RefereeController   （Referee 模块）  构建裁判 NPC
// 备注：本脚本不直接引用上述类型（避免 Core 模块对功能模块的硬依赖），
//       而是通过反射按类型名查找。各模块需将自身 Builder 注册为同名 MonoBehaviour
//       并挂载到场景中，或通过 ServiceLocator 暴露接口。
//=============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FC26.Core
{
    /// <summary>
    /// 场景装配器。运行时按顺序构建场景内容，不依赖预制体。
    /// </summary>
    public class SceneBootstrapper : MonoBehaviour
    {
        [Header("构建选项")]
        [Tooltip("是否在 Awake 时自动构建场景。若关闭，需手动调用 BuildAll。")]
        [SerializeField] private bool _autoBuildOnAwake = true;

        [Tooltip("构建失败时是否抛出异常中断流程。默认 false，仅记录错误日志。")]
        [SerializeField] private bool _throwOnBuildError = false;

        /// <summary>
        /// 已构建的根节点字典，便于后续模块查询与清理。
        /// 键为构建步骤名（如 "Stadium"、"Players"），值为对应根节点。
        /// </summary>
        private readonly Dictionary<string, GameObject> _builtRoots = new Dictionary<string, GameObject>();

        /// <summary>
        /// Awake 入口。若开启自动构建，则按顺序构建场景。
        /// </summary>
        private void Awake()
        {
            if (_autoBuildOnAwake)
            {
                BuildAll();
            }
        }

        /// <summary>
        /// 按顺序构建场景全部内容。每步独立 try-catch，单步失败不阻断后续。
        /// </summary>
        public void BuildAll()
        {
            Debug.Log("[SceneBootstrapper] 开始构建场景...");

            // 构建顺序：球场 -> 灯光 -> 摄像机 -> 球员 -> 足球 -> 裁判 -> 观众 -> UI
            // 该顺序保证后续模块能引用到前置对象（如球员需要球场坐标）。
            BuildStep("Stadium", BuildStadium);
            BuildStep("Lighting", BuildLighting);
            BuildStep("Camera", BuildCamera);
            BuildStep("Players", BuildPlayers);
            BuildStep("Ball", BuildBall);
            BuildStep("Referee", BuildReferee);
            BuildStep("Crowd", BuildCrowd);
            BuildStep("UI", BuildUI);

            // 通知 GameManager 场景已就绪（若存在）。
            // 注意：GameManager 可能在场景加载前已初始化，此处仅做就绪广播。
            if (GameManager.Instance != null)
            {
                Debug.Log("[SceneBootstrapper] 场景构建完成，GameManager 已就绪。");
            }
            else
            {
                Debug.LogWarning("[SceneBootstrapper] 场景构建完成，但 GameManager 未找到。请确保场景中挂载了 GameManager。");
            }
        }

        /// <summary>
        /// 通用构建步骤封装。统一异常处理与日志。
        /// </summary>
        /// <param name="stepName">步骤名（用于日志与字典键）</param>
        /// <param name="buildAction">构建动作，返回该步骤的根节点（可为 null）</param>
        private void BuildStep(string stepName, Func<GameObject> buildAction)
        {
            try
            {
                GameObject root = buildAction();
                if (root != null)
                {
                    _builtRoots[stepName] = root;
                }

                Debug.Log($"[SceneBootstrapper] 步骤 {stepName} 构建完成。");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SceneBootstrapper] 步骤 {stepName} 构建失败: {e}");

                if (_throwOnBuildError)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// 构建球场。依赖 Stadium 模块的 StadiumBuilder。
        /// 若该模块尚未实现，则创建一个占位平面，保证场景可运行。
        /// </summary>
        private GameObject BuildStadium()
        {
            GameObject root = new GameObject("Stadium_Root");

            // 尝试通过反射调用 StadiumBuilder.Build()（若该类型存在）。
            // 类型名约定：FC26.Stadium.StadiumBuilder
            if (TryInvokeBuilder("FC26.Stadium.StadiumBuilder", "Build", root))
            {
                return root;
            }

            // 占位实现：创建一个绿色平面作为球场，便于早期阶段验证场景。
            Debug.LogWarning("[SceneBootstrapper] StadiumBuilder 未找到，使用占位平面。请后续实现 Stadium 模块。");
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Placeholder_Field";
            plane.transform.SetParent(root.transform, false);
            plane.transform.localScale = new Vector3(10f, 1f, 6f); // 约 100m x 60m
            // 占位绿色材质。
            var renderer = plane.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = new Color(0.18f, 0.55f, 0.18f)
                };
            }

            return root;
        }

        /// <summary>
        /// 构建场景灯光。URP 推荐使用 Directional Light + 反射探针。
        /// 此处创建一盏方向光作为占位。
        /// </summary>
        private GameObject BuildLighting()
        {
            GameObject root = new GameObject("Lighting_Root");

            GameObject lightObj = new GameObject("Main_DirectionalLight");
            lightObj.transform.SetParent(root.transform, false);
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.96f, 0.86f);
            light.shadows = LightShadows.Soft;

            return root;
        }

        /// <summary>
        /// 构建摄像机。依赖 Camera 模块的 CameraRig。
        /// 若未实现，则创建一个俯瞰球场的占位摄像机。
        /// </summary>
        private GameObject BuildCamera()
        {
            GameObject root = new GameObject("Camera_Root");

            if (TryInvokeBuilder("FC26.Camera.CameraRig", "Build", root))
            {
                return root;
            }

            Debug.LogWarning("[SceneBootstrapper] CameraRig 未找到，使用占位摄像机。请后续实现 Camera 模块。");
            GameObject camObj = new GameObject("Main_Camera");
            camObj.transform.SetParent(root.transform, false);
            camObj.transform.position = new Vector3(0f, 30f, -25f);
            camObj.transform.rotation = Quaternion.Euler(50f, 0f, 0f);

            Camera cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.18f, 0.22f, 0.28f);

            return root;
        }

        /// <summary>
        /// 构建两队球员。依赖 Player 模块的 PlayerFactory。
        /// </summary>
        private GameObject BuildPlayers()
        {
            GameObject root = new GameObject("Players_Root");

            if (TryInvokeBuilder("FC26.Player.PlayerFactory", "BuildAll", root))
            {
                return root;
            }

            Debug.LogWarning("[SceneBootstrapper] PlayerFactory 未找到，跳过球员构建。请后续实现 Player 模块。");
            return root;
        }

        /// <summary>
        /// 构建足球。依赖 Ball 模块的 BallController。
        /// </summary>
        private GameObject BuildBall()
        {
            GameObject root = new GameObject("Ball_Root");

            if (TryInvokeBuilder("FC26.Ball.BallController", "Build", root))
            {
                return root;
            }

            Debug.LogWarning("[SceneBootstrapper] BallController 未找到，使用占位足球。请后续实现 Ball 模块。");
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Placeholder_Ball";
            ball.transform.SetParent(root.transform, false);
            ball.transform.position = new Vector3(0f, 0.5f, 0f);
            ball.transform.localScale = Vector3.one * 0.22f; // 足球直径约 22cm

            var renderer = ball.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = Color.white
                };
            }

            return root;
        }

        /// <summary>
        /// 构建裁判 NPC。依赖 Referee 模块的 RefereeController。
        /// </summary>
        private GameObject BuildReferee()
        {
            GameObject root = new GameObject("Referee_Root");

            if (TryInvokeBuilder("FC26.Referee.RefereeController", "Build", root))
            {
                return root;
            }

            Debug.LogWarning("[SceneBootstrapper] RefereeController 未找到，跳过裁判构建。请后续实现 Referee 模块。");
            return root;
        }

        /// <summary>
        /// 构建观众 NPC。依赖 Stadium 模块的 CrowdController。
        /// </summary>
        private GameObject BuildCrowd()
        {
            GameObject root = new GameObject("Crowd_Root");

            if (TryInvokeBuilder("FC26.Stadium.CrowdController", "Build", root))
            {
                return root;
            }

            Debug.LogWarning("[SceneBootstrapper] CrowdController 未找到，跳过观众构建。请后续实现 Crowd 模块。");
            return root;
        }

        /// <summary>
        /// 构建 UI。依赖 UI 模块的 UIManager。
        /// </summary>
        private GameObject BuildUI()
        {
            GameObject root = new GameObject("UI_Root");

            if (TryInvokeBuilder("FC26.UI.UIManager", "Build", root))
            {
                return root;
            }

            Debug.LogWarning("[SceneBootstrapper] UIManager 未找到，跳过 UI 构建。请后续实现 UI 模块。");
            return root;
        }

        /// <summary>
        /// 通过反射尝试调用指定类型的静态/实例构建方法。
        /// 兼容以下方法签名（按优先级尝试）：
        ///   1. 实例方法 void Build(GameObject root)
        ///   2. 实例方法 void Build()            （无参，构建器自建根节点，本方法会尝试将其根节点挂到 root 下）
        ///   3. 实例方法 void BuildAll(GameObject root)
        ///   4. 静态方法 void Build(GameObject root)
        ///   5. 静态方法 void Build()
        /// 若类型不存在或方法不存在，返回 false（不抛异常）。
        /// </summary>
        /// <param name="typeName">类型全名（含命名空间）</param>
        /// <param name="methodName">方法名（Build 或 BuildAll）</param>
        /// <param name="root">构建根节点，传入构建方法</param>
        /// <returns>是否成功调用</returns>
        private bool TryInvokeBuilder(string typeName, string methodName, GameObject root)
        {
            try
            {
                Type type = Type.GetType(typeName);
                if (type == null)
                {
                    return false;
                }

                // 优先尝试实例方法（需先在场景中查找该类型实例）。
                var instance = FindObjectOfType(type);
                if (instance != null)
                {
                    // 1. 带 GameObject 参数的实例方法。
                    var methodWithParam = type.GetMethod(methodName, new[] { typeof(GameObject) });
                    if (methodWithParam != null)
                    {
                        methodWithParam.Invoke(instance, new object[] { root });
                        return true;
                    }

                    // 2. 无参实例方法（构建器自建根节点）。
                    var methodNoParam = type.GetMethod(methodName, Type.EmptyTypes);
                    if (methodNoParam != null)
                    {
                        methodNoParam.Invoke(instance, null);

                        // 尝试将构建器自建的根节点挂到传入的 root 下，统一层级管理。
                        // 约定构建器暴露 GetRoot() 方法返回其根节点 Transform。
                        TryReparentBuilderRoot(type, instance, root);
                        return true;
                    }
                }

                // 3. 静态方法（带 GameObject 参数）。
                var staticMethodWithParam = type.GetMethod(methodName, new[] { typeof(GameObject) });
                if (staticMethodWithParam != null && staticMethodWithParam.IsStatic)
                {
                    staticMethodWithParam.Invoke(null, new object[] { root });
                    return true;
                }

                // 4. 静态方法（无参）。
                var staticMethodNoParam = type.GetMethod(methodName, Type.EmptyTypes);
                if (staticMethodNoParam != null && staticMethodNoParam.IsStatic)
                {
                    staticMethodNoParam.Invoke(null, null);
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SceneBootstrapper] 调用 {typeName}.{methodName} 异常: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 尝试通过反射调用构建器的 GetRoot() 方法，将其返回的根节点挂到 parentRoot 下。
        /// 用于兼容无参 Build() 签名的构建器（如 StadiumBuilder）。
        /// 若构建器未暴露 GetRoot() 或返回 null，则静默跳过（构建器自身已挂载到场景）。
        /// </summary>
        /// <param name="builderType">构建器类型</param>
        /// <param name="builderInstance">构建器实例</param>
        /// <param name="parentRoot">期望挂载到的父节点</param>
        private void TryReparentBuilderRoot(Type builderType, object builderInstance, GameObject parentRoot)
        {
            try
            {
                var getRootMethod = builderType.GetMethod("GetRoot", Type.EmptyTypes);
                if (getRootMethod == null)
                {
                    return;
                }

                var builderRoot = getRootMethod.Invoke(builderInstance, null) as Transform;
                if (builderRoot != null && builderRoot != parentRoot.transform)
                {
                    builderRoot.SetParent(parentRoot.transform, false);
                }
            }
            catch
            {
                // 静默忽略：构建器可能未实现 GetRoot()，不影响主流程。
            }
        }

        /// <summary>
        /// 获取已构建的根节点。供其他模块查询。
        /// </summary>
        /// <param name="stepName">步骤名</param>
        /// <returns>根节点，不存在返回 null</returns>
        public GameObject GetBuiltRoot(string stepName)
        {
            _builtRoots.TryGetValue(stepName, out var root);
            return root;
        }
    }
}
