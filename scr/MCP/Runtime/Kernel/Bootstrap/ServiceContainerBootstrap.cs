using MCP.DependencyInjection;
using MCP.DependencyInjection.Extensions;

namespace MCP.Kernel.Bootstrap
{
    public sealed class ServiceContainerBootstrap : IBootstrap
    {
        public int Order => (int)BootstrapEnum.ServiceContainer;

        public void Initialize()
        {
            ServiceContainer.AddModule<KernelModule>();

            var options = new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = false
            };

            ServiceContainer.Build(options);
        }
    }
}
