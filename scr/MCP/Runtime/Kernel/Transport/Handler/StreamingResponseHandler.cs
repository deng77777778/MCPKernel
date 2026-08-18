using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MCP.Kernel.Transport.Handler
{
    /// <summary>
    /// 流式响应处理器（SSE / Chunked）
    /// </summary>
    public sealed class StreamingResponseHandler : IResponseHandler
    {
        public bool CanHandle(MCPResponse response)
            => response.Body.IsStreaming;

        public Task HandleAsync(HttpListenerResponse httpResponse, MCPResponse mcpResponse, CancellationToken ct)
        {           
            // 流式响应不关闭 OutputStream，由调用方管理生命周期
            httpResponse.SendChunked = true;
            httpResponse.ContentLength64 = 0;
            httpResponse.Headers["Cache-Control"] = "no-cache";
            httpResponse.Headers["Connection"] = "keep-alive";

            return mcpResponse.Body.WriteToAsync(httpResponse.OutputStream, ct);
        }
    }
}
