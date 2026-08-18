using System;

namespace MCP.DependencyInjection
{
    /// <summary>
    /// 服务作用域接口
    /// </summary>
    public interface IServiceScope : IDisposable
    {
        /// <summary>
        /// 作用域内的服务提供者
        /// </summary>
        IServiceProvider ServiceProvider { get; }
    }
}
