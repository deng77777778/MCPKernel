// Handlers/DictionaryTypeHandler.cs
#nullable enable
using Newtonsoft.Json.Linq;
using System;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// 字典类型处理器
    /// </summary>
    public class DictionaryTypeHandler : ITypeSchemaHandler
    {
        public int Priority => 70;

        public bool CanHandle(Type type)
        {
            return TypeHelper.IsDictionaryType(type);
        }

        public JObject GenerateSchema(Type type, SchemaContext context)
        {
            var args = type.GetGenericArguments();
            if (args.Length >= 2 && args[0] == typeof(string))
            {
                var valueType = args[1];

                // 检查循环引用
                if (context.IsTypeInStack(valueType))
                {
                    return new JObject
                    {
                        ["type"] = "object",
                        ["additionalProperties"] = new JObject
                        {
                            ["$ref"] = $"#/$defs/{valueType.Name}",
                            ["description"] = "Circular reference"
                        }
                    };
                }

                // 使用 TypeSchemaGenerator 获取值类型的 Schema
                var valueSchema = GetTypeSchema(valueType, context);
                return new JObject
                {
                    ["type"] = "object",
                    ["additionalProperties"] = valueSchema
                };
            }

            return new JObject { ["type"] = "object" };
        }

        /// <summary>
        /// 获取类型 Schema - 使用生成器
        /// </summary>
        private static JObject? GetTypeSchema(Type type, SchemaContext context)
        {
            // 使用 TypeSchemaGenerator 生成
            var generator = new TypeSchemaGenerator();
            return generator.Generate(type, context);
        }
    }
}