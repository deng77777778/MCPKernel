// Helpers/SchemaTransformer.cs
#nullable enable
using MCP.AI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// Schema 转换器
    /// </summary>
    public static class SchemaTransformer
    {
        public static JToken Transform(JToken schema, AIJsonSchemaTransformOptions transformOptions)
        {
            if (transformOptions == null)
                throw new ArgumentNullException(nameof(transformOptions));

            if (!transformOptions.HasTransformations)
                return schema.DeepClone();

            var cloned = schema.DeepClone();
            var path = transformOptions.TransformSchemaNode != null ? new List<string>() : null;

            return TransformCore(cloned, transformOptions, path);
        }

        private static JToken TransformCore(JToken? schema, AIJsonSchemaTransformOptions options, List<string>? path)
        {
            if (schema == null)
                return JValue.CreateNull();

            switch (schema.Type)
            {
                case JTokenType.Boolean:
                    return TransformBoolean((bool)schema, options);

                case JTokenType.Object:
                    return TransformObject((JObject)schema, options, path);

                default:
                    return schema;
            }
        }

        private static JToken TransformBoolean(bool value, AIJsonSchemaTransformOptions options)
        {
            if (!options.ConvertBooleanSchemas)
                return value;

            if (!value)
            {
                return new JObject { ["not"] = true };
            }
            return new JObject();
        }

        private static JToken TransformObject(JObject obj, AIJsonSchemaTransformOptions options, List<string>? path)
        {
            JObject? properties = null;

            // 递归处理子节点
            if (obj.TryGetValue("properties", out var propsToken) && propsToken is JObject propsObj)
            {
                properties = propsObj;
                path?.Add("properties");

                var keys = propsObj.Properties().Select(p => p.Name).ToList();
                foreach (var key in keys)
                {
                    path?.Add(key);
                    propsObj[key] = TransformCore(propsObj[key], options, path);
                    path?.RemoveAt(path.Count - 1);
                }

                path?.RemoveAt(path.Count - 1);
            }

            // items
            if (obj.TryGetValue("items", out var itemsToken))
            {
                path?.Add("items");
                obj["items"] = TransformCore(itemsToken, options, path);
                path?.RemoveAt(path.Count - 1);
            }

            // additionalProperties
            if (obj.TryGetValue("additionalProperties", out var addPropsToken) &&
                (addPropsToken.Type != JTokenType.Boolean || (bool)addPropsToken != false))
            {
                path?.Add("additionalProperties");
                obj["additionalProperties"] = TransformCore(addPropsToken, options, path);
                path?.RemoveAt(path.Count - 1);
            }

            // 组合关键字
            foreach (var keyword in new[] { "anyOf", "oneOf", "allOf" })
            {
                if (obj.TryGetValue(keyword, out var arrToken) && arrToken is JArray arr)
                {
                    path?.Add(keyword);
                    for (int i = 0; i < arr.Count; i++)
                    {
                        path?.Add($"[{i}]");
                        arr[i] = TransformCore(arr[i], options, path);
                        path?.RemoveAt(path.Count - 1);
                    }
                    path?.RemoveAt(path.Count - 1);
                }
            }

            // 节点级转换
            ApplyNodeTransformations(obj, options, properties);

            // 用户自定义转换
            if (options.TransformSchemaNode != null)
            {
                var context = new AIJsonSchemaTransformContext(path?.ToArray() ?? Array.Empty<string>());
                obj = options.TransformSchemaNode(context, obj) as JObject ?? obj;
            }

            return obj;
        }

        private static void ApplyNodeTransformations(JObject obj, AIJsonSchemaTransformOptions options, JObject? properties)
        {
            // additionalProperties: false
            if (options.DisallowAdditionalProperties && properties != null && !obj.ContainsKey("additionalProperties"))
            {
                obj["additionalProperties"] = false;
            }

            // required all properties
            if (options.RequireAllProperties && properties != null)
            {
                var required = new JArray();
                foreach (var prop in properties.Properties())
                {
                    required.Add(prop.Name);
                }
                obj["required"] = required;
            }

            // nullable keyword
            if (options.UseNullableKeyword && obj.TryGetValue("type", out var typeToken) && typeToken is JArray typeArray)
            {
                ApplyNullableKeyword(obj, typeArray);
            }

            // move default to description
            if (options.MoveDefaultKeywordToDescription && obj.TryGetValue("default", out var defaultToken))
            {
                MoveDefaultToDescription(obj, defaultToken);
            }
        }

        private static void ApplyNullableKeyword(JObject obj, JArray typeArray)
        {
            bool isNullable = false;
            string? foundType = null;

            foreach (var node in typeArray)
            {
                var typeStr = node.Value<string>();
                if (typeStr == "null")
                {
                    isNullable = true;
                    continue;
                }

                if (foundType != null)
                {
                    foundType = null;
                    break;
                }
                foundType = typeStr;
            }

            if (isNullable && foundType != null)
            {
                obj["type"] = foundType;
                obj["nullable"] = true;
            }
        }

        private static void MoveDefaultToDescription(JObject obj, JToken defaultToken)
        {
            var desc = obj.TryGetValue("description", out var descToken)
                ? descToken.Value<string>()
                : null;

            var defaultJson = defaultToken.ToString(Newtonsoft.Json.Formatting.None);
            desc = desc == null
                ? $"Default value: {defaultJson}"
                : $"{desc} (Default value: {defaultJson})";

            obj["description"] = desc;
            obj.Remove("default");
        }
    }
}