//=============================================================================
// 文件名：UIBase.cs
// 所属模块：UI
// 命名空间：FC26.UI
// 作用：UI 面板基类。所有具体面板（MainMenuUI/TeamSelectUI/LineupUI/MatchHUD/
//       PostMatchPanel/PauseMenuUI）均继承本类。
//       提供 Show()/Hide() 显隐控制、IsVisible 状态查询、Build() 构建入口。
//       子类重写 Build() 在运行时通过 UGUI API 创建控件（不依赖预制体）。
// 备注：本类为抽象基类，不可直接挂载。子类需挂载到场景 GameObject 上，
//       由 UIManager.Push 统一管理生命周期。
//=============================================================================
using UnityEngine;

namespace FC26.UI
{
    /// <summary>
    /// UI 面板基类。提供显隐控制与构建入口。
    /// 子类重写 Build() 完成具体面板的运行时构建。
    /// </summary>
    public abstract class UIBase : MonoBehaviour
    {
        /// <summary>面板根节点 RectTransform（Build 时创建）。</summary>
        public RectTransform Root { get; protected set; }

        /// <summary>面板是否可见。</summary>
        public bool IsVisible { get; private set; }

        /// <summary>是否已构建完成（避免重复构建）。</summary>
        public bool IsBuilt { get; private set; }

        /// <summary>
        /// Awake：初始状态为不可见。
        /// </summary>
        protected virtual void Awake()
        {
            IsVisible = false;
        }

        /// <summary>
        /// 构建面板。子类重写以通过 UGUI API 创建控件。
        /// 调用前需确保 UIManager.MainCanvas 已存在。
        /// 重复调用会被忽略（已构建则直接返回）。
        /// </summary>
        public virtual void Build()
        {
            if (IsBuilt)
            {
                return;
            }

            // 创建面板根节点：挂在 UIManager 主 Canvas 下。
            // 使用类型名作为根节点名，便于 Hierarchy 窗口调试定位。
            Root = UIManager.CreatePanelRoot(GetType().Name + "_Root");

            IsBuilt = true;
            Debug.Log($"[UIBase] {GetType().Name} 面板构建完成。");
        }

        /// <summary>
        /// 显示面板。激活根节点并标记可见。
        /// 若尚未构建，先调用 Build()。
        /// </summary>
        public virtual void Show()
        {
            if (!IsBuilt)
            {
                Build();
            }

            if (Root != null)
            {
                Root.gameObject.SetActive(true);
            }

            IsVisible = true;
        }

        /// <summary>
        /// 隐藏面板。禁用根节点并标记不可见。
        /// 不销毁对象，便于再次 Show。
        /// </summary>
        public virtual void Hide()
        {
            if (Root != null)
            {
                Root.gameObject.SetActive(false);
            }

            IsVisible = false;
        }

        /// <summary>
        /// 销毁面板：销毁根节点 GameObject，重置状态。
        /// 用于彻底移除面板（如返回主菜单时清理子面板）。
        /// </summary>
        public virtual void Dispose()
        {
            if (Root != null)
            {
                Destroy(Root.gameObject);
                Root = null;
            }

            IsBuilt = false;
            IsVisible = false;
        }
    }
}
