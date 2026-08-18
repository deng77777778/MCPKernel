using MCP.DependencyInjection;
using MCP.Kernel.Attributes;
using MCP.Kernel.Registry;
using MCP.Kernel.Transport;
using MCP.Protocol;
using System.Threading.Tasks;

namespace MCP.Kernel.RouteHandler
{
    [HttpRoute(HttpMethod.POST, "resources/list")]
    public sealed class ResourceListHandler : IRouteHandler
    {
        private readonly JsonRpcResponse rpcResponse;
        private readonly MCPResourceRegistry registry;
        public ResourceListHandler()
        {
            rpcResponse = new JsonRpcResponse();
            registry = ServiceContainer.GetService<MCPResourceRegistry>();
        }

        public ValueTask<MCPResponse> Handle(MCPRequest request, JsonRpcRequest message)
        {
            var result = new ListResourcesResult();
            rpcResponse.Id = message.Id;

            foreach (var resouce in registry.Values)
            {
                result.Resources.Add(resouce.ProtocolResource);
            }
            rpcResponse.Result = result.AsToken();

            var response = MCPResponse.Json(rpcResponse);
            response.Headers["Mcp-Session-Id"] = InitializeHandler.SessionId;
            return new ValueTask<MCPResponse>(response);
        }

    }

}
