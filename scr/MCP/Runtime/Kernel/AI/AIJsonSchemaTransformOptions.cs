#nullable enable
using Newtonsoft.Json.Linq;
using System;

namespace MCP.AI
{
    /// <summary>
    /// Provides options for configuring the behavior of <see cref="AIJsonUtilities"/> JSON schema transformation functionality.
    /// </summary>
    public sealed class AIJsonSchemaTransformOptions
    {
        /// <summary>
        /// Gets or initializes a callback that is invoked for every schema that is generated within the type graph.
        /// </summary>
        public Func<AIJsonSchemaTransformContext, JToken, JToken>? TransformSchemaNode { get; set; }

        /// <summary>
        /// Gets a value indicating whether to convert boolean schemas to equivalent object-based representations.
        /// </summary>
        public bool ConvertBooleanSchemas { get; set; }

        /// <summary>
        /// Gets a value indicating whether to generate schemas with the additionalProperties set to false for .NET objects.
        /// </summary>
        public bool DisallowAdditionalProperties { get; set; }

        /// <summary>
        /// Gets a value indicating whether to mark all properties as required in the schema.
        /// </summary>
        public bool RequireAllProperties { get; set; }

        /// <summary>
        /// Gets a value indicating whether to substitute nullable "type" keywords with OpenAPI 3.0 style "nullable" keywords in the schema.
        /// </summary>
        public bool UseNullableKeyword { get; set; }

        /// <summary>
        /// Gets a value indicating whether to move the default keyword to the description field in the schema.
        /// </summary>
        public bool MoveDefaultKeywordToDescription { get; set; }

        /// <summary>
        /// Gets the default options instance.
        /// </summary>
        internal static AIJsonSchemaTransformOptions Default { get; } = new();

        /// <summary>
        /// Checks if any transformation is enabled.
        /// </summary>
        internal bool HasTransformations =>
            ConvertBooleanSchemas ||
            DisallowAdditionalProperties ||
            RequireAllProperties ||
            UseNullableKeyword ||
            MoveDefaultKeywordToDescription ||
            TransformSchemaNode != null;

    }
}
