using MCP.DependencyInjection;
using MCP.Kernel.Attributes;
using MCP.Kernel.Registry;
using MCP.Kernel.Transport;
using MCP.Protocol;
using System.Threading.Tasks;

namespace MCP.Kernel.RouteHandler
{
    [HttpRoute(HttpMethod.POST, "prompts/list")]
    public sealed class PromptListHandler : IRouteHandler
    {
        private readonly JsonRpcResponse rpcResponse;
        private readonly MCPPromptRegistry registry;
        public PromptListHandler()
        {
            rpcResponse = new JsonRpcResponse();
            registry = ServiceContainer.GetService<MCPPromptRegistry>();
        }

        public ValueTask<MCPResponse> Handle(MCPRequest request, JsonRpcRequest message)
        {
            var result = new ListPromptsResult();
            rpcResponse.Id = message.Id;
            

            foreach (var prompt in registry.Values)
            {
                result.Prompts.Add(prompt.ProtocolPrompt);
            }
            rpcResponse.Result = result.AsToken();

            var response = MCPResponse.Json(rpcResponse);
            response.Headers["Mcp-Session-Id"] = InitializeHandler.SessionId;
            return new ValueTask<MCPResponse>(response);
        }

    }
}
