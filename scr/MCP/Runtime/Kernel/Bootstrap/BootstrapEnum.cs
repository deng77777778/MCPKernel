namespace MCP.Kernel.Bootstrap
{
    public enum BootstrapEnum : int
    {
        /// <summary>
        /// IOC容器初始化
        /// </summary>
        ServiceContainer,
        /// <summary>
        /// HttpMethodRegistry初始化
        /// </summary>
        HttpMethod,
        /// <summary>
        /// 程序集搜索初始化
        /// </summary>
        ScanAssembly
    }
}
