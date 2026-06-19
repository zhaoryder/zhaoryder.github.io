//=============================================================================
// 文件名：GameManager.cs
// 所属模块：Core
// 命名空间：FC26.Core
// 作用：游戏主循环入口。继承 MonoSingleton<GameManager>，全局唯一。
//       Awake 阶段初始化各系统（事件总线、服务定位器、状态机等）；
//       FixedUpdate 驱动物理（状态机.OnFixedUpdate）；
//       Update 驱动输入与 AI（状态机.OnUpdate），暂停时停止游戏逻辑但 UI 仍响应。
//       持有 GameStateMachine 引用，对外提供 IsPaused 属性与暂停切换方法。
// 备注：本脚本需挂载在场景中的 GameManager GameObject 上。
//       各模块的状态实现通过 RegisterState 在 Awake 中注册（此处仅注册占位说明，
//       实际状态实现由各模块自行注册到 GameManager.Instance.StateMachine）。
//=============================================================================
using UnityEngine;

namespace FC26.Core
{
    /// <summary>
    /// 游戏主循环入口，全局唯一单例。
    /// 负责初始化系统、驱动状态机、管理暂停。
    /// </summary>
    public class GameManager : MonoSingleton<GameManager>
    {
        /// <summary>
        /// 游戏状态机实例。各模块通过此属性注册状态、查询当前状态。
        /// </summary>
        public GameStateMachine StateMachine { get; private set; }

        /// <summary>
        /// 是否处于暂停状态。暂停时 Update 中的游戏逻辑停止，但 UI 仍响应。
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// 比赛是否正在进行（Playing 状态且未暂停）。
        /// 供其他模块快速判断是否需要驱动 AI、物理等。
        /// </summary>
        public bool IsPlaying => !IsPaused && StateMachine != null
            && StateMachine.CurrentStateEnum == GameState.Playing;

        [Header("初始状态")]
        [Tooltip("游戏启动后进入的第一个状态。默认 MainMenu。")]
        [SerializeField] private GameState _initialState = GameState.MainMenu;

        /// <summary>
        /// Awake 初始化各系统。子类不应重写 Awake；如需扩展请重写 InitSystems。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            // 创建状态机。
            StateMachine = new GameStateMachine();

            // 初始化各系统（事件总线、服务定位器、状态注册等）。
            InitSystems();

            // 启动状态机，进入初始状态。
            StateMachine.Start(_initialState);

            Debug.Log("[GameManager] 初始化完成，进入状态: " + _initialState);
        }

        /// <summary>
        /// 初始化各系统。子类可重写以追加自定义初始化逻辑（注意调用 base.InitSystems）。
        /// </summary>
        protected virtual void InitSystems()
        {
            // 此处可注册核心服务、加载配置等。
            // 各功能模块（Player/Ball/AI/UI 等）的状态实现由各自 ModuleInitializer
            // 在 Awake 阶段调用 GameManager.Instance.StateMachine.RegisterState 注册。
            //
            // 示例（伪代码）：
            //   StateMachine.RegisterState(GameState.MainMenu, () => new MainMenuState());
            //   StateMachine.RegisterState(GameState.Playing,  () => new PlayingState());
            //
            // 为避免 Core 模块对其他模块的硬依赖，此处不直接 new 具体状态，
            // 由各模块自行注册。若某状态未注册，状态机会以空状态占位并告警。
        }

        /// <summary>
        /// FixedUpdate 驱动物理。仅在非暂停时分发到状态机。
        /// </summary>
        private void FixedUpdate()
        {
            if (IsPaused)
            {
                return;
            }

            StateMachine?.FixedUpdate();
        }

        /// <summary>
        /// Update 驱动输入与 AI。仅在非暂停时分发到状态机。
        /// 注意：UI 系统不依赖状态机分发，由 EventSystem 独立响应，因此暂停时 UI 仍可用。
        /// </summary>
        private void Update()
        {
            if (IsPaused)
            {
                return;
            }

            StateMachine?.Update();
        }

        /// <summary>
        /// 切换暂停状态。会广播 PauseToggledEvent，并切换 GameStateMachine 在
        /// Playing <-> Paused 之间的状态。
        /// </summary>
        /// <param name="paused">true=暂停，false=恢复</param>
        public void SetPaused(bool paused)
        {
            if (IsPaused == paused)
            {
                return;
            }

            IsPaused = paused;

            // 在 Playing 与 Paused 之间切换状态机，便于 UI/音频模块响应。
            if (paused)
            {
                if (StateMachine.CurrentStateEnum == GameState.Playing)
                {
                    StateMachine.SwitchTo(GameState.Paused);
                }
            }
            else
            {
                if (StateMachine.CurrentStateEnum == GameState.Paused)
                {
                    StateMachine.SwitchTo(GameState.Playing);
                }
            }

            // 广播暂停事件。
            EventBus.Publish(new PauseToggledEvent { Paused = paused });

            Debug.Log($"[GameManager] 暂停状态切换: {paused}");
        }

        /// <summary>
        /// 切换暂停（便捷方法，供输入模块按 Esc 调用）。
        /// </summary>
        public void TogglePause()
        {
            SetPaused(!IsPaused);
        }

        /// <summary>
        /// 应用退出时清理事件总线与服务定位器，避免内存泄漏。
        /// </summary>
        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();

            EventBus.Clear();
            ServiceLocator.Clear();
        }
    }
}
