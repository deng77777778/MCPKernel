// Builders/SchemaBuilder.cs
#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// Schema 流式构建器
    /// </summary>
    public class SchemaBuilder
    {
        private readonly JObject _schema;
        private JObject _properties;
        private JArray _required;
        private JObject _definitions;
        private JObject _dependencies;

        public SchemaBuilder()
        {
            _schema = new JObject();
            _properties = new JObject();
            _required = new JArray();
            _definitions = new JObject();
            _dependencies = new JObject();
        }

        #region 基础类型

        public SchemaBuilder Type(string type)
        {
            _schema["type"] = type;
            return this;
        }

        public SchemaBuilder Title(string? title)
        {
            if (!string.IsNullOrEmpty(title))
                _schema["title"] = title;
            return this;
        }

        public SchemaBuilder Description(string? description)
        {
            if (!string.IsNullOrEmpty(description))
                _schema["description"] = description;
            return this;
        }

        public SchemaBuilder Format(string? format)
        {
            if (!string.IsNullOrEmpty(format))
                _schema["format"] = format;
            return this;
        }

        public SchemaBuilder Pattern(string? pattern)
        {
            if (!string.IsNullOrEmpty(pattern))
                _schema["pattern"] = pattern;
            return this;
        }

        public SchemaBuilder Default(object? defaultValue)
        {
            if (defaultValue != null)
                _schema["default"] = JToken.FromObject(defaultValue);
            return this;
        }

        public SchemaBuilder Enum(params string[] values)
        {
            if (values.Length > 0)
                _schema["enum"] = new JArray(values);
            return this;
        }

        public SchemaBuilder Enum<T>(params T[] values) where T : Enum
        {
            var arr = new JArray();
            foreach (var v in values)
                arr.Add(v.ToString());
            _schema["enum"] = arr;
            return this;
        }

        #endregion

        #region 对象类型

        public SchemaBuilder Object(string? description = null)
        {
            Type("object");
            if (!string.IsNullOrEmpty(description))
                Description(description);
            return this;
        }

        public SchemaBuilder Property(string name, JObject schema, bool required = false)
        {
            _properties[name] = schema;
            if (required)
                _required.Add(name);
            return this;
        }

        public SchemaBuilder Property(string name, Action<SchemaBuilder> configure, bool required = false)
        {
            var builder = new SchemaBuilder();
            configure(builder);
            return Property(name, builder.Build(), required);
        }

        public SchemaBuilder Properties(IEnumerable<KeyValuePair<string, JObject>> properties)
        {
            foreach (var prop in properties)
            {
                _properties[prop.Key] = prop.Value;
            }
            return this;
        }

        public SchemaBuilder Required(params string[] names)
        {
            foreach (var name in names)
            {
                if (!_required.Contains(name))
                    _required.Add(name);
            }
            return this;
        }

        public SchemaBuilder AdditionalProperties(bool allow)
        {
            _schema["additionalProperties"] = allow;
            return this;
        }

        public SchemaBuilder AdditionalProperties(JObject schema)
        {
            _schema["additionalProperties"] = schema;
            return this;
        }

        #endregion

        #region 数组类型

        public SchemaBuilder Array(string? description = null)
        {
            Type("array");
            if (!string.IsNullOrEmpty(description))
                Description(description);
            return this;
        }

        public SchemaBuilder Items(JObject schema)
        {
            _schema["items"] = schema;
            return this;
        }

        public SchemaBuilder Items(Action<SchemaBuilder> configure)
        {
            var builder = new SchemaBuilder();
            configure(builder);
            return Items(builder.Build());
        }

        public SchemaBuilder MinItems(int min)
        {
            _schema["minItems"] = min;
            return this;
        }

        public SchemaBuilder MaxItems(int max)
        {
            _schema["maxItems"] = max;
            return this;
        }

        #endregion

        #region 数字类型

        public SchemaBuilder Integer(string? description = null)
        {
            Type("integer");
            if (!string.IsNullOrEmpty(description))
                Description(description);
            return this;
        }

        public SchemaBuilder Number(string? description = null)
        {
            Type("number");
            if (!string.IsNullOrEmpty(description))
                Description(description);
            return this;
        }

        public SchemaBuilder Minimum(double min, bool exclusive = false)
        {
            _schema[exclusive ? "exclusiveMinimum" : "minimum"] = min;
            return this;
        }

        public SchemaBuilder Maximum(double max, bool exclusive = false)
        {
            _schema[exclusive ? "exclusiveMaximum" : "maximum"] = max;
            return this;
        }

        public SchemaBuilder MultipleOf(double multiple)
        {
            _schema["multipleOf"] = multiple;
            return this;
        }

        #endregion

        #region 字符串类型

        public SchemaBuilder String(string? description = null)
        {
            Type("string");
            if (!string.IsNullOrEmpty(description))
                Description(description);
            return this;
        }

        public SchemaBuilder MinLength(int min)
        {
            _schema["minLength"] = min;
            return this;
        }

        public SchemaBuilder MaxLength(int max)
        {
            _schema["maxLength"] = max;
            return this;
        }

        #endregion

        #region 布尔类型

        public SchemaBuilder Boolean(string? description = null)
        {
            Type("boolean");
            if (!string.IsNullOrEmpty(description))
                Description(description);
            return this;
        }

        #endregion

        #region 引用和定义

        public SchemaBuilder Ref(string refPath)
        {
            _schema["$ref"] = refPath;
            return this;
        }

        public SchemaBuilder Definition(string name, JObject schema)
        {
            _definitions[name] = schema;
            return this;
        }

        public SchemaBuilder Definition(string name, Action<SchemaBuilder> configure)
        {
            var builder = new SchemaBuilder();
            configure(builder);
            return Definition(name, builder.Build());
        }

        public SchemaBuilder Definitions(IEnumerable<KeyValuePair<string, JObject>> definitions)
        {
            foreach (var def in definitions)
            {
                _definitions[def.Key] = def.Value;
            }
            return this;
        }

        #endregion

        #region 组合和条件

        public SchemaBuilder AllOf(params JObject[] schemas)
        {
            _schema["allOf"] = new JArray(schemas);
            return this;
        }

        public SchemaBuilder AnyOf(params JObject[] schemas)
        {
            _schema["anyOf"] = new JArray(schemas);
            return this;
        }

        public SchemaBuilder OneOf(params JObject[] schemas)
        {
            _schema["oneOf"] = new JArray(schemas);
            return this;
        }

        public SchemaBuilder Not(JObject schema)
        {
            _schema["not"] = schema;
            return this;
        }

        public SchemaBuilder If(JObject schema)
        {
            _schema["if"] = schema;
            return this;
        }

        public SchemaBuilder Then(JObject schema)
        {
            _schema["then"] = schema;
            return this;
        }

        public SchemaBuilder Else(JObject schema)
        {
            _schema["else"] = schema;
            return this;
        }

        #endregion

        #region 构建

        public JObject Build()
        {
            var result = (JObject)_schema.DeepClone();

            // 只添加非空的属性
            if (_properties.HasValues)
                result["properties"] = _properties;

            if (_required.HasValues)
                result["required"] = _required;

            if (_definitions.HasValues)
                result["$defs"] = _definitions;

            if (_dependencies.HasValues)
                result["dependencies"] = _dependencies;

            // 确保 type 字段存在
            if (!result.ContainsKey("type") && !result.ContainsKey("$ref") && !result.ContainsKey("allOf"))
                result["type"] = "object";

            return result;
        }

        public string BuildJson()
        {
            return Build().ToString(Newtonsoft.Json.Formatting.None);
        }

        public string BuildPrettyJson()
        {
            return Build().ToString(Newtonsoft.Json.Formatting.Indented);
        }

        #endregion

        #region 静态工厂方法

        public static SchemaBuilder Create() => new();

        public static SchemaBuilder ObjectSchema(string? description = null)
        {
            return new SchemaBuilder().Object(description);
        }

        public static SchemaBuilder ArraySchema(JObject? items = null, string? description = null)
        {
            var builder = new SchemaBuilder().Array(description);
            if (items != null)
                builder.Items(items);
            return builder;
        }

        public static SchemaBuilder StringSchema(string? format = null, string? description = null)
        {
            var builder = new SchemaBuilder().String(description);
            if (!string.IsNullOrEmpty(format))
                builder.Format(format);
            return builder;
        }

        public static SchemaBuilder IntegerSchema(string? description = null)
        {
            return new SchemaBuilder().Integer(description);
        }

        public static SchemaBuilder NumberSchema(string? description = null)
        {
            return new SchemaBuilder().Number(description);
        }

        public static SchemaBuilder BooleanSchema(string? description = null)
        {
            return new SchemaBuilder().Boolean(description);
        }

        public static SchemaBuilder EnumSchema<T>(string? description = null) where T : Enum
        {
            var builder = new SchemaBuilder().String(description);
            var values = System.Enum.GetNames(typeof(T));
            builder.Enum(values);
            return builder;
        }

        #endregion
    }
}