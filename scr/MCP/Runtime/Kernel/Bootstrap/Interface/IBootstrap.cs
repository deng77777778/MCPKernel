
namespace MCP.Kernel.Bootstrap
{
    public interface IBootstrap
    {
        int Order { get; }
        void Initialize();
    }
}
