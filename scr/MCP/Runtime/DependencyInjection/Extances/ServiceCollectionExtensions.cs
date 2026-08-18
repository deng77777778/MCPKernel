
namespace MCP.DependencyInjection.Extensions
{
    /// <summary>
    /// 服务集合扩展方法（完全模仿官方API）
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        // ===== AddSingleton 重载 =====

        public static IServiceCollection AddSingleton<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            services.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), ServiceLifetime.Singleton));
            return services;
        }

        public static IServiceCollection AddSingleton<TService>(this IServiceCollection services)
            where TService : class
        {
            services.Add(new ServiceDescriptor(typeof(TService), typeof(TService), ServiceLifetime.Singleton));
            return services;
        }

        public static IServiceCollection AddSingleton<TService>(this IServiceCollection services, TService implementationInstance)
            where TService : class
        {
            services.Add(new ServiceDescriptor(typeof(TService), implementationInstance));
            return services;
        }

        public static IServiceCollection AddSingleton<TService>(this IServiceCollection services, System.Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            services.Add(new ServiceDescriptor(typeof(TService), sp => implementationFactory(sp), ServiceLifetime.Singleton));
            return services;
        }

        public static IServiceCollection AddSingleton(this IServiceCollection services, System.Type serviceType, System.Type implementationType)
        {
            services.Add(new ServiceDescriptor(serviceType, implementationType, ServiceLifetime.Singleton));
            return services;
        }

        public static IServiceCollection AddSingleton(this IServiceCollection services, System.Type serviceType, System.Func<IServiceProvider, object> implementationFactory)
        {
            services.Add(new ServiceDescriptor(serviceType, implementationFactory, ServiceLifetime.Singleton));
            return services;
        }

        // ===== AddScoped 重载 =====

        public static IServiceCollection AddScoped<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            services.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), ServiceLifetime.Scoped));
            return services;
        }

        public static IServiceCollection AddScoped<TService>(this IServiceCollection services)
            where TService : class
        {
            services.Add(new ServiceDescriptor(typeof(TService), typeof(TService), ServiceLifetime.Scoped));
            return services;
        }

        public static IServiceCollection AddScoped<TService>(this IServiceCollection services, System.Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            services.Add(new ServiceDescriptor(typeof(TService), sp => implementationFactory(sp), ServiceLifetime.Scoped));
            return services;
        }

        public static IServiceCollection AddScoped(this IServiceCollection services, System.Type serviceType, System.Type implementationType)
        {
            services.Add(new ServiceDescriptor(serviceType, implementationType, ServiceLifetime.Scoped));
            return services;
        }

        public static IServiceCollection AddScoped(this IServiceCollection services, System.Type serviceType, System.Func<IServiceProvider, object> implementationFactory)
        {
            services.Add(new ServiceDescriptor(serviceType, implementationFactory, ServiceLifetime.Scoped));
            return services;
        }

        // ===== AddTransient 重载 =====

        public static IServiceCollection AddTransient<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            services.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), ServiceLifetime.Transient));
            return services;
        }

        public static IServiceCollection AddTransient<TService>(this IServiceCollection services)
            where TService : class
        {
            services.Add(new ServiceDescriptor(typeof(TService), typeof(TService), ServiceLifetime.Transient));
            return services;
        }

        public static IServiceCollection AddTransient<TService>(this IServiceCollection services, System.Func<IServiceProvider, TService> implementationFactory)
            where TService : class
        {
            services.Add(new ServiceDescriptor(typeof(TService), sp => implementationFactory(sp), ServiceLifetime.Transient));
            return services;
        }

        public static IServiceCollection AddTransient(this IServiceCollection services, System.Type serviceType, System.Type implementationType)
        {
            services.Add(new ServiceDescriptor(serviceType, implementationType, ServiceLifetime.Transient));
            return services;
        }

        public static IServiceCollection AddTransient(this IServiceCollection services, System.Type serviceType, System.Func<IServiceProvider, object> implementationFactory)
        {
            services.Add(new ServiceDescriptor(serviceType, implementationFactory, ServiceLifetime.Transient));
            return services;
        }
    }
}
