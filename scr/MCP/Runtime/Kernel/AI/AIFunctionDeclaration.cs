#nullable enable
using Newtonsoft.Json.Linq;

namespace MCP.AI
{
    public abstract class AIFunctionDeclaration : AITool
    {
        protected AIFunctionDeclaration() { }

        public virtual JToken JsonSchema => AIJsonUtilities.DefaultJsonSchema;
        public virtual JToken? ReturnJsonSchema => null;
    }
}
