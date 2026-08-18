using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MCP.Kernel.Transport.Handler
{
    /// <summary>
    /// 空响应处理器（优化空响应的处理）
    /// </summary>
    public sealed class EmptyResponseHandler : IResponseHandler
    {
        public bool CanHandle(MCPResponse response)
            => response.Body is EmptyBody || response.Body.ContentLength == 0;

        public Task HandleAsync(HttpListenerResponse httpResponse, MCPResponse mcpResponse, CancellationToken ct)
        {
            httpResponse.SendChunked = false;
            httpResponse.ContentLength64 = 0;

            // 空响应不需要写入任何内容，直接关闭
            httpResponse.OutputStream.Close();
            return Task.CompletedTask;
        }
    }
}
