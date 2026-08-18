using MCP.DependencyInjection;
using MCP.Kernel.Extensions;
using MCP.Kernel.Registry;
using MCP.Kernel.ScanAssembly;
using MCP.Kernel.Schema;
using MCP.Kernel.Server;
using MCP.Protocol;
using System;

namespace MCP.Kernel.RouteHandler
{
    public sealed class MCPResourceScaner : IScanType
    {
        private readonly MCPResourceRegistry registry;
        public MCPResourceScaner()
        {
            registry = ServiceContainer.GetService<MCPResourceRegistry>();
        }

        public bool AllowScan(Type type) =>
             type.HasAttribute<McpServerResourceTypeAttribute>();

        public void ScanType(Type type)
        {
            var methods = type.GetMethods<Resource>();

            foreach (var method in methods)
            {
                try
                {
                    var resource = AIFunctionMcpServerResource.Create(method);
                    registry.Register(resource.ProtocolResource.Uri, resource);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.Log("b:"+ex.Message);
                }
            }
        }
    }
}
