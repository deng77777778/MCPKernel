namespace MCP.DependencyInjection
{
    /// <summary>
    /// 服务生命周期枚举
    /// </summary>
    public enum ServiceLifetime
    {
        /// <summary>
        /// 单例 - 整个应用程序生命周期内只有一个实例
        /// </summary>
        Singleton,

        /// <summary>
        /// 作用域 - 在每个作用域内共享同一个实例
        /// </summary>
        Scoped,

        /// <summary>
        /// 瞬态 - 每次请求都创建新的实例
        /// </summary>
        Transient
    }
}
