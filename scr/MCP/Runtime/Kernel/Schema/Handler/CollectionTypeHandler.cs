// Handlers/CollectionTypeHandler.cs
#nullable enable
using Newtonsoft.Json.Linq;
using System;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// 集合类型处理器
    /// </summary>
    public class CollectionTypeHandler : ITypeSchemaHandler
    {
        public int Priority => 80;

        public bool CanHandle(Type type)
        {
            return TypeHelper.IsCollectionType(type) && !TypeHelper.IsDictionaryType(type);
        }

        public JObject GenerateSchema(Type type, SchemaContext context)
        {
            var elementType = TypeHelper.GetElementType(type);

            // 检查循环引用
            if (elementType != null && context.IsTypeInStack(elementType))
            {
                return new JObject
                {
                    ["type"] = "array",
                    ["items"] = new JObject
                    {
                        ["$ref"] = $"#/$defs/{elementType.Name}",
                        ["description"] = "Circular reference"
                    }
                };
            }

            var items = elementType != null && elementType != typeof(object)
                ? GetTypeSchema(elementType, context)
                : null;

            var schema = new JObject { ["type"] = "array" };
            if (items != null)
                schema["items"] = items;

            return schema;
        }

        /// <summary>
        /// 获取类型 Schema - 使用生成器
        /// </summary>
        private static JObject? GetTypeSchema(Type type, SchemaContext context)
        {
            var generator = new TypeSchemaGenerator();
            return generator.Generate(type, context);
        }
    }
}