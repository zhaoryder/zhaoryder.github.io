//=============================================================================
// 文件名：ServiceLocator.cs
// 所属模块：Core
// 命名空间：FC26.Core
// 作用：服务定位器（Service Locator）模式实现，用于解耦地注册与获取全局服务实例。
//       相比单例，服务定位器更适合"多服务、按接口注册"的场景，例如：
//         Register&lt;IAudioService&gt;(new AudioService());
//         var audio = ServiceLocator.Get&lt;IAudioService&gt;();
// 备注：本类为静态单例（非 MonoBehaviour），生命周期与 AppDomain 一致；
//       所有方法均线程安全（使用 lock 保护内部字典）。
//=============================================================================
using System;
using System.Collections.Generic;

namespace FC26.Core
{
    /// <summary>
    /// 服务定位器静态类。
    /// 提供按类型注册、获取、反注册服务实例的能力。
    /// </summary>
    public static class ServiceLocator
    {
        // 内部服务字典：以类型为键，存储对应实例。
        // 使用 Dictionary 而非 ConcurrentDictionary 以保持对 Unity 2022 LTS .NET 标准库的兼容。
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        // 同步锁对象，保证多线程访问安全。
        private static readonly object _lock = new object();

        /// <summary>
        /// 注册服务实例。若类型已注册，则覆盖旧实例。
        /// </summary>
        /// <typeparam name="T">服务接口或类型</typeparam>
        /// <param name="service">服务实例</param>
        public static void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service), "[ServiceLocator] 不允许注册 null 服务。");
            }

            lock (_lock)
            {
                _services[typeof(T)] = service;
            }
        }

        /// <summary>
        /// 注册服务实例（非泛型版本，便于反射调用）。
        /// </summary>
        /// <param name="serviceType">服务类型（通常为接口）</param>
        /// <param name="service">服务实例</param>
        public static void Register(Type serviceType, object service)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (!serviceType.IsInstanceOfType(service))
            {
                throw new ArgumentException($"[ServiceLocator] 实例类型 {service.GetType().Name} 不可赋值给 {serviceType.Name}。");
            }

            lock (_lock)
            {
                _services[serviceType] = service;
            }
        }

        /// <summary>
        /// 获取服务实例。若未注册则返回 null（不抛异常，调用方需判空）。
        /// </summary>
        /// <typeparam name="T">服务接口或类型</typeparam>
        /// <returns>服务实例，未注册时返回 null</returns>
        public static T Get<T>() where T : class
        {
            lock (_lock)
            {
                if (_services.TryGetValue(typeof(T), out var service))
                {
                    return service as T;
                }

                return null;
            }
        }

        /// <summary>
        /// 尝试获取服务实例，返回是否成功。
        /// </summary>
        /// <typeparam name="T">服务接口或类型</typeparam>
        /// <param name="service">输出服务实例</param>
        /// <returns>是否成功获取</returns>
        public static bool TryGet<T>(out T service) where T : class
        {
            lock (_lock)
            {
                if (_services.TryGetValue(typeof(T), out var obj))
                {
                    service = obj as T;
                    return service != null;
                }

                service = null;
                return false;
            }
        }

        /// <summary>
        /// 反注册服务。若不存在则无操作。
        /// </summary>
        /// <typeparam name="T">服务接口或类型</typeparam>
        public static void Unregister<T>() where T : class
        {
            lock (_lock)
            {
                _services.Remove(typeof(T));
            }
        }

        /// <summary>
        /// 反注册所有服务（场景切换或应用退出时调用，避免内存泄漏）。
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _services.Clear();
            }
        }
    }
}
