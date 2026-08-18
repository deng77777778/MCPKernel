using MCP.DependencyInjection;
using System;

namespace MCP.Kernel.Bootstrap
{
    public sealed class HttpMethodBootstrap : IBootstrap
    {
        public int Order => (int)BootstrapEnum.HttpMethod;

        public void Initialize()
        {
            var registry = ServiceContainer.GetService<HttpMethodRegistry>();
            var array = Enum.GetValues(typeof(HttpMethod));
            foreach (var val in array)
            {
                registry.Register((HttpMethod)val, new MCP.Kernel.Registry.RouteHandlerRegistry());
            }
        }
    }
}
