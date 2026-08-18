#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MCP.AI
{
    public class DelegatingAIFunction : AIFunction
    {
        protected AIFunction InnerFunction { get; }

        public DelegatingAIFunction(AIFunction innerFunction)
        {
            InnerFunction = innerFunction ?? throw new ArgumentNullException(nameof(innerFunction));
        }

        public override string Name => InnerFunction.Name;
        public override string Description => InnerFunction.Description;
        public override JToken JsonSchema => InnerFunction.JsonSchema;
        public override JToken? ReturnJsonSchema => InnerFunction.ReturnJsonSchema;
        public override JsonSerializerSettings JsonSerializerSettings =>
            InnerFunction.JsonSerializerSettings;
        public override MethodInfo? UnderlyingMethod => InnerFunction.UnderlyingMethod;
        public override IReadOnlyDictionary<string, object?> AdditionalProperties =>
            InnerFunction.AdditionalProperties;

        protected override ValueTask<object?> InvokeCoreAsync(
            AIFunctionArguments arguments,
            CancellationToken cancellationToken)
        {
            return InnerFunction.InvokeAsync(arguments, cancellationToken);
        }

        public override object? GetService(Type serviceType, object? serviceKey = null)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            return serviceKey == null && serviceType.IsInstanceOfType(this) ? this :
                   InnerFunction.GetService(serviceType, serviceKey);
        }
    }
}
