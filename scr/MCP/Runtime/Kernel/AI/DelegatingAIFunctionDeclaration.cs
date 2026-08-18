#nullable enable
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MCP.AI
{
    public class DelegatingAIFunctionDeclaration : AIFunctionDeclaration
    {
        protected AIFunctionDeclaration InnerFunction { get; }

        public DelegatingAIFunctionDeclaration(AIFunctionDeclaration innerFunction)
        {
            InnerFunction = innerFunction ?? throw new ArgumentNullException(nameof(innerFunction));
        }

        public override string Name => InnerFunction.Name;
        public override string Description => InnerFunction.Description;
        public override JToken JsonSchema => InnerFunction.JsonSchema;
        public override JToken? ReturnJsonSchema => InnerFunction.ReturnJsonSchema;
        public override IReadOnlyDictionary<string, object?> AdditionalProperties =>
            InnerFunction.AdditionalProperties;

        public override object? GetService(Type serviceType, object? serviceKey = null)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            return serviceKey == null && serviceType.IsInstanceOfType(this) ? this :
                   InnerFunction.GetService(serviceType, serviceKey);
        }
    }
}
