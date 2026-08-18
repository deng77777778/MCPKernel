namespace MCP.DependencyInjection
{
    /// <summary>
    /// 服务作用域工厂接口
    /// </summary>
    public interface IServiceScopeFactory
    {
        /// <summary>
        /// 创建新的服务作用域
        /// </summary>
        IServiceScope CreateScope();
    }
}
