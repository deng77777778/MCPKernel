// Binding/ArgumentsBinder.cs
#nullable enable
using MCP.AI;
using System.Reflection;
using System.Threading;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// AIFunctionArguments 绑定器
    /// </summary>
    public class ArgumentsBinder : IParameterBinder
    {
        public int Priority => 90;

        public bool CanBind(ParameterInfo parameter)
        {
            return parameter.ParameterType == typeof(AIFunctionArguments);
        }

        public object? Bind(ParameterInfo parameter, AIFunctionArguments arguments, CancellationToken cancellationToken)
        {
            return arguments;
        }
    }
}