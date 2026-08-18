#nullable enable
using MCP.Kernel.Cache;
using MCP.Kernel.Server;
using MCP.Protocol;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MCP.Kernel.Schema
{
    public sealed class ToolSchemaGenerator : McpSchemaGeneratorBase<Tool, McpServerToolAttribute>
    {
        private readonly TypeSchemaGenerator _typeGenerator = new();
        private static UnifiedCache Cache => UnifiedCache.Instance;
        protected override Tool? GenerateCore(MethodInfo method)
        {
            var attr = GetAttribute(method);
            if (attr == null) return null;

            var options = CurrentOptions;

            // 使用 UnifiedCache 替代 SchemaCache
            if (options.EnableCaching && Cache.TryGetFunctionSchema(method, out var cachedSchema))
            {
                var cached = cachedSchema.ToObject<Tool>();
                if (cached != null) 
                    return cached;
            }

            var tool = new Tool
            {
                Name = attr.Name ?? GetName(method),
                Title = attr.Title,
                Description = GetDescription(method) ?? attr.Title,
                InputSchema = BuildInputSchema(method),
                Annotations = BuildAnnotations(attr),
                Meta = new JObject { ["method"] = method.Name }
            };

            if (!string.IsNullOrEmpty(attr.IconSource))
                tool.Icons = new List<Icon> { new Icon { Source = attr.IconSource } };

            if (attr.UseStructuredContent)
            {
                var outputType = attr.OutputSchemaType ?? GetReturnType(method);
                if (outputType != null && outputType != typeof(void))
                    tool.OutputSchema = SchemaHelpers.CreateJsonSchema(outputType);
            }

            if (options.EnableCaching)
            {
                // 存储为 JObject
                Cache.AddFunctionSchema(method, JObject.FromObject(tool));
            }

            return tool;
        }
        private JObject BuildInputSchema(MethodInfo method)
        {
            // 修复：使用 Type() 代替 WithType()，使用 Property() 代替 AddProperty()
            var builder = new SchemaBuilder()
                .Type("object")
                .Description(GetDescription(method));

            foreach (var param in method.GetParameters().Where(p => !IsSpecialParameter(p)))
            {
                var schema = SchemaHelpers.CreateJsonSchema(param.ParameterType);
                var desc = GetParameterDescription(param);
                if (!string.IsNullOrEmpty(desc)) schema["description"] = desc;
                if (param.HasDefaultValue && param.DefaultValue != null)
                    schema["default"] = JToken.FromObject(param.DefaultValue);

                builder.Property(
                    NameHelper.GetParameterName(param),
                    schema,
                    IsParameterRequired(param));
            }

            return builder.Build();
        }

        private static ToolAnnotations? BuildAnnotations(McpServerToolAttribute? attr)
            => attr == null ? null : new ToolAnnotations
            {
                Title = attr.Title,
                DestructiveHint = attr._destructive,
                IdempotentHint = attr._idempotent,
                OpenWorldHint = attr._openWorld,
                ReadOnlyHint = attr._readOnly
            };

        private static Type? GetReturnType(MethodInfo method)
        {
            var rt = method.ReturnType;
            return rt.IsGenericType && (rt.GetGenericTypeDefinition() == typeof(Task<>) ||
                                        rt.GetGenericTypeDefinition() == typeof(ValueTask<>))
                ? rt.GetGenericArguments()[0]
                : rt;
        }
    }
}
