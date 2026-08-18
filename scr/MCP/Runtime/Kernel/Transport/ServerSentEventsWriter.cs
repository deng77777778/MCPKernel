using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MCP.Kernel.Transport
{
    public sealed class ServerSentEventsWriter : IStreamWriterAsync
    {
        private readonly IEnumerable<string> _events;
        private static readonly Encoding Utf8 = new UTF8Encoding(false);

        public ServerSentEventsWriter(IEnumerable<string> events)
            => _events = events ?? throw new ArgumentNullException(nameof(events));

        public async Task WriteAsync(Stream output, CancellationToken ct)
        {
            foreach (var ev in _events)
            {
                var bytes = Utf8.GetBytes(ev);

                await output.WriteAsync(bytes, ct);
                await output.FlushAsync(ct); // ✅ 关键：立即推送
            }
        }
    }
}
