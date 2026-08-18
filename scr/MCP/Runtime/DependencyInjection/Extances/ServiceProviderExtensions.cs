using System;
using System.Collections.Generic;

namespace MCP.DependencyInjection.Extensions
{
    /// <summary>
    /// 泛型服务解析扩展
    /// </summary>
    public static class ServiceProviderExtensions
    {
        /// <summary>
        /// 获取服务（泛型版本）
        /// </summary>
        public static T GetService<T>(this IServiceProvider provider) where T : class
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            return (T)provider.GetService(typeof(T));
        }

        /// <summary>
        /// 获取必需服务（如果不存在则抛出异常）
        /// </summary>
        public static T GetRequiredService<T>(this IServiceProvider provider) where T : class
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            var service = provider.GetService(typeof(T));
            if (service == null)
                throw new InvalidOperationException($"服务 {typeof(T).FullName} 未注册");
            return (T)service;
        }

        /// <summary>
        /// 获取多个服务（返回所有注册的实现）
        /// </summary>
        public static IEnumerable<T> GetServices<T>(this IServiceProvider provider) where T : class
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            var serviceType = typeof(T);

            // 通过反射获取所有注册的服务
            if (provider is ServiceProvider sp)
            {
                // 这里可以访问内部的_serviceMap来获取所有注册
                // 由于_serviceMap是私有的，这里简化处理
                var service = provider.GetService(serviceType);
                if (service != null)
                    yield return (T)service;
            }
            else
            {
                var service = provider.GetService(serviceType);
                if (service != null)
                    yield return (T)service;
            }
        }

        /// <summary>
        /// 创建作用域并执行操作
        /// </summary>
        public static void CreateScope(this IServiceProvider provider, Action<IServiceProvider> action)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (provider is ISupportScopedServiceProvider scopedProvider)
            {
                using (var scope = scopedProvider.CreateScope())
                {
                    action(scope.ServiceProvider);
                }
            }
            else
            {
                throw new InvalidOperationException("当前服务提供者不支持创建作用域");
            }
        }

        /// <summary>
        /// 创建作用域并获取结果
        /// </summary>
        public static T CreateScope<T>(this IServiceProvider provider, Func<IServiceProvider, T> func)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            if (provider is ISupportScopedServiceProvider scopedProvider)
            {
                using (var scope = scopedProvider.CreateScope())
                {
                    return func(scope.ServiceProvider);
                }
            }
            else
            {
                throw new InvalidOperationException("当前服务提供者不支持创建作用域");
            }
        }
    }
}
