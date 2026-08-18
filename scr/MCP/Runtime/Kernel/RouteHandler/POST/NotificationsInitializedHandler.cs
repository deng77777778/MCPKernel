using MCP.Kernel.Attributes;
using MCP.Kernel.Transport;
using MCP.Protocol;
using System.Threading.Tasks;

namespace MCP.Kernel.RouteHandler
{
    [HttpRoute(HttpMethod.POST, "notifications/initialized")]
    public sealed class NotificationsInitializedHandler : IRouteHandler
    {
        public ValueTask<MCPResponse> Handle(MCPRequest request, JsonRpcRequest message)
        {
            var response = MCPResponse.StatusCodeResponse(System.Net.HttpStatusCode.Accepted);
            return new ValueTask<MCPResponse>(response);
        }
    }
}
