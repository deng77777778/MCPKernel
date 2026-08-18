using MCP.Kernel.Transport;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public sealed class StreamingBody : IResponseBody
{
    private readonly IStreamWriterAsync _writer;

    public StreamingBody(IStreamWriterAsync writer)
        => _writer = writer ?? throw new ArgumentNullException(nameof(writer));

    public bool IsStreaming => true;

    public long? ContentLength => null;

    public Task WriteToAsync(Stream output, CancellationToken cancellationToken = default)
        => _writer.WriteAsync(output, cancellationToken);
}