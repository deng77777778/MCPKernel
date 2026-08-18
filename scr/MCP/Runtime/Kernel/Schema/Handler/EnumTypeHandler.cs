// Handlers/EnumTypeHandler.cs
#nullable enable
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Reflection;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// 枚举类型处理器
    /// </summary>
    public class EnumTypeHandler : ITypeSchemaHandler
    {
        public int Priority => 90;

        public bool CanHandle(Type type) => type.IsEnum;

        public JObject GenerateSchema(Type type, SchemaContext context)
        {
            var names = Enum.GetNames(type);
            var desc = type.GetCustomAttribute<DescriptionAttribute>(true)?.Description;

            var schema = new JObject
            {
                ["type"] = "string",
                ["enum"] = new JArray(names)
            };

            if (!string.IsNullOrEmpty(desc))
                schema["description"] = desc;

            // 添加枚举描述
            var enumDescriptions = new JObject();
            foreach (var value in Enum.GetValues(type))
            {
                var field = type.GetField(value.ToString()!);
                var fieldDesc = field?.GetCustomAttribute<DescriptionAttribute>(true)?.Description;
                if (!string.IsNullOrEmpty(fieldDesc))
                {
                    enumDescriptions[value.ToString()!] = fieldDesc;
                }
            }
            if (enumDescriptions.HasValues)
                schema["x-enum-descriptions"] = enumDescriptions;

            return schema;
        }
    }
}