#nullable enable
using MCP.AI;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// Schema 生成选项
    /// </summary>
    public class SchemaOptions
    {
        public bool IncludeSchemaKeyword { get; set; } = true;
        public bool EnableCaching { get; set; } = true;
        public bool DetectCircularReferences { get; set; } = true;
        public bool AllowNulls { get; set; }
        public bool UseEnumNames { get; set; }
        public bool IgnoreReadOnlyProperties { get; set; }

        public Func<ParameterInfo, bool>? IncludeParameter { get; set; }
        public Func<ParameterInfo, string?>? ParameterDescriptionProvider { get; set; }
        public Func<Type, bool>? ShouldSkipType { get; set; }
        public Func<AIJsonSchemaCreateContext, JToken, JToken>? TransformSchemaNode { get; set; }
        public AIJsonSchemaTransformOptions? TransformOptions { get; set; }

        public static SchemaOptions FromAIOptions(AIJsonSchemaCreateOptions? options)
        {
            if (options == null) return new SchemaOptions();

            return new SchemaOptions
            {
                IncludeSchemaKeyword = options.IncludeSchemaKeyword,
                EnableCaching = true,
                DetectCircularReferences = true,
                IncludeParameter = options.IncludeParameter,
                ParameterDescriptionProvider = options.ParameterDescriptionProvider,
                ShouldSkipType = options.ShouldSkipType,
                TransformSchemaNode = options.TransformSchemaNode,
                TransformOptions = options.TransformOptions
            };
        }

        public AIJsonSchemaCreateOptions ToAIOptions()
        {
            return new AIJsonSchemaCreateOptions
            {
                IncludeSchemaKeyword = IncludeSchemaKeyword,
                IncludeParameter = IncludeParameter,
                ParameterDescriptionProvider = ParameterDescriptionProvider,
                ShouldSkipType = ShouldSkipType,
                TransformSchemaNode = TransformSchemaNode,
                TransformOptions = TransformOptions,
                AllowNulls = AllowNulls,
                UseEnumNames = UseEnumNames,
                IgnoreReadOnlyProperties = IgnoreReadOnlyProperties
            };
        }
    }
}
