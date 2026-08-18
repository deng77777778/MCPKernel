using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MCP.DependencyInjection
{
    /// <summary>
    /// 高性能服务提供者（类似官方ServiceProvider）
    /// </summary>
    public class ServiceProvider : IServiceProvider, ISupportScopedServiceProvider, IDisposable
    {
        // 服务描述符查找表（按类型分组，支持同一类型的多个注册）
        private readonly Dictionary<Type, ServiceDescriptor[]> _serviceMap;

        // 单例缓存
        private readonly Dictionary<Type, object> _singletons = new();

        // 作用域缓存（用于Scoped生命周期）
        private readonly Dictionary<Type, object> _scopedInstances = new Dictionary<Type, object>();

        // 工厂委托缓存（编译后的表达式树）
        private readonly Dictionary<Type, Func<IServiceProvider, object>> _compiledFactories =
            new Dictionary<Type, Func<IServiceProvider, object>>();

        // 线程安全锁
        private readonly object _lock = new object();

        // 是否已释放
        private bool _disposed;

        // 根作用域（用于Scoped生命周期）
        private readonly ServiceProvider _root;

        /// <summary>
        /// 构造函数（根容器）
        /// </summary>
        public ServiceProvider(IEnumerable<ServiceDescriptor> descriptors)
        {
            if (descriptors == null)
                throw new ArgumentNullException(nameof(descriptors));

            _serviceMap = BuildServiceMap(descriptors);
            _root = this;
        }

        /// <summary>
        /// 构造函数（作用域容器，内部使用）
        /// </summary>
        internal ServiceProvider(ServiceProvider root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            _serviceMap = root._serviceMap;
            _compiledFactories = root._compiledFactories;
            _root = root;
        }

        /// <summary>
        /// 构建服务映射表（优化查找性能）
        /// </summary>
        private Dictionary<Type, ServiceDescriptor[]> BuildServiceMap(IEnumerable<ServiceDescriptor> descriptors)
        {
            var map = new Dictionary<Type, ServiceDescriptor[]>();
            var tempMap = new Dictionary<Type, List<ServiceDescriptor>>();

            foreach (var descriptor in descriptors)
            {
                if (descriptor == null)
                    continue;

                if (!tempMap.TryGetValue(descriptor.ServiceType, out var list))
                {
                    list = new List<ServiceDescriptor>();
                    tempMap[descriptor.ServiceType] = list;
                }
                list.Add(descriptor);
            }

            // 转换为数组以减少内存开销
            foreach (var kvp in tempMap)
            {
                map[kvp.Key] = kvp.Value.ToArray();
            }

            return map;
        }

        /// <summary>
        /// 获取服务（类似官方的GetService）
        /// </summary>
        public object GetService(Type serviceType)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ServiceProvider));

            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            if (!_serviceMap.TryGetValue(serviceType, out var descriptors) || descriptors.Length == 0)
                return null;

            // 返回最后一个注册的服务（与官方行为一致）
            var descriptor = descriptors[descriptors.Length - 1];
            return ResolveService(descriptor);
        }

        /// <summary>
        /// 解析服务实例
        /// </summary>
        private object ResolveService(ServiceDescriptor descriptor)
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));

            switch (descriptor.Lifetime)
            {
                case ServiceLifetime.Singleton:
                    return ResolveSingleton(descriptor);

                case ServiceLifetime.Scoped:
                    return ResolveScoped(descriptor);

                case ServiceLifetime.Transient:
                    return ResolveTransient(descriptor);

                default:
                    throw new ArgumentOutOfRangeException(nameof(descriptor.Lifetime),
                        $"不支持的生命周期类型: {descriptor.Lifetime}");
            }
        }

        /// <summary>
        /// 解析单例服务
        /// </summary>
        private object ResolveSingleton(ServiceDescriptor descriptor)
        {
            // 先尝试获取缓存的单例
            lock (_lock)
            {
                if (_root._singletons.TryGetValue(descriptor.ServiceType, out var instance))
                    return instance;
            }

            // 创建实例
            var newInstance = CreateInstance(descriptor);

            // 缓存单例
            lock (_lock)
            {
                if (!_root._singletons.ContainsKey(descriptor.ServiceType))
                {
                    _root._singletons[descriptor.ServiceType] = newInstance;
                }
                return _root._singletons[descriptor.ServiceType];
            }
        }

        /// <summary>
        /// 解析作用域服务
        /// </summary>
        private object ResolveScoped(ServiceDescriptor descriptor)
        {
            // 先尝试获取作用域内缓存的实例
            lock (_lock)
            {
                if (_scopedInstances.TryGetValue(descriptor.ServiceType, out var instance))
                    return instance;
            }

            // 创建实例
            var newInstance = CreateInstance(descriptor);

            // 缓存在当前作用域
            lock (_lock)
            {
                if (!_scopedInstances.ContainsKey(descriptor.ServiceType))
                {
                    _scopedInstances[descriptor.ServiceType] = newInstance;
                }
                return _scopedInstances[descriptor.ServiceType];
            }
        }

        /// <summary>
        /// 解析瞬态服务
        /// </summary>
        private object ResolveTransient(ServiceDescriptor descriptor)
        {
            // 每次都创建新实例
            return CreateInstance(descriptor);
        }

        /// <summary>
        /// 创建服务实例（核心方法）
        /// </summary>
        private object CreateInstance(ServiceDescriptor descriptor)
        {
            // 使用已注册的实例
            if (descriptor.ImplementationInstance != null)
                return descriptor.ImplementationInstance;

            // 使用工厂方法
            if (descriptor.ImplementationFactory != null)
                return descriptor.ImplementationFactory(this);

            // 使用类型创建
            if (descriptor.ImplementationType != null)
                return CreateInstanceFromType(descriptor.ImplementationType);

            throw new InvalidOperationException($"无法创建服务 {descriptor.ServiceType} 的实例");
        }

        /// <summary>
        /// 通过类型创建实例（使用编译后的表达式树）
        /// </summary>
        private object CreateInstanceFromType(Type implementationType)
        {
            if (implementationType == null)
                throw new ArgumentNullException(nameof(implementationType));

            // 获取或编译工厂方法
            if (!_compiledFactories.TryGetValue(implementationType, out var factory))
            {
                factory = CompileFactory(implementationType);
                lock (_lock)
                {
                    if (!_compiledFactories.ContainsKey(implementationType))
                    {
                        _compiledFactories[implementationType] = factory;
                    }
                }
            }

            return factory(this);
        }

        /// <summary>
        /// 编译表达式树为工厂委托
        /// </summary>
        private Func<IServiceProvider, object> CompileFactory(Type implementationType)
        {
            var constructors = implementationType.GetConstructors();

            if (constructors.Length == 0)
            {
                // 没有公共构造函数，尝试使用默认构造函数
                var defaultConstructor = implementationType.GetConstructor(Type.EmptyTypes);
                if (defaultConstructor == null)
                {
                    throw new InvalidOperationException(
                        $"类型 {implementationType.FullName} 没有公共构造函数");
                }
                constructors = new[] { defaultConstructor };
            }

            // 选择第一个构造函数（可以根据需要选择参数最多的）
            var constructor = constructors[0];
            var parameters = constructor.GetParameters();

            if (parameters.Length == 0)
            {
                // 无参构造函数
                var newExpression = Expression.New(constructor);
                var lambda = Expression.Lambda<Func<IServiceProvider, object>>(
                    Expression.Convert(newExpression, typeof(object)),
                    Expression.Parameter(typeof(IServiceProvider), "sp")
                );
                return lambda.Compile();
            }
            else
            {
                // 带参数的构造函数 - 从ServiceProvider解析依赖
                var spParameter = Expression.Parameter(typeof(IServiceProvider), "sp");
                var parameterExpressions = new Expression[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    var paramType = parameters[i].ParameterType;

                    // 调用GetService方法解析依赖
                    var getServiceMethod = typeof(IServiceProvider).GetMethod("GetService");
                    if (getServiceMethod == null)
                    {
                        throw new InvalidOperationException("无法找到GetService方法");
                    }

                    var callExpression = Expression.Call(
                        spParameter,
                        getServiceMethod,
                        Expression.Constant(paramType, typeof(Type))
                    );

                    // 转换到参数类型
                    parameterExpressions[i] = Expression.Convert(callExpression, paramType);
                }

                var newExpression = Expression.New(constructor, parameterExpressions);
                var lambda = Expression.Lambda<Func<IServiceProvider, object>>(
                    Expression.Convert(newExpression, typeof(object)),
                    spParameter
                );
                return lambda.Compile();
            }
        }

        /// <summary>
        /// 创建作用域
        /// </summary>
        public IServiceScope CreateScope()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ServiceProvider));

            return new ServiceScope(new ServiceProvider(_root));
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源（内部实现）
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    lock (_lock)
                    {
                        // 释放作用域中的可释放对象
                        foreach (var instance in _scopedInstances.Values)
                        {
                            if (instance is IDisposable disposable)
                            {
                                try
                                {
                                    disposable.Dispose();
                                }
                                catch
                                {
                                    // 忽略释放异常
                                }
                            }
                        }
                        _scopedInstances.Clear();
                    }
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~ServiceProvider()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// 服务作用域实现
    /// </summary>
    internal class ServiceScope : IServiceScope
    {
        private bool _disposed;

        public IServiceProvider ServiceProvider { get; }

        public ServiceScope(ServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (ServiceProvider is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }

                _disposed = true;
            }
        }

        ~ServiceScope()
        {
            Dispose(false);
        }
    }
}