using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MCP.Kernel.Transport
{
    public interface IStreamWriterAsync 
    {
        Task WriteAsync(Stream output, CancellationToken cancellationToken);
    }
}
