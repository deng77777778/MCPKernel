// Binding/IParameterBinder.cs
#nullable enable
using MCP.AI;
using System.Reflection;
using System.Threading;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// 参数绑定器接口
    /// </summary>
    public interface IParameterBinder
    {
        /// <summary>
        /// 优先级
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 判断是否能绑定该参数
        /// </summary>
        bool CanBind(ParameterInfo parameter);

        /// <summary>
        /// 绑定参数值
        /// </summary>
        object? Bind(ParameterInfo parameter, AIFunctionArguments arguments, CancellationToken cancellationToken);
    }
}