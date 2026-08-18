using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MCP.Kernel.Transport
{
    /// <summary>
    /// 响应处理器接口
    /// </summary>
    public interface IResponseHandler
    {
        bool CanHandle(MCPResponse response);
        Task HandleAsync(HttpListenerResponse httpResponse, MCPResponse mcpResponse, CancellationToken ct);
    }
}
