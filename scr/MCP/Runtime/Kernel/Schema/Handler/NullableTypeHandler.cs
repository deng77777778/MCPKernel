// Handlers/NullableTypeHandler.cs
#nullable enable
using System;
using Newtonsoft.Json.Linq;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// 可空类型处理器
    /// </summary>
    public class NullableTypeHandler : ITypeSchemaHandler
    {
        public int Priority => 95;

        public bool CanHandle(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        public JObject GenerateSchema(Type type, SchemaContext context)
        {
            var underlyingType = Nullable.GetUnderlyingType(type)!;
            var handler = TypeHandlerRegistry.GetHandler(underlyingType);
            var schema = handler.GenerateSchema(underlyingType, context);

            // 添加 null 支持
            if (schema["type"] is JValue jv && jv.Type == JTokenType.String)
            {
                schema["type"] = new JArray { (string?)jv, "null" };
            }
            else if (schema["type"] == null)
            {
                schema["type"] = new JArray { "string", "null" };
            }

            return schema;
        }
    }
}