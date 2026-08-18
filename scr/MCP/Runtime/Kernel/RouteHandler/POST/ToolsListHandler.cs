using MCP.DependencyInjection;
using MCP.Kernel.Attributes;
using MCP.Kernel.Registry;
using MCP.Kernel.Transport;
using MCP.Protocol;
using System.Threading.Tasks;

namespace MCP.Kernel.RouteHandler
{
    [HttpRoute(HttpMethod.POST, "tools/list")]
    public sealed class ToolsListHandler : IRouteHandler
    {
        private readonly JsonRpcResponse rpcResponse;
        private readonly MCPToolRegistry registry;
        public ToolsListHandler()
        {
            rpcResponse = new JsonRpcResponse();
            registry = ServiceContainer.GetService<MCPToolRegistry>();
        }

        public ValueTask<MCPResponse> Handle(MCPRequest request, JsonRpcRequest message)
        {
            var result = new ListToolsResult();
            rpcResponse.Id = message.Id;

            foreach (var tool in registry.Values)
            {
                result.Tools.Add(tool.ProtocolTool);
            }
            rpcResponse.Result = result.AsToken();

            var response = MCPResponse.Json(rpcResponse);
            response.Headers["Mcp-Session-Id"] = InitializeHandler.SessionId;
            return new ValueTask<MCPResponse>(response);
        }

    }
}