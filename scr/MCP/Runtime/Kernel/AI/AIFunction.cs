#nullable enable
using Newtonsoft.Json;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MCP.AI
{
    public abstract class AIFunction : AIFunctionDeclaration
    {
        protected AIFunction() { }

        public virtual MethodInfo? UnderlyingMethod => null;
        public virtual JsonSerializerSettings JsonSerializerSettings =>
            AIJsonUtilities.DefaultSettings;

        public ValueTask<object?> InvokeAsync(
            AIFunctionArguments? arguments = null,
            CancellationToken cancellationToken = default)
        {
            return InvokeCoreAsync(arguments ?? new AIFunctionArguments(), cancellationToken);
        }

        protected abstract ValueTask<object?> InvokeCoreAsync(
            AIFunctionArguments arguments,
            CancellationToken cancellationToken);

        public AIFunctionDeclaration AsDeclarationOnly() =>
            new NonInvocableAIFunction(this);

        private sealed class NonInvocableAIFunction : DelegatingAIFunctionDeclaration
        {
            public NonInvocableAIFunction(AIFunction function) : base(function) { }
        }
    }
}