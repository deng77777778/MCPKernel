using MCP.DependencyInjection;
using MCP.DependencyInjection.Extensions;
using MCP.Kernel.Registry;

namespace MCP.Kernel
{
    public class KernelModule : IServiceModule
    {
        public void Configure(IServiceCollection services)
        {
            services
                .AddSingleton<BootstrapRegistry>()
                .AddSingleton<HttpMethodRegistry>()
                .AddSingleton<RouteHandlerRegistry>()
                .AddSingleton<MCPResourceRegistry>()
                .AddSingleton<MCPPromptRegistry>()
                .AddSingleton<MCPToolRegistry>();
        }
    }
}
