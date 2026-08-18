// Binding/CancellationTokenBinder.cs
#nullable enable
using MCP.AI;
using System.Reflection;
using System.Threading;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// CancellationToken 绑定器
    /// </summary>
    public class CancellationTokenBinder : IParameterBinder
    {
        public int Priority => 100;

        public bool CanBind(ParameterInfo parameter)
        {
            return parameter.ParameterType == typeof(CancellationToken);
        }

        public object? Bind(ParameterInfo parameter, AIFunctionArguments arguments, CancellationToken cancellationToken)
        {
            return cancellationToken;
        }
    }
}