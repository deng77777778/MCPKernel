using MCP.DependencyInjection;
using MCP.Kernel.Attributes;
using MCP.Kernel.Registry;
using MCP.Kernel.Server;
using MCP.Kernel.Transport;
using MCP.Protocol;
using System.Threading.Tasks;

namespace MCP.Kernel.RouteHandler
{
    [HttpRoute(HttpMethod.POST, "resources/read")]
    public sealed class ResourceReadHandler : IRouteHandler
    {
        private readonly JsonRpcResponse rpcResponse;
        private readonly MCPResourceRegistry registry;

        public ResourceReadHandler()
        {
            rpcResponse = new JsonRpcResponse();
            registry = ServiceContainer.GetService<MCPResourceRegistry>();
        }
        public async ValueTask<MCPResponse> Handle(MCPRequest request, JsonRpcRequest message)
        {
            var requestParams = message.Params.ToObject<ReadResourceRequestParams>();
            var resourceResult = registry.Resolve(requestParams.Uri);
            if (resourceResult.Result)
            {
                var resource = resourceResult.Value;
                var context = new RequestContext<ReadResourceRequestParams>(message, requestParams);
                await MiniTask.MiniTask.MainThread();

                var result = await resource.ReadAsync(context);
                rpcResponse.Result = result.AsToken();
            }

            var response = MCPResponse.Json(rpcResponse);
            return await new ValueTask<MCPResponse>(response);
        }

    }
}
