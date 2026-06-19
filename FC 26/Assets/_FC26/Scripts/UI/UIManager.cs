//=============================================================================
// 文件名：UIManager.cs
// 所属模块：UI
// 命名空间：FC26.UI
// 作用：UI 管理器单例。负责：
//       1. 运行时创建主 Canvas（ScreenSpaceOverlay）与 EventSystem；
//       2. 面板栈管理（Push/Pop），支持菜单层级切换；
//       3. 提供 UGUI 控件创建工具方法（CreateButton/CreateText/CreateImage），
//          统一处理 RectTransform 锚点、尺寸、颜色，供各面板 Build 调用；
//       4. 鼠标点击适配：Button.onClick 自动绑定回调。
// 依赖：UnityEngine.UI（UGUI）、UnityEngine.EventSystems。
// 备注：本类继承 MonoSingleton<UIManager>，需挂载到场景 GameObject 上。
//       SceneBootstrapper 通过反射调用 Build(GameObject) 触发初始化。
//=============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FC26.Core;

namespace FC26.UI
{
    /// <summary>
    /// UI 管理器单例。统一创建 Canvas、管理面板栈、提供 UGUI 工具方法。
    /// </summary>
    public class UIManager : MonoSingleton<UIManager>
    {
        /// <summary>主 Canvas（ScreenSpaceOverlay），所有面板挂在其下。</summary>
        public Canvas MainCanvas { get; private set; }

        /// <summary>主 Canvas 的 RectTransform，便于面板挂载。</summary>
        public RectTransform CanvasRect { get; private set; }

        /// <summary>面板栈：后进先出，栈顶为当前激活面板。</summary>
        private readonly Stack<UIBase> _panelStack = new Stack<UIBase>();

        /// <summary>已注册的面板实例缓存（按类型查找，避免重复创建）。</summary>
        private readonly Dictionary<Type, UIBase> _panelCache = new Dictionary<Type, UIBase>();

        /// <summary>UI 默认字体（运行时获取系统默认字体）。</summary>
        private Font _defaultFont;

        /// <summary>
        /// Awake：调用基类完成单例注册，并初始化主 Canvas。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateCanvas();
        }

        /// <summary>
        /// SceneBootstrapper 反射调用入口：触发 UI 构建。
        /// 创建主 Canvas 后，默认显示主菜单。
        /// </summary>
        /// <param name="root">构建根节点（本类自建 Canvas，参数仅用于兼容反射签名）</param>
        public void Build(GameObject root)
        {
            // Canvas 已在 Awake 创建，此处仅做就绪日志。
            Debug.Log("[UIManager] UI 系统就绪。");

            // 默认显示主菜单。
            ShowPanel<MainMenuUI>();
        }

        /// <summary>
        /// 运行时创建主 Canvas（ScreenSpaceOverlay）与 EventSystem。
        /// 若已存在则跳过。
        /// </summary>
        public void CreateCanvas()
        {
            if (MainCanvas != null)
            {
                return;
            }

            // ===== 创建主 Canvas =====
            GameObject canvasObj = new GameObject("MainCanvas");
            canvasObj.transform.SetParent(transform, false);

            MainCanvas = canvasObj.AddComponent<Canvas>();
            MainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            MainCanvas.sortingOrder = 0;

            // CanvasScaler：适配 PC 端 1920x1080 参考分辨率。
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // GraphicRaycaster：使 Canvas 内控件可接收鼠标点击。
            canvasObj.AddComponent<GraphicRaycaster>();

            CanvasRect = canvasObj.GetComponent<RectTransform>();

            // ===== 创建 EventSystem（若场景中不存在）=====
            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.transform.SetParent(transform, false);
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
            }

            Debug.Log("[UIManager] 主 Canvas 与 EventSystem 创建完成。");
        }

        /// <summary>
        /// 创建面板根节点 RectTransform（挂在主 Canvas 下）。
        /// 默认铺满父节点（Stretch），便于子控件用锚点定位。
        /// </summary>
        /// <param name="name">根节点名称</param>
        /// <returns>面板根 RectTransform</returns>
        public static RectTransform CreatePanelRoot(string name)
        {
            GameObject panelObj = new GameObject(name);
            panelObj.transform.SetParent(Instance.CanvasRect, false);

            RectTransform rt = panelObj.AddComponent<RectTransform>();
            // Stretch：铺满父节点
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // 默认隐藏，由 Show() 激活。
            panelObj.SetActive(false);

            return rt;
        }

        // ===== 面板栈管理 =====

        /// <summary>
        /// 压入面板并显示。栈顶面板自动隐藏。
        /// 若该类型面板已缓存，复用实例；否则创建新实例。
        /// </summary>
        /// <typeparam name="T">面板类型</typeparam>
        public void Push<T>() where T : UIBase
        {
            // 隐藏当前栈顶面板。
            if (_panelStack.Count > 0)
            {
                _panelStack.Peek().Hide();
            }

            UIBase panel = GetOrCreatePanel<T>();
            panel.Show();
            _panelStack.Push(panel);
        }

        /// <summary>
        /// 弹出栈顶面板并销毁/隐藏，显示下一层面板。
        /// </summary>
        public void Pop()
        {
            if (_panelStack.Count == 0)
            {
                return;
            }

            UIBase top = _panelStack.Pop();
            top.Hide();

            // 显示新的栈顶面板。
            if (_panelStack.Count > 0)
            {
                _panelStack.Peek().Show();
            }
        }

        /// <summary>
        /// 获取栈顶面板（当前激活面板）。
        /// </summary>
        /// <returns>栈顶面板，栈空返回 null</returns>
        public UIBase GetCurrentPanel()
        {
            return _panelStack.Count > 0 ? _panelStack.Peek() : null;
        }

        /// <summary>
        /// 清空面板栈（返回主菜单时调用）。
        /// </summary>
        public void ClearStack()
        {
            while (_panelStack.Count > 0)
            {
                _panelStack.Pop().Hide();
            }
        }

        /// <summary>
        /// 显示指定类型面板（清空栈后压入，作为新根面板）。
        /// 用于主菜单 -> 子菜单的顶层切换。
        /// </summary>
        /// <typeparam name="T">面板类型</typeparam>
        public void ShowPanel<T>() where T : UIBase
        {
            ClearStack();
            Push<T>();
        }

        /// <summary>
        /// 获取或创建指定类型面板实例。
        /// 创建时挂载到 UIManager 所在 GameObject 上。
        /// </summary>
        private UIBase GetOrCreatePanel<T>() where T : UIBase
        {
            Type type = typeof(T);
            if (_panelCache.TryGetValue(type, out var cached))
            {
                return cached;
            }

            // 运行时添加面板组件。
            UIBase panel = gameObject.AddComponent<T>();
            _panelCache[type] = panel;
            return panel;
        }

        // ===== UGUI 控件创建工具方法 =====

        /// <summary>
        /// 创建按钮（带 Text 子物体）。
        /// 鼠标点击通过 Button.onClick 绑定回调，自动适配 PC 端鼠标。
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="text">按钮文字</param>
        /// <param name="pos">锚点中心位置（相对父节点中心，像素）</param>
        /// <param name="onClick">点击回调</param>
        /// <returns>创建的 Button 组件</returns>
        public Button CreateButton(Transform parent, string text, Vector2 pos, Action onClick)
        {
            // 按钮根节点
            GameObject btnObj = new GameObject("Button_" + text);
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(280f, 60f); // 默认按钮尺寸

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.15f, 0.35f, 0.65f, 0.95f); // 深蓝色背景

            Button btn = btnObj.AddComponent<Button>();
            // 按钮过渡颜色：高亮/按下时变亮。
            var colors = btn.colors;
            colors.normalColor = new Color(0.15f, 0.35f, 0.65f, 0.95f);
            colors.highlightedColor = new Color(0.25f, 0.50f, 0.85f, 1f);
            colors.pressedColor = new Color(0.10f, 0.25f, 0.55f, 1f);
            colors.selectedColor = colors.highlightedColor;
            btn.colors = colors;

            // 按钮文字
            CreateText(btnObj.transform, text, Vector2.zero, 24).alignment = TextAnchor.MiddleCenter;

            // 绑定点击回调（鼠标点击适配）
            if (onClick != null)
            {
                btn.onClick.AddListener(() => onClick.Invoke());
            }

            return btn;
        }

        /// <summary>
        /// 创建文本控件。
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="content">文本内容</param>
        /// <param name="pos">锚点中心位置（像素）</param>
        /// <param name="fontSize">字号</param>
        /// <returns>创建的 Text 组件</returns>
        public Text CreateText(Transform parent, string content, Vector2 pos, int fontSize)
        {
            GameObject textObj = new GameObject("Text_" + content);
            textObj.transform.SetParent(parent, false);

            RectTransform rt = textObj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(400f, 50f);

            Text text = textObj.AddComponent<Text>();
            text.text = content;
            text.font = _defaultFont;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false; // 文本不拦截鼠标事件

            return text;
        }

        /// <summary>
        /// 创建图像控件（纯色块，用于背景、力度条、柱状图等）。
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="color">颜色</param>
        /// <param name="pos">锚点中心位置（像素）</param>
        /// <param name="size">尺寸（像素）</param>
        /// <returns>创建的 Image 组件</returns>
        public Image CreateImage(Transform parent, Color color, Vector2 pos, Vector2 size)
        {
            GameObject imgObj = new GameObject("Image");
            imgObj.transform.SetParent(parent, false);

            RectTransform rt = imgObj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = imgObj.AddComponent<Image>();
            img.color = color;

            return img;
        }

        /// <summary>
        /// 创建带自定义尺寸的按钮（用于球队选择网格等紧凑布局）。
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="text">按钮文字</param>
        /// <param name="pos">位置</param>
        /// <param name="size">尺寸</param>
        /// <param name="onClick">点击回调</param>
        /// <returns>创建的 Button 组件</returns>
        public Button CreateButton(Transform parent, string text, Vector2 pos, Vector2 size, Action onClick)
        {
            Button btn = CreateButton(parent, text, pos, onClick);
            btn.GetComponent<RectTransform>().sizeDelta = size;
            return btn;
        }

        /// <summary>
        /// 创建带背景的面板容器（半透明黑色遮罩 + 内容区）。
        /// 用于弹窗式面板（暂停菜单、赛后面板）。
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="name">容器名</param>
        /// <returns>容器 RectTransform</returns>
        public RectTransform CreatePanelContainer(Transform parent, string name)
        {
            // 全屏半透明遮罩
            GameObject maskObj = new GameObject(name + "_Mask");
            maskObj.transform.SetParent(parent, false);
            RectTransform maskRt = maskObj.AddComponent<RectTransform>();
            maskRt.anchorMin = Vector2.zero;
            maskRt.anchorMax = Vector2.one;
            maskRt.offsetMin = Vector2.zero;
            maskRt.offsetMax = Vector2.zero;
            Image maskImg = maskObj.AddComponent<Image>();
            maskImg.color = new Color(0f, 0f, 0f, 0.6f); // 半透明黑遮罩

            // 内容区（居中）
            GameObject contentObj = new GameObject(name + "_Content");
            contentObj.transform.SetParent(maskObj.transform, false);
            RectTransform contentRt = contentObj.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 0.5f);
            contentRt.anchorMax = new Vector2(0.5f, 0.5f);
            contentRt.pivot = new Vector2(0.5f, 0.5f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(800f, 600f);
            Image contentImg = contentObj.AddComponent<Image>();
            contentImg.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

            return contentRt;
        }
    }
}
