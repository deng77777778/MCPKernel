
using MCP.Kernel.Bootstrap;
using MCP.Kernel.Registry;
using System.Linq;
using UnityEditor;

namespace MCP.Unity.Editor
{
    public static class MCPKernelEditorBootstrap
    {
        private static readonly BootstrapRegistry registry = new();
        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            var bootstraps = UnityEditor.TypeCache
                       .GetTypesDerivedFrom<IBootstrap>()
                       .Select(t =>
                       {
                           return (IBootstrap)System.Activator.CreateInstance(t);
                       })
                       .OrderBy(b => b.Order);

            foreach (var bootstrap in bootstraps)
            {
                registry.Register(bootstrap.GetType(), bootstrap);
                bootstrap.Initialize();
            }
        }
    }
}
