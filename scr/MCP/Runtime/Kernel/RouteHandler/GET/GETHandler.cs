using MCP.Kernel.Attributes;
using MCP.Kernel.Transport;
using MCP.Protocol;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MCP.Kernel.RouteHandler
{
    [HttpRoute(HttpMethod.GET, "")]
    public sealed class GETHandler : IRouteHandler
    {
        private IEnumerable<string> GenerateResult()
        {
            yield return "event: endpoint\ndata: /mcp\n\n";
        }
        public ValueTask<MCPResponse> Handle(MCPRequest request, JsonRpcRequest message)
        {
            var response = MCPResponse.Streaming(GenerateResult());
            response.Headers["Cache-Control"] = "no-cache";
            response.Headers["Connection"] = "keep-alive";
            return new ValueTask<MCPResponse>(response);

            //var response = MCPResponse.MethodNotAllowed();
            //return new ValueTask<MCPResponse>(response);
        }
    }
}
