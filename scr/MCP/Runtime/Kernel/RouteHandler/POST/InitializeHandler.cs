using MCP.Kernel.Attributes;
using MCP.Kernel.Transport;
using MCP.Protocol;
using System;
using System.Threading.Tasks;

namespace MCP.Kernel.RouteHandler
{
    [HttpRoute(HttpMethod.POST, "initialize")]
    public sealed class InitializeHandler : IRouteHandler
    {
        public static string SessionId = string.Empty;
        private readonly JsonRpcResponse rpcResponse;
        public InitializeHandler()
        {
            rpcResponse = new JsonRpcResponse
            {
                Result = new InitializeResult()
                {
                    ProtocolVersion = McpProtocolVersions.November2025ProtocolVersion,
                    ServerInfo = new Implementation { Name = "Unity MCP Server", Version = "1.0" },
                    Capabilities = new ServerCapabilities() { Tools = new(), Resources = new() },
                }.AsToken()
            };
        }
        public ValueTask<MCPResponse> Handle(MCPRequest request, JsonRpcRequest message)
        {
            rpcResponse.Id = message.Id;

            var response = MCPResponse.Json(rpcResponse);

            SessionId = Guid.NewGuid().ToString("N");
            response.Headers["Mcp-Session-Id"] = SessionId;

            return new ValueTask<MCPResponse>(response);
        }

    }
}
