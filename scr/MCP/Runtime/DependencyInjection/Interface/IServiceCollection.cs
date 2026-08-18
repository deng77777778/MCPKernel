using System.Collections;
using System.Collections.Generic;

namespace MCP.DependencyInjection
{
    /// <summary>
    /// 服务集合接口
    /// </summary>
    public interface IServiceCollection : IList<ServiceDescriptor>, ICollection<ServiceDescriptor>, IEnumerable<ServiceDescriptor>, IEnumerable
    {
    }
}
