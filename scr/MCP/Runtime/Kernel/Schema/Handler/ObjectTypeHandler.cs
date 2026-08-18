// Handlers/ObjectTypeHandler.cs
#nullable enable
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Reflection;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// 对象类型处理器（兜底）
    /// </summary>
    public class ObjectTypeHandler : ITypeSchemaHandler
    {
        public int Priority => 10;

        public bool CanHandle(Type type) => true;

        public JObject GenerateSchema(Type type, SchemaContext context)
        {
            var builder = new SchemaBuilder()
                .Type("object")
                .AdditionalProperties(false);

            var desc = type.GetCustomAttribute<DescriptionAttribute>(true)?.Description;
            if (!string.IsNullOrEmpty(desc))
                builder.Description(desc);

            // 处理属性
            foreach (var prop in TypeHelper.GetSerializableProperties(type))
            {
                var propType = prop.PropertyType;
                var propName = NameHelper.ToSnakeCase(prop.Name);

                // 检查循环引用
                if (context.IsTypeInStack(propType))
                {
                    builder.Property(propName, new JObject
                    {
                        ["$ref"] = $"#/$defs/{propType.Name}",
                        ["description"] = "Circular reference"
                    }, false);
                    continue;
                }

                var propSchema = GetTypeSchema(propType, context);

                var propDesc = prop.GetCustomAttribute<DescriptionAttribute>(true)?.Description;
                if (!string.IsNullOrEmpty(propDesc) && propSchema is not null)
                    propSchema["description"] = propDesc;

                var isRequired = TypeHelper.IsValueType(propType) &&
                                 Nullable.GetUnderlyingType(propType) == null;

                if (propSchema is not null)
                    builder.Property(propName, propSchema, isRequired);
            }

            // 处理字段
            foreach (var field in TypeHelper.GetSerializableFields(type))
            {
                var fieldType = field.FieldType;
                var fieldName = NameHelper.ToSnakeCase(field.Name);

                if (context.IsTypeInStack(fieldType))
                {
                    builder.Property(fieldName, new JObject
                    {
                        ["$ref"] = $"#/$defs/{fieldType.Name}",
                        ["description"] = "Circular reference"
                    }, false);
                    continue;
                }

                var fieldSchema = GetTypeSchema(fieldType, context);

                var fieldDesc = field.GetCustomAttribute<DescriptionAttribute>(true)?.Description;
                if (!string.IsNullOrEmpty(fieldDesc) && fieldSchema is not null)
                    fieldSchema["description"] = fieldDesc;

                var isRequired = TypeHelper.IsValueType(fieldType) &&
                                 Nullable.GetUnderlyingType(fieldType) == null;

                if (fieldSchema is not null)
                    builder.Property(fieldName, fieldSchema, isRequired);
            }

            return builder.Build();
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