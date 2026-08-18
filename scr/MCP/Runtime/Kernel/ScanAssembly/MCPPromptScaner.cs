using MCP.AI;
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
    public sealed class MCPPromptScaner : IScanType
    {
        private readonly MCPPromptRegistry registry;
        public MCPPromptScaner()
        {
            registry = ServiceContainer.GetService<MCPPromptRegistry>();
        }
        public bool AllowScan(Type type) 
            => type.HasAttribute<McpServerPromptTypeAttribute>();

        public void ScanType(Type type)
        {
            var methods = type.GetMethods<Prompt>();

            foreach (var method in methods)
            {
                var prompt = AIFunctionMcpServerPrompt.Create(method);
                registry.Register(prompt.ProtocolPrompt.Name, prompt);
            }

            //var prompts = type.GenerateSchema<Prompt>();
            //foreach (var prompt in prompts)
            //{
            //    registry.Register(prompt.Name, prompt);
            //}
        }

    }
}
