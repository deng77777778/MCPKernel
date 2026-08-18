using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MCP.Kernel.Transport.Body
{
    public sealed class BytesBody : IResponseBody
    {
        private readonly byte[] _bytes;

        public BytesBody(byte[] bytes)
            => _bytes = bytes ?? Array.Empty<byte>();

        public bool IsStreaming => false;
        public long? ContentLength => _bytes.Length;

        public Task WriteToAsync(Stream output, CancellationToken ct = default)
            => output.WriteAsync(_bytes, ct).AsTask();
    }
}
