using MCP.DependencyInjection;
using MCP.Kernel.Attributes;
using MCP.Kernel.Registry;
using MCP.Kernel.Server;
using MCP.Kernel.Transport;
using MCP.Protocol;
using System.Threading.Tasks;

namespace MCP.Kernel.RouteHandler
{
    [HttpRoute(HttpMethod.POST, "tools/call")]
    public sealed class ToolsCallHandler : IRouteHandler
    {
        private readonly JsonRpcResponse rpcResponse;
        private readonly MCPToolRegistry registry;

        public ToolsCallHandler()
        {
            rpcResponse = new JsonRpcResponse();
            registry = ServiceContainer.GetService<MCPToolRegistry>();
        }
        public async ValueTask<MCPResponse> Handle(MCPRequest request, JsonRpcRequest message)
        {
            //Debug.Log(message.Params);
            var requestParams = message.Params.ToObject<CallToolRequestParams>();
            var toolResult = registry.Resolve(requestParams.Name);
            if (toolResult.Result)
            {
                var tool = toolResult.Value;
                var context = new RequestContext<CallToolRequestParams>(message, requestParams);
                await MiniTask.MiniTask.MainThread();

                var result = await tool.InvokeAsync(context);
                rpcResponse.Result = result.AsToken();
                rpcResponse.Id = message.Id;
            }
            //Debug.Log(requestParams);
            //var response = MCPResponse.MethodNotAllowed();
            var response = MCPResponse.Json(rpcResponse);
            return await new ValueTask<MCPResponse>(response);
        }
    }
}
