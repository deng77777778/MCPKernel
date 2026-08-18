using MCP.Kernel.Transport;
using MCP.Protocol;
using System.Threading.Tasks;

//public interface IRouteHandler<in T>
//    where T : JsonRpcMessage
public interface IRouteHandler
{
    ValueTask<MCPResponse> Handle(MCPRequest request, JsonRpcRequest message);

}
