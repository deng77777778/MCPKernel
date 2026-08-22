using MCP.Kernel.Registry;
using System.Linq;
using UnityEditor;

namespace MCP.Initialize.Editor
{
    public static class MCPKernelEditorInitialize
    {
        private static readonly BootstrapRegistry registry = new();
        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            if (EditorApplication.isPlaying) return;
            var bootstraps = MCP.Kernel.BootstrapTypeGenerated.CreateAllInstances().OrderBy(b => b.Order);

            foreach (var bootstrap in bootstraps)
            {
                registry.Register(bootstrap.GetType(), bootstrap);
                bootstrap.Initialize();
            }
        }
    }
}
