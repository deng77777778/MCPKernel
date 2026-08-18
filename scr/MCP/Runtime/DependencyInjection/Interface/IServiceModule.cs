namespace MCP.DependencyInjection
{
    /// <summary>
    /// 服务模块接口
    /// </summary>
    public interface IServiceModule
    {
        /// <summary>
        /// 配置服务
        /// </summary>
        void Configure(IServiceCollection services);
    }
}
