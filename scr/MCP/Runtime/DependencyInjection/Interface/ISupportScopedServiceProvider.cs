namespace MCP.DependencyInjection
{
    /// <summary>
    /// 支持作用域的服务提供者接口
    /// </summary>
    public interface ISupportScopedServiceProvider : IServiceProvider
    {
        /// <summary>
        /// 创建新的服务作用域
        /// </summary>
        IServiceScope CreateScope();
    }
}
