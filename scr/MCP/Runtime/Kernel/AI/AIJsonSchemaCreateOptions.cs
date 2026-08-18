#nullable enable
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace MCP.AI
{
    /// <summary>
    /// Provides options for configuring the behavior of <see cref="AIJsonUtilities"/> JSON schema creation functionality.
    /// </summary>
    public sealed class AIJsonSchemaCreateOptions
    {
        /// <summary>
        /// Gets the default options instance.
        /// </summary>
        public static AIJsonSchemaCreateOptions Default { get; } = new AIJsonSchemaCreateOptions();

        /// <summary>
        /// Gets or initializes a callback that is invoked for every schema that is generated within the type graph.
        /// </summary>
        public Func<AIJsonSchemaCreateContext, JToken, JToken>? TransformSchemaNode { get; set; }

        /// <summary>
        /// Gets a callback that is invoked for every parameter in the <see cref="MethodBase"/> provided to
        /// <see cref="AIJsonUtilities.CreateFunctionJsonSchema"/> in order to determine whether it should
        /// be included in the generated schema.
        /// </summary>
        /// <remarks>
        /// By default, when <see cref="IncludeParameter"/> is <see langword="null"/>, all parameters other
        /// than those of type <see cref="CancellationToken"/> are included in the generated schema.
        /// The delegate is not invoked for <see cref="CancellationToken"/> parameters.
        /// </remarks>
        public Func<ParameterInfo, bool>? IncludeParameter { get; set; }

        /// <summary>
        /// Gets a callback that is invoked for each parameter in the <see cref="MethodBase"/> provided to
        /// <see cref="AIJsonUtilities.CreateFunctionJsonSchema"/> to obtain a description for the parameter.
        /// </summary>
        /// <remarks>
        /// The delegate receives a <see cref="ParameterInfo"/> instance and returns a string describing
        /// the parameter. If <see langword="null"/>, or if the delegate returns <see langword="null"/>,
        /// the description will be sourced from the <see cref="MethodBase"/> metadata (like <see cref="DescriptionAttribute"/>),
        /// if available.
        /// </remarks>
        public Func<ParameterInfo, string?>? ParameterDescriptionProvider { get; set; }

        /// <summary>
        /// Gets or initializes a <see cref="AIJsonSchemaTransformOptions"/> governing transformations on the JSON schema after it has been generated.
        /// </summary>
        public AIJsonSchemaTransformOptions? TransformOptions { get; set; }

        /// <summary>
        /// Gets a value indicating whether to include the $schema keyword in created schemas.
        /// </summary>
        public bool IncludeSchemaKeyword { get; set; }

        public bool AllowNulls { get; set; }
        public bool UseEnumNames { get; set; }
        public bool IgnoreReadOnlyProperties { get; set; }
        /// <summary>
        /// Whether to detect circular references (default: true).
        /// </summary>
        public bool DetectCircularReferences { get; set; } = true;
        /// <summary>
        /// Custom filter for skipping specific types.
        /// </summary>
        public Func<Type, bool>? ShouldSkipType { get; set; }
        /// <summary>
        /// Whether to use caching (default: true).
        /// </summary>
        public bool EnableCaching { get; set; } = true;
        /// <summary>
        /// Creates a copy of this options instance.
        /// </summary>
        public AIJsonSchemaCreateOptions Clone()
        {
            return new AIJsonSchemaCreateOptions
            {
                IncludeSchemaKeyword = IncludeSchemaKeyword,
                IncludeParameter = IncludeParameter,
                ParameterDescriptionProvider = ParameterDescriptionProvider,
                TransformOptions = TransformOptions,
                TransformSchemaNode = TransformSchemaNode,
                DetectCircularReferences = DetectCircularReferences,
                ShouldSkipType = ShouldSkipType,
                EnableCaching = EnableCaching
            };
        }

        /// <summary>
        /// Creates options without caching.
        /// </summary>
        public static AIJsonSchemaCreateOptions NoCache => new() { EnableCaching = false };

        /// <summary>
        /// Creates options without $schema keyword.
        /// </summary>
        public static AIJsonSchemaCreateOptions WithoutSchemaKeyword => new() { IncludeSchemaKeyword = false };

    }
}