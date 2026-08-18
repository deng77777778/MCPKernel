using MCP.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MCP.DependencyInjection
{
    /// <summary>
    /// 增强版服务容器
    /// </summary>
    public static class ServiceContainer
    {
        private static IServiceCollection _services;
        private static ServiceProvider _provider;
        private static readonly List<IServiceModule> _modules = new();

        /// <summary>
        /// 获取服务集合（用于注册服务）
        /// </summary>
        public static IServiceCollection Services
        {
            get
            {
                _services ??= new ServiceCollection();
                return _services;
            }
        }

        /// <summary>
        /// 获取服务提供者
        /// </summary>
        public static ServiceProvider Provider
        {
            get
            {
                if (_provider == null)
                {
                    throw new InvalidOperationException(
                        "服务容器尚未构建。请先调用 ServiceContainer.Build() 方法。");
                }
                return _provider;
            }
        }

        /// <summary>
        /// 添加服务模块
        /// </summary>
        public static void AddModule<T>() where T : IServiceModule, new()
        {
            AddModule(new T());
        }

        /// <summary>
        /// 添加服务模块
        /// </summary>
        public static void AddModule(IServiceModule module)
        {
            _modules.Add(module);
        }

        /// <summary>
        /// 构建服务容器
        /// </summary>
        /// <param name="options">构建选项</param>
        public static void Build(ServiceProviderOptions options = null)
        {
            // 执行所有模块的配置
            foreach (var module in _modules)
            {
                module.Configure(Services);
            }

            _provider = options != null
                ? Services.BuildServiceProvider(options)
                : Services.BuildServiceProvider();

        }

        /// <summary>
        /// 获取服务
        /// </summary>
        public static T GetService<T>() where T : class
        {
            return Provider.GetService<T>();
        }

        /// <summary>
        /// 获取必需服务（如果不存在则抛出异常）
        /// </summary>
        public static T GetRequiredService<T>() where T : class
        {
            return Provider.GetRequiredService<T>();
        }

        /// <summary>
        /// 创建服务作用域
        /// </summary>
        public static IServiceScope CreateScope()
        {
            return Provider.CreateScope();
        }

        /// <summary>
        /// 在作用域中执行操作
        /// </summary>
        public static void ExecuteInScope(Action<IServiceProvider> action)
        {
            using (var scope = CreateScope())
            {
                action(scope.ServiceProvider);
            }
        }

        /// <summary>
        /// 在作用域中执行操作并返回结果
        /// </summary>
        public static T ExecuteInScope<T>(Func<IServiceProvider, T> func)
        {
            using (var scope = CreateScope())
            {
                return func(scope.ServiceProvider);
            }
        }

        /// <summary>
        /// 重置容器
        /// </summary>
        public static void Reset()
        {
            _provider?.Dispose();
            _provider = null;
            _services = null;
            _modules.Clear();
            Debug.Log("服务容器已重置");
        }

        /// <summary>
        /// 检查服务是否已注册
        /// </summary>
        public static bool IsRegistered<T>() where T : class
        {
            if (_provider == null) return false;
            return _provider.GetService<T>() != null;
        }

        /// <summary>
        /// 释放容器资源
        /// </summary>
        public static void Dispose()
        {
            _provider?.Dispose();
            _provider = null;
            _services = null;
            _modules.Clear();
        }
    }
}
