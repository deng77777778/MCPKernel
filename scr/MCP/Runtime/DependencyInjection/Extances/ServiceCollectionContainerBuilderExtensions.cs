using System;

namespace MCP.DependencyInjection.Extensions
{
    /// <summary>
    /// 构建服务提供者的扩展方法
    /// </summary>
    public static class ServiceCollectionContainerBuilderExtensions
    {
        /// <summary>
        /// 从 IServiceCollection 构建 ServiceProvider
        /// </summary>
        public static ServiceProvider BuildServiceProvider(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            return new ServiceProvider(services);
        }

        /// <summary>
        /// 构建服务提供者（带选项）
        /// </summary>
        public static ServiceProvider BuildServiceProvider(this IServiceCollection services, ServiceProviderOptions options)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var provider = new ServiceProvider(services);

            // 如果设置了验证，可以在这里验证作用域
            if (options.ValidateScopes)
            {
                // 可以添加作用域验证逻辑
                ValidateScopes(provider, services);
            }

            if (options.ValidateOnBuild)
            {
                // 可以添加构建时验证逻辑
                ValidateServices(provider, services);
            }

            return provider;
        }

        private static void ValidateScopes(ServiceProvider provider, IServiceCollection services)
        {
            // 验证作用域服务不会在根容器中被解析为单例
            // 这是一个简化的实现
        }

        private static void ValidateServices(ServiceProvider provider, IServiceCollection services)
        {
            // 验证所有服务都可以被正确创建
            foreach (var descriptor in services)
            {
                try
                {
                    provider.GetService(descriptor.ServiceType);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"无法验证服务 {descriptor.ServiceType.FullName}: {ex.Message}", ex);
                }
            }
        }
    }

    /// <summary>
    /// ServiceProvider构建选项
    /// </summary>
    public class ServiceProviderOptions
    {
        /// <summary>
        /// 是否验证作用域
        /// </summary>
        public bool ValidateScopes { get; set; }

        /// <summary>
        /// 是否在构建时验证
        /// </summary>
        public bool ValidateOnBuild { get; set; }
    }
}