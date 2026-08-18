using System.Threading.Tasks;

namespace MCP.Kernel.Transport
{
    public interface IHttpTransport
    {
        Task StartAsync(int port);
        Task StopAsync();
    }
}