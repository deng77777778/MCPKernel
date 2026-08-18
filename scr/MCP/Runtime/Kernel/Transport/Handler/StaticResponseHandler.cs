using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MCP.Kernel.Transport.Handler
{
    /// <summary>
    /// 静态响应处理器（非流式）
    /// </summary>
    public sealed class StaticResponseHandler : IResponseHandler
    {
        public bool CanHandle(MCPResponse response)
            => !response.Body.IsStreaming;

        public async Task HandleAsync(HttpListenerResponse httpResponse, MCPResponse mcpResponse, CancellationToken ct)
        {
            var body = mcpResponse.Body;
            var contentLength = body.ContentLength ?? 0;

            httpResponse.SendChunked = false;
            httpResponse.ContentLength64 = contentLength;

            if (contentLength > 0)
            {
                await body.WriteToAsync(httpResponse.OutputStream, ct);
            }

            httpResponse.OutputStream.Close();
        }
    }

}
