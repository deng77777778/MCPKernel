#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MCP.AI
{
    /// <summary>
    /// Represents the result of a tool call.
    /// </summary>
    [JsonConverter(typeof(ToolResultContentConverter))]
    public class ToolResultContent : AIContent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToolResultContent"/> class.
        /// </summary>
        /// <param name="callId">The tool call ID for which this is the result.</param>
        /// <exception cref="ArgumentNullException"><paramref name="callId"/> is <see langword="null"/>.</exception>
        [JsonConstructor]
        public ToolResultContent(string callId)
        {
            CallId = Throw.IfNull(callId);
        }

        /// <summary>
        /// Gets the ID of the tool call for which this is the result.
        /// </summary>
        /// <remarks>
        /// If this is the result for a <see cref="ToolCallContent"/>, this property should contain the same
        /// <see cref="ToolCallContent.CallId"/> value.
        /// </remarks>
        [JsonProperty("callId", Required = Required.Always)]
        public string CallId { get; }
    }

    public class ToolResultContentConverter : JsonConverter<ToolResultContent>
    {
        private static readonly Dictionary<string, Type> TypeMapping = new(StringComparer.Ordinal)
        {
            ["functionResult"] = typeof(FunctionResultContent),
            ["mcpServerToolResult"] = typeof(McpServerToolResultContent),
            ["imageGenerationToolResult"] = typeof(ImageGenerationToolResultContent),
            ["codeInterpreterToolResult"] = typeof(CodeInterpreterToolResultContent),
            ["webSearchToolResult"] = typeof(WebSearchToolResultContent)
        };

        private static readonly Dictionary<Type, string> ReverseTypeMapping = new()
        {
            [typeof(FunctionResultContent)] = "functionResult",
            [typeof(McpServerToolResultContent)] = "mcpServerToolResult",
            [typeof(ImageGenerationToolResultContent)] = "imageGenerationToolResult",
            [typeof(CodeInterpreterToolResultContent)] = "codeInterpreterToolResult",
            [typeof(WebSearchToolResultContent)] = "webSearchToolResult"
        };

        private const string TypeDiscriminatorPropertyName = "$type";

        public override bool CanWrite => true;

        public override ToolResultContent? ReadJson(JsonReader reader, Type objectType, ToolResultContent? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var jsonObject = JObject.Load(reader);

            // 获取类型鉴别器
            var typeDiscriminator = jsonObject[TypeDiscriminatorPropertyName]?.ToString();

            if (string.IsNullOrEmpty(typeDiscriminator))
            {
                return InferAndDeserialize(jsonObject, serializer);
            }

            if (!TypeMapping.TryGetValue(typeDiscriminator, out var targetType))
            {
                return InferAndDeserialize(jsonObject, serializer);
            }

            using var subReader = jsonObject.CreateReader();
            return (ToolResultContent?)serializer.Deserialize(subReader, targetType);
        }

        public override void WriteJson(JsonWriter writer, ToolResultContent? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            // 获取类型的鉴别器名称
            Type valueType = value.GetType();
            if (!ReverseTypeMapping.TryGetValue(valueType, out string? typeDiscriminator))
            {
                if (!TryGetBaseTypeDiscriminator(valueType, out typeDiscriminator))
                {
                    throw new NotSupportedException($"Unknown ToolResultContent type: {valueType.Name}");
                }
            }

            // 开始写入对象
            writer.WriteStartObject();

            // 先写入 $type 鉴别器（放在最前面）
            writer.WritePropertyName(TypeDiscriminatorPropertyName);
            writer.WriteValue(typeDiscriminator);

            // 获取对象的契约信息
            var contract = serializer.ContractResolver.ResolveContract(valueType) as Newtonsoft.Json.Serialization.JsonObjectContract;
            if (contract != null)
            {
                // 写入所有可读的属性
                foreach (var property in contract.Properties)
                {
                    if (property.Ignored || !property.Readable)
                        continue;

                    // 检查是否有 JsonIgnore 属性
                    var attributes = property.AttributeProvider?.GetAttributes(typeof(JsonIgnoreAttribute), true);
                    var hasJsonIgnore = attributes.Any();
                    if (hasJsonIgnore)
                        continue;

                    // 获取属性值
                    var propertyValue = property.ValueProvider?.GetValue(value);

                    // 跳过 null 值（如果设置了 NullValueHandling.Ignore）
                    if (propertyValue == null && (serializer.NullValueHandling == NullValueHandling.Ignore))
                        continue;

                    // 写入属性名和值
                    var propertyName = property.PropertyName ?? property.UnderlyingName;
                    if (propertyName is not null)
                        writer.WritePropertyName(propertyName);
                    serializer.Serialize(writer, propertyValue);
                }

                // 处理 AdditionalProperties（如果有）
                if (value is ToolResultContent toolResult && toolResult.AdditionalProperties != null)
                {
                    foreach (var kvp in toolResult.AdditionalProperties)
                    {
                        writer.WritePropertyName(kvp.Key);
                        serializer.Serialize(writer, kvp.Value);
                    }
                }

                // 处理 Annotations（如果有）
                if (value is ToolResultContent toolResultWithAnnotations && toolResultWithAnnotations.Annotations != null)
                {
                    // 如果 Annotations 没有被序列化，需要在这里处理
                    // 注意：如果 AIContent 的 Annotations 属性有 JsonProperty 属性，它会被上面的循环处理
                }
            }

            writer.WriteEndObject();
        }

        private static bool TryGetBaseTypeDiscriminator(Type type, out string? discriminator)
        {
            discriminator = null;

            if (typeof(ToolResultContent).IsAssignableFrom(type) && type != typeof(ToolResultContent))
            {
                discriminator = "toolResult";
                return true;
            }

            return false;
        }

        private static ToolResultContent? InferAndDeserialize(JObject jsonObject, JsonSerializer serializer)
        {
            using var reader = jsonObject.CreateReader();
            // 如果包含 CallId 和 Result 属性，推断为 FunctionResultContent
            if (jsonObject.ContainsKey("callId") && jsonObject.ContainsKey("result"))
            {
                return serializer.Deserialize<FunctionResultContent>(reader);
            }

            // 如果包含 CallId 和 Outputs 属性
            if (jsonObject.ContainsKey("callId") && jsonObject.ContainsKey("outputs"))
            {
                // 检查是否有 serverName 属性来判断是否为 McpServerToolResultContent
                if (jsonObject.ContainsKey("serverName"))
                {
                    return serializer.Deserialize<McpServerToolResultContent>(reader);
                }

                return serializer.Deserialize<ToolResultContent>(reader);
            }

            // 如果包含 CallId 属性但不知道具体类型，使用 ToolResultContent
            if (jsonObject.ContainsKey("callId"))
            {
                return serializer.Deserialize<ToolResultContent>(reader);
            }

            // 默认使用基类型
            using var defaultReader = jsonObject.CreateReader();
            return serializer.Deserialize<ToolResultContent>(defaultReader);
        }
    }
}