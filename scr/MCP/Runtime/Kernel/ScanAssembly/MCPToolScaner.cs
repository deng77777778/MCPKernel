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
    public class MCPToolScaner : IScanType
    {
        private readonly MCPToolRegistry registry;
        public MCPToolScaner()
        {
            registry = ServiceContainer.GetService<MCPToolRegistry>();
        }

        public bool AllowScan(Type type) =>
             type.HasAttribute<McpServerToolTypeAttribute>();

        public void ScanType(Type type)
        {
            var methods = type.GetMethods<Tool>();

            foreach (var method in methods)
            {
                var tool = AIFunctionMcpServerTool.Create(method);
                registry.Register(tool.ProtocolTool.Name, tool);
            }
        }
    }
}
