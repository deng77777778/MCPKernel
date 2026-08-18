// SchemaTemplates.cs
#nullable enable
using Newtonsoft.Json.Linq;
using System;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// 预编译 Schema 模板
    /// </summary>
    internal static class SchemaTemplates
    {
        // 预创建的模板
        private static readonly JObject StringSchema = new() { ["type"] = "string" };
        private static readonly JObject BooleanSchema = new() { ["type"] = "boolean" };
        private static readonly JObject IntegerSchema = new() { ["type"] = "integer" };
        private static readonly JObject NumberSchema = new() { ["type"] = "number" };
        private static readonly JObject ObjectSchema = new() { ["type"] = "object" };
        private static readonly JObject NullSchema = new() { ["type"] = "null" };

        // 格式化的模板
        private static readonly JObject DateTimeSchema = new()
        {
            ["type"] = "string",
            ["format"] = "date-time"
        };
        private static readonly JObject GuidSchema = new()
        {
            ["type"] = "string",
            ["format"] = "uuid"
        };
        private static readonly JObject UriSchema = new()
        {
            ["type"] = "string",
            ["format"] = "uri"
        };
        private static readonly JObject DurationSchema = new()
        {
            ["type"] = "string",
            ["format"] = "duration"
        };

        /// <summary>
        /// 获取基本类型的 Schema
        /// </summary>
        public static JObject GetPrimitiveSchema(Type type)
        {
            if (type == typeof(string) || type == typeof(char))
                return Clone(StringSchema);
            if (type == typeof(bool))
                return Clone(BooleanSchema);
            if (type == typeof(byte) || type == typeof(sbyte) ||
                type == typeof(short) || type == typeof(ushort) ||
                type == typeof(int) || type == typeof(uint) ||
                type == typeof(long) || type == typeof(ulong) ||
                type == typeof(nint) || type == typeof(nuint))
                return Clone(IntegerSchema);
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
                return Clone(NumberSchema);
            if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
                return Clone(DateTimeSchema);
            if (type == typeof(Guid))
                return Clone(GuidSchema);
            if (type == typeof(Uri))
                return Clone(UriSchema);
            if (type == typeof(TimeSpan))
                return Clone(DurationSchema);
            if (type == typeof(Version))
                return new JObject { ["type"] = "string", ["pattern"] = @"^\d+(\.\d+){0,3}$" };

            return Clone(ObjectSchema);
        }

        /// <summary>
        /// 获取简单类型模板
        /// </summary>
        public static JObject GetSimpleSchema(string type)
        {
            return type switch
            {
                "string" => Clone(StringSchema),
                "boolean" => Clone(BooleanSchema),
                "integer" => Clone(IntegerSchema),
                "number" => Clone(NumberSchema),
                "object" => Clone(ObjectSchema),
                "null" => Clone(NullSchema),
                "date-time" => Clone(DateTimeSchema),
                "uuid" => Clone(GuidSchema),
                "uri" => Clone(UriSchema),
                "duration" => Clone(DurationSchema),
                _ => Clone(ObjectSchema)
            };
        }

        /// <summary>
        /// 克隆模板
        /// </summary>
        public static JObject Clone(JObject template)
        {
            if (template == null) return new JObject();

            // 对于简单模板，手动创建比 DeepClone 更快
            if (ReferenceEquals(template, StringSchema))
                return new JObject { ["type"] = "string" };
            if (ReferenceEquals(template, BooleanSchema))
                return new JObject { ["type"] = "boolean" };
            if (ReferenceEquals(template, IntegerSchema))
                return new JObject { ["type"] = "integer" };
            if (ReferenceEquals(template, NumberSchema))
                return new JObject { ["type"] = "number" };
            if (ReferenceEquals(template, ObjectSchema))
                return new JObject { ["type"] = "object" };
            if (ReferenceEquals(template, NullSchema))
                return new JObject { ["type"] = "null" };
            if (ReferenceEquals(template, DateTimeSchema))
                return new JObject { ["type"] = "string", ["format"] = "date-time" };
            if (ReferenceEquals(template, GuidSchema))
                return new JObject { ["type"] = "string", ["format"] = "uuid" };
            if (ReferenceEquals(template, UriSchema))
                return new JObject { ["type"] = "string", ["format"] = "uri" };
            if (ReferenceEquals(template, DurationSchema))
                return new JObject { ["type"] = "string", ["format"] = "duration" };

            // 复杂模板使用 DeepClone
            return (JObject)template.DeepClone();
        }

        /// <summary>
        /// 创建枚举 Schema
        /// </summary>
        public static JObject CreateEnumSchema(string[] names, string? description = null)
        {
            var schema = new JObject
            {
                ["type"] = "string",
                ["enum"] = new JArray(names)
            };

            if (!string.IsNullOrEmpty(description))
                schema["description"] = description;

            return schema;
        }

        /// <summary>
        /// 创建对象 Schema
        /// </summary>
        public static JObject CreateObjectSchema(string? description = null, bool additionalProperties = false)
        {
            var schema = new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject()
            };

            if (!string.IsNullOrEmpty(description))
                schema["description"] = description;

            schema["additionalProperties"] = additionalProperties;

            return schema;
        }

        /// <summary>
        /// 创建数组 Schema
        /// </summary>
        public static JObject CreateArraySchema(JObject? items = null)
        {
            var schema = new JObject { ["type"] = "array" };
            if (items != null)
                schema["items"] = items;
            return schema;
        }

        /// <summary>
        /// 创建字典 Schema
        /// </summary>
        public static JObject CreateDictionarySchema(JObject additionalProperties)
        {
            return new JObject
            {
                ["type"] = "object",
                ["additionalProperties"] = additionalProperties
            };
        }
    }
}