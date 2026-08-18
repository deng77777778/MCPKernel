using System;

namespace MCP.DependencyInjection
{
    /// <summary>
    /// 服务描述符（类似官方的ServiceDescriptor）
    /// 用于描述一个服务的注册信息，包括服务类型、实现类型、生命周期等
    /// </summary>
    public class ServiceDescriptor
    {
        /// <summary>
        /// 服务类型（通常是接口）
        /// </summary>
        public Type ServiceType { get; }

        /// <summary>
        /// 实现类型
        /// </summary>
        public Type ImplementationType { get; }

        /// <summary>
        /// 服务生命周期
        /// </summary>
        public ServiceLifetime Lifetime { get; }

        /// <summary>
        /// 已创建的实现实例（用于注册实例时）
        /// </summary>
        public object ImplementationInstance { get; }

        /// <summary>
        /// 实现工厂委托（用于工厂模式创建服务）
        /// </summary>
        public Func<IServiceProvider, object> ImplementationFactory { get; }

        /// <summary>
        /// 私有构造函数
        /// </summary>
        private ServiceDescriptor(Type serviceType, ServiceLifetime lifetime)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
            Lifetime = lifetime;
        }

        /// <summary>
        /// 通过类型映射创建服务描述符
        /// </summary>
        /// <param name="serviceType">服务类型（接口）</param>
        /// <param name="implementationType">实现类型</param>
        /// <param name="lifetime">生命周期</param>
        public ServiceDescriptor(Type serviceType, Type implementationType, ServiceLifetime lifetime)
            : this(serviceType, lifetime)
        {
            ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));

            // 验证实现类型是否继承或实现了服务类型
            if (!serviceType.IsAssignableFrom(implementationType))
            {
                throw new ArgumentException(
                    $"类型 {implementationType.FullName} 没有实现或继承 {serviceType.FullName}");
            }
        }

        /// <summary>
        /// 通过实例创建服务描述符
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        /// <param name="instance">已创建的实例</param>
        public ServiceDescriptor(Type serviceType, object instance)
            : this(serviceType, ServiceLifetime.Singleton)
        {
            ImplementationInstance = instance ?? throw new ArgumentNullException(nameof(instance));

            // 验证实例类型是否匹配
            if (!serviceType.IsInstanceOfType(instance))
            {
                throw new ArgumentException(
                    $"实例类型 {instance.GetType().FullName} 不能转换为 {serviceType.FullName}");
            }

            // 当使用实例时，实现类型就是实例的类型
            ImplementationType = instance.GetType();
        }

        /// <summary>
        /// 通过工厂委托创建服务描述符
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        /// <param name="factory">工厂委托</param>
        /// <param name="lifetime">生命周期</param>
        public ServiceDescriptor(Type serviceType, Func<IServiceProvider, object> factory, ServiceLifetime lifetime)
            : this(serviceType, lifetime)
        {
            ImplementationFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// 静态工厂方法：创建类型映射的描述符
        /// </summary>
        public static ServiceDescriptor Describe(Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            return new ServiceDescriptor(serviceType, implementationType, lifetime);
        }

        /// <summary>
        /// 静态工厂方法：创建实例的描述符
        /// </summary>
        public static ServiceDescriptor Describe(Type serviceType, object instance)
        {
            return new ServiceDescriptor(serviceType, instance);
        }

        /// <summary>
        /// 静态工厂方法：创建工厂的描述符
        /// </summary>
        public static ServiceDescriptor Describe(Type serviceType, Func<IServiceProvider, object> factory, ServiceLifetime lifetime)
        {
            return new ServiceDescriptor(serviceType, factory, lifetime);
        }

        /// <summary>
        /// 泛型静态工厂方法：创建类型映射的描述符
        /// </summary>
        public static ServiceDescriptor Describe<TService, TImplementation>(ServiceLifetime lifetime)
            where TService : class
            where TImplementation : class, TService
        {
            return Describe(typeof(TService), typeof(TImplementation), lifetime);
        }

        /// <summary>
        /// 泛型静态工厂方法：创建实例的描述符
        /// </summary>
        public static ServiceDescriptor Describe<TService>(TService instance)
            where TService : class
        {
            return Describe(typeof(TService), instance);
        }

        /// <summary>
        /// 泛型静态工厂方法：创建工厂的描述符
        /// </summary>
        public static ServiceDescriptor Describe<TService>(Func<IServiceProvider, object> factory, ServiceLifetime lifetime)
            where TService : class
        {
            return Describe(typeof(TService), factory, lifetime);
        }

        /// <summary>
        /// 创建单例类型映射的描述符
        /// </summary>
        public static ServiceDescriptor Singleton(Type serviceType, Type implementationType)
        {
            return new ServiceDescriptor(serviceType, implementationType, ServiceLifetime.Singleton);
        }

        /// <summary>
        /// 创建单例实例的描述符
        /// </summary>
        public static ServiceDescriptor Singleton(Type serviceType, object instance)
        {
            return new ServiceDescriptor(serviceType, instance);
        }

        /// <summary>
        /// 创建单例工厂的描述符
        /// </summary>
        public static ServiceDescriptor Singleton(Type serviceType, Func<IServiceProvider, object> factory)
        {
            return new ServiceDescriptor(serviceType, factory, ServiceLifetime.Singleton);
        }

        /// <summary>
        /// 泛型：创建单例类型映射的描述符
        /// </summary>
        public static ServiceDescriptor Singleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            return Singleton(typeof(TService), typeof(TImplementation));
        }

        /// <summary>
        /// 泛型：创建单例实例的描述符
        /// </summary>
        public static ServiceDescriptor Singleton<TService>(TService instance)
            where TService : class
        {
            return Singleton(typeof(TService), instance);
        }

        /// <summary>
        /// 泛型：创建单例工厂的描述符
        /// </summary>
        public static ServiceDescriptor Singleton<TService>(Func<IServiceProvider, object> factory)
            where TService : class
        {
            return Singleton(typeof(TService), factory);
        }

        /// <summary>
        /// 创建作用域类型映射的描述符
        /// </summary>
        public static ServiceDescriptor Scoped(Type serviceType, Type implementationType)
        {
            return new ServiceDescriptor(serviceType, implementationType, ServiceLifetime.Scoped);
        }

        /// <summary>
        /// 创建作用域工厂的描述符
        /// </summary>
        public static ServiceDescriptor Scoped(Type serviceType, Func<IServiceProvider, object> factory)
        {
            return new ServiceDescriptor(serviceType, factory, ServiceLifetime.Scoped);
        }

        /// <summary>
        /// 泛型：创建作用域类型映射的描述符
        /// </summary>
        public static ServiceDescriptor Scoped<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            return Scoped(typeof(TService), typeof(TImplementation));
        }

        /// <summary>
        /// 泛型：创建作用域工厂的描述符
        /// </summary>
        public static ServiceDescriptor Scoped<TService>(Func<IServiceProvider, object> factory)
            where TService : class
        {
            return Scoped(typeof(TService), factory);
        }

        /// <summary>
        /// 创建瞬态类型映射的描述符
        /// </summary>
        public static ServiceDescriptor Transient(Type serviceType, Type implementationType)
        {
            return new ServiceDescriptor(serviceType, implementationType, ServiceLifetime.Transient);
        }

        /// <summary>
        /// 创建瞬态工厂的描述符
        /// </summary>
        public static ServiceDescriptor Transient(Type serviceType, Func<IServiceProvider, object> factory)
        {
            return new ServiceDescriptor(serviceType, factory, ServiceLifetime.Transient);
        }

        /// <summary>
        /// 泛型：创建瞬态类型映射的描述符
        /// </summary>
        public static ServiceDescriptor Transient<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            return Transient(typeof(TService), typeof(TImplementation));
        }

        /// <summary>
        /// 泛型：创建瞬态工厂的描述符
        /// </summary>
        public static ServiceDescriptor Transient<TService>(Func<IServiceProvider, object> factory)
            where TService : class
        {
            return Transient(typeof(TService), factory);
        }

        /// <summary>
        /// 重写 ToString 方法
        /// </summary>
        public override string ToString()
        {
            var lifetimeStr = Lifetime.ToString();

            if (ImplementationInstance != null)
            {
                return $"ServiceType: {ServiceType.Name} | Lifetime: {lifetimeStr} | Instance: {ImplementationInstance.GetType().Name}";
            }
            else if (ImplementationFactory != null)
            {
                return $"ServiceType: {ServiceType.Name} | Lifetime: {lifetimeStr} | Factory";
            }
            else if (ImplementationType != null)
            {
                return $"ServiceType: {ServiceType.Name} | Lifetime: {lifetimeStr} | ImplementationType: {ImplementationType.Name}";
            }

            return $"ServiceType: {ServiceType.Name} | Lifetime: {lifetimeStr}";
        }

        /// <summary>
        /// 重写 Equals 方法
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is ServiceDescriptor other)
            {
                return ServiceType == other.ServiceType
                    && ImplementationType == other.ImplementationType
                    && Lifetime == other.Lifetime
                    && ReferenceEquals(ImplementationInstance, other.ImplementationInstance)
                    && ImplementationFactory == other.ImplementationFactory;
            }
            return false;
        }

        /// <summary>
        /// 重写 GetHashCode 方法
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ServiceType?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (ImplementationType?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ Lifetime.GetHashCode();
                hashCode = (hashCode * 397) ^ (ImplementationInstance?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (ImplementationFactory?.GetHashCode() ?? 0);
                return hashCode;
            }
        }

        /// <summary>
        /// 判断两个 ServiceDescriptor 是否相等
        /// </summary>
        public static bool operator ==(ServiceDescriptor left, ServiceDescriptor right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left is null || right is null)
                return false;
            return left.Equals(right);
        }

        /// <summary>
        /// 判断两个 ServiceDescriptor 是否不相等
        /// </summary>
        public static bool operator !=(ServiceDescriptor left, ServiceDescriptor right)
        {
            return !(left == right);
        }
    }
}
