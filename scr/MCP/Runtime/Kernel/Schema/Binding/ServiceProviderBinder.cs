// Binding/ServiceProviderBinder.cs
#nullable enable
using MCP.AI;
using System;
using System.Reflection;
using System.Threading;
using IServiceProvider = MCP.DependencyInjection.IServiceProvider;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// IServiceProvider 绑定器
    /// </summary>
    public class ServiceProviderBinder : IParameterBinder
    {
        public int Priority => 80;

        public bool CanBind(ParameterInfo parameter)
        {
            return parameter.ParameterType == typeof(IServiceProvider);
        }

        public object? Bind(ParameterInfo parameter, AIFunctionArguments arguments, CancellationToken cancellationToken)
        {
            var services = arguments.Services;
            if (services == null && !DefaultValueHelper.TryGetValue(parameter, out _))
            {
                throw new ArgumentNullException(
                    nameof(AIFunctionArguments.Services),
                    $"Services are required for parameter '{parameter.Name}'.");
            }
            return services;
        }
    }
}