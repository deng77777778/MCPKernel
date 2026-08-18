using MCP.Kernel.Factory;
using MCP.Kernel.Transport;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MCP.Kernel.Extensions
{
    public static class HttpListenerExtensions
    {
        public static async ValueTask<MCPRequest> ToRequestAsync(this HttpListenerRequest hlr)
        {
            if (!Enum.TryParse<HttpMethod>(hlr.HttpMethod, true, out var method))
                throw new NotSupportedException(hlr.HttpMethod);
            var bodyBytes = await ReadBodyAsync(hlr);
            var request = new MCPRequest
            {
                Method = method,
                Path = hlr.Url.AbsolutePath,
                Headers = hlr.Headers.AllKeys
                    .Where(k => k != null)
                    .ToDictionary(k => k, k => hlr.Headers[k]),
                QueryParameters = hlr.QueryString.AllKeys
                   .Where(k => k != null)
                   .ToDictionary(k => k, k => hlr.QueryString[k]),
                Body = (hlr.ContentEncoding ?? Encoding.UTF8).GetString(bodyBytes)
            };
            return request;
        }

        /// <summary>
        /// 将 MCPResponse 应用到 HttpListenerResponse（策略模式）
        /// </summary>
        public static async ValueTask ApplyResponseAsync(
            this HttpListenerResponse response,
            MCPResponse mcpResponse,
            CancellationToken cancellationToken = default)
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response));
            if (mcpResponse == null)
                throw new ArgumentNullException(nameof(mcpResponse));

            // 1. 设置基础属性
            response.StatusCode = mcpResponse.StatusCode;
            response.ContentType = mcpResponse.ContentType ?? "application/json";

            // 2. 应用用户自定义 Headers（过滤掉禁止的头）
            ApplyCustomHeaders(response, mcpResponse.Headers);

            // 3. 使用策略模式处理 Body
            var handler = ResponseHandlerFactory.GetHandler(mcpResponse);
            await handler.HandleAsync(response, mcpResponse, cancellationToken);
        }

        /// <summary>
        /// 应用自定义 Headers
        /// </summary>
        private static void ApplyCustomHeaders(HttpListenerResponse response, Dictionary<string, string> headers)
        {
            if (headers == null || headers.Count == 0)
                return;

            foreach (var (key, value) in headers)
            {
                if (HttpHeaderValidator.IsValid(key))
                {
                    try
                    {
                        response.Headers[key] = value;
                    }
                    catch (ArgumentException ex)
                    {
                        // 某些特殊头可能无法设置，记录日志但不中断流程
                        // 这里可以根据需要添加日志
                        UnityEngine.Debug.Log($"Failed to set header '{key}': {ex.Message}");
                    }
                }
            }
        }
        private static async ValueTask<byte[]> ReadBodyAsync(HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
                return Array.Empty<byte>();

            await using var ms = new MemoryStream();
            var buffer = ArrayPool<byte>.Shared.Rent(8192);

            try
            {
                int bytesRead;
                while ((bytesRead = await request.InputStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
                {
                    ms.Write(buffer.AsSpan(0, bytesRead));
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            return ms.ToArray();
        }
    }
}
