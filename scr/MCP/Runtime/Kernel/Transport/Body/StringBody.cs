using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MCP.Kernel.Transport.Body
{
    public sealed class StringBody : IResponseBody
    {
        private static readonly Encoding UTF8 = new UTF8Encoding(false);
        private readonly byte[] _bytes;

        public StringBody(string text)
            => _bytes = UTF8.GetBytes(text ?? string.Empty);

        public bool IsStreaming => false;
        public long? ContentLength => _bytes.Length;

        public Task WriteToAsync(Stream output, CancellationToken ct = default)
            => output.WriteAsync(_bytes, ct).AsTask();
    }
}
