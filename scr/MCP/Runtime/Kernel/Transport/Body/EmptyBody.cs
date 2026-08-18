using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MCP.Kernel.Transport
{
    public sealed class EmptyBody : IResponseBody
    {
        public static readonly EmptyBody Instance = new();

        private EmptyBody() { }

        public bool IsStreaming => false;
        public long? ContentLength => 0;

        public Task WriteToAsync(Stream stream, CancellationToken _)
            => Task.CompletedTask;
    }
}
