#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MCP.AI
{
    /// <summary>
    /// Represents the response to an <see cref="InputRequestContent"/>.
    /// </summary>
    [JsonConverter(typeof(InputResponseContentConverter))]
    public class InputResponseContent : AIContent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputResponseContent"/> class.
        /// </summary>
        /// <param name="requestId">The unique identifier that correlates this response with its corresponding request.</param>
        /// <exception cref="ArgumentNullException"><paramref name="requestId"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="requestId"/> is empty or composed entirely of whitespace.</exception>
        [JsonConstructor]
        protected InputResponseContent(string requestId)
        {
            RequestId = Throw.IfNullOrWhitespace(requestId);
        }

        /// <summary>
        /// Gets the unique identifier that correlates this response with its corresponding <see cref="InputRequestContent"/>.
        /// </summary>
        [JsonProperty("requestId", Required = Required.Always)]
        public string RequestId { get; }
    }

    /// <summary>
    /// Custom JSON converter for polymorphic InputResponseContent serialization/deserialization.
    /// </summary>
    public class InputResponseContentConverter : JsonConverter<InputResponseContent>
    {
        private static readonly Dictionary<string, Type> TypeMapping = new(StringComparer.OrdinalIgnoreCase)
        {
            ["toolApprovalResponse"] = typeof(ToolApprovalResponseContent)
        };

        private static readonly Dictionary<Type, string> ReverseTypeMapping = new()
        {
            [typeof(ToolApprovalResponseContent)] = "toolApprovalResponse"
        };

        public override InputResponseContent? ReadJson(JsonReader reader, Type objectType, InputResponseContent? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var jsonObject = JObject.Load(reader);

            // 尝试从 $type 或 type 属性获取类型鉴别器
            string? typeDiscriminator = null;

            if (jsonObject.TryGetValue("$type", out JToken? typeToken))
            {
                typeDiscriminator = typeToken.Value<string>();
            }
            else if (jsonObject.TryGetValue("type", out JToken? typeToken2))
            {
                typeDiscriminator = typeToken2.Value<string>();
            }

            // 如果有类型鉴别器且匹配已知类型，反序列化为对应的派生类
            if (typeDiscriminator != null && TypeMapping.TryGetValue(typeDiscriminator, out Type? targetType))
            {
                return (InputResponseContent?)jsonObject.ToObject(targetType, serializer);
            }

            // 默认反序列化为基类
            return jsonObject.ToObject<InputResponseContent>(serializer);
        }

        public override void WriteJson(JsonWriter writer, InputResponseContent? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            // 获取类型的鉴别器名称
            Type valueType = value.GetType();
            if (!ReverseTypeMapping.TryGetValue(valueType, out string? typeDiscriminator))
            {
                throw new NotSupportedException($"Unknown InputResponseContent type: {valueType.Name}");
            }

            // 先序列化对象为 JObject
            var jsonObject = JObject.FromObject(value, serializer);

            // 添加类型鉴别器（使用 $type 以保持与 System.Text.Json 兼容）
            jsonObject.AddFirst(new JProperty("$type", typeDiscriminator));

            jsonObject.WriteTo(writer);
        }
    }

}