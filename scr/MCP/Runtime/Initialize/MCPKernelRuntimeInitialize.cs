using MCP.Kernel.Registry;
using System.Linq;
using UnityEngine;

namespace MCP.Initialize
{
    public static class MCPKernelRuntimeInitialize
    {
        private static readonly BootstrapRegistry registry = new();
        [RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            var bootstraps = MCP.Kernel.BootstrapTypeGenerated.CreateAllInstances().OrderBy(b => b.Order);

            foreach (var bootstrap in bootstraps)
            {
                registry.Register(bootstrap.GetType(), bootstrap);
                bootstrap.Initialize();
            }
        }
    }

}
