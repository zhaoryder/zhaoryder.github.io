//=============================================================================
// 文件名：MonoSingleton.cs
// 所属模块：Core
// 命名空间：FC26.Core
// 作用：泛型单例基类，供 GameManager、UIManager、InputManager 等全局唯一
//       MonoBehaviour 派生类继承使用。Awake 时注册 Instance，OnDestroy 时清理。
// 备注：本类仅适用于 MonoBehaviour 子类；不保证多场景并存时的唯一性，
//       切换场景时若需保留请配合 DontDestroyOnLoad 使用。
//=============================================================================
using UnityEngine;

namespace FC26.Core
{
    /// <summary>
    /// 泛型单例基类。
    /// 继承方式：public class GameManager : MonoSingleton&lt;GameManager&gt; { ... }
    /// 通过 Instance 静态属性访问唯一实例。
    /// </summary>
    /// <typeparam name="T">需要被单例化的 MonoBehaviour 子类类型</typeparam>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        // 静态实例引用。使用 volatile 保证多线程读取时拿到最新值
        // （Unity API 仍需在主线程调用，此处仅为防止编译器优化导致的可见性问题）。
        private static volatile T _instance;

        // 同步锁对象，用于双重检查锁定，避免重复创建。
        private static readonly object _lock = new object();

        // 标记实例是否已被销毁（例如应用退出时），避免在 OnDestroy 后再访问产生幽灵实例。
        private static bool _applicationIsQuitting = false;

        /// <summary>
        /// 全局唯一实例访问入口。
        /// 若场景中尚未存在实例，会自动查找；若仍找不到则返回 null（不自动创建，避免与场景生命周期冲突）。
        /// </summary>
        public static T Instance
        {
            get
            {
                // 应用退出期间禁止再创建/获取实例，避免产生幽灵对象。
                if (_applicationIsQuitting)
                {
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        // 在已加载场景中查找现有实例。
                        _instance = FindObjectOfType<T>();

                        if (_instance == null)
                        {
                            // 未找到实例时不再自动创建（由子类在场景中预先挂载）。
                            // 返回 null，调用方需自行判空。
                            return null;
                        }
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// 标记当前实例是否已成功初始化（Awake 已执行完毕）。
        /// 子类可据此判断是否可安全使用。
        /// </summary>
        protected bool IsInitialized { get; private set; }

        /// <summary>
        /// Unity Awake 回调。子类如需重写，必须调用 base.Awake() 以完成单例注册。
        /// </summary>
        protected virtual void Awake()
        {
            // 双重检查：若已存在不同实例，则销毁当前重复对象，保证全局唯一。
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[MonoSingleton] 检测到 {typeof(T).Name} 的重复实例，已销毁新对象。");
                Destroy(gameObject);
                return;
            }

            lock (_lock)
            {
                _instance = this as T;
            }

            IsInitialized = true;
        }

        /// <summary>
        /// Unity OnDestroy 回调。子类如需重写，必须调用 base.OnDestroy() 以清理静态引用。
        /// </summary>
        protected virtual void OnDestroy()
        {
            // 仅当当前实例是自己时才清理，避免清理到新替换的实例。
            if (_instance == this)
            {
                _instance = null;
            }

            IsInitialized = false;
        }

        /// <summary>
        /// Unity OnApplicationQuit 回调。标记应用退出，禁止后续再创建实例。
        /// </summary>
        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }
    }
}
