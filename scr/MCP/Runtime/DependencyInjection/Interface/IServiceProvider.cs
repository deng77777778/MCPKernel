using System;

namespace MCP.DependencyInjection
{
    /// <summary>
    /// 服务提供者接口
    /// </summary>
    public interface IServiceProvider
    {
        /// <summary>
        /// 获取指定类型的服务
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        /// <returns>服务实例，如果未注册则返回null</returns>
        object GetService(Type serviceType);
    }
}
