using MCP.DependencyInjection;
using MCP.Kernel.Attributes;
using MCP.Kernel.Registry;
using MCP.Kernel.Server;
using MCP.Kernel.Transport;
using MCP.Protocol;
using System.Threading.Tasks;

namespace MCP.Kernel.RouteHandler
{
    [HttpRoute(HttpMethod.POST, "prompts/get")]
    public sealed class PromptsGetHandler : IRouteHandler
    {
        private readonly JsonRpcResponse rpcResponse;
        private readonly MCPPromptRegistry registry;

        public PromptsGetHandler()
        {
            rpcResponse = new JsonRpcResponse();
            registry = ServiceContainer.GetService<MCPPromptRegistry>();
        }
        public async ValueTask<MCPResponse> Handle(MCPRequest request, JsonRpcRequest message)
        {
            //Debug.Log(message.Params);
            var requestParams = message.Params.ToObject<GetPromptRequestParams>();
            var paramResult = registry.Resolve(requestParams.Name);
            if (paramResult.Result)
            {
                var param = paramResult.Value;
                var context = new RequestContext<GetPromptRequestParams>(message, requestParams);
                await MiniTask.MiniTask.MainThread();

                var result = await param.GetAsync(context);
                rpcResponse.Result = result.AsToken();
            }

            var response = MCPResponse.Json(rpcResponse);
            return await new ValueTask<MCPResponse>(response);
        }
    }
}
