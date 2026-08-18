using MCP.Kernel.Attributes;
using MCP.Kernel.Transport;
using MCP.Protocol;
using System.Threading.Tasks;

namespace MCP.Kernel.RouteHandler
{
    [HttpRoute(HttpMethod.POST, "resources/templates/list")]
    public sealed class ResourceTemplatesListHandler : IRouteHandler
    {
        private readonly JsonRpcResponse rpcResponse;
        public ResourceTemplatesListHandler()
        {
            rpcResponse = new JsonRpcResponse();
        }

        public ValueTask<MCPResponse> Handle(MCPRequest request, JsonRpcRequest message)
        {
            var result = new ListResourceTemplatesResult();
            rpcResponse.Id = message.Id;

            rpcResponse.Result = result.AsToken();

            var response = MCPResponse.Json(rpcResponse);
            response.Headers["Mcp-Session-Id"] = InitializeHandler.SessionId;
            return new ValueTask<MCPResponse>(response);
        }

    }
}
