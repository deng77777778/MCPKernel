using System.IO;
using System.Threading;

namespace MCP.Kernel.Transport
{
    public interface IResponseBody
    {
        /// <summary>
        /// 是否为流式响应（chunked / SSE）
        /// </summary>
        bool IsStreaming { get; }

        /// <summary>
        /// 已知的内容长度；流式时为 null
        /// </summary>
        long? ContentLength { get; }

        /// <summary>
        /// 将内容写入输出流
        /// </summary>
        System.Threading.Tasks.Task WriteToAsync(Stream output, CancellationToken cancellationToken = default);
    }
}
