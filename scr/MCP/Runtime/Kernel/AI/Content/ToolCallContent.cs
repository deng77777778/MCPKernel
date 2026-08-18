#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MCP.AI
{
    /// <summary>
    /// Represents a tool call request.
    /// </summary>
    [JsonConverter(typeof(ToolCallContentConverter))]  // 自定义转换器处理多态
    public class ToolCallContent : AIContent  // 保持非抽象
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToolCallContent"/> class.
        /// </summary>
        /// <param name="callId">The tool call ID.</param>
        /// <exception cref="ArgumentNullException"><paramref name="callId"/> is <see langword="null"/>.</exception>
        [JsonConstructor]
        public ToolCallContent(string callId)
        {
            CallId = Throw.IfNull(callId);
        }

        /// <summary>
        /// Gets the tool call ID.
        /// </summary>
        [JsonProperty("callId", Required = Required.Always)]
        public string CallId { get; }
    }

    /// <summary>
    /// Custom JSON converter for polymorphic ToolCallContent serialization.
    /// </summary>
    public class ToolCallContentConverter : JsonConverter
    {
        private static readonly Dictionary<string, Type> TypeMapping = new()
        {
            ["functionCall"] = typeof(FunctionCallContent),
            ["mcpServerToolCall"] = typeof(McpServerToolCallContent),
            ["imageGenerationToolCall"] = typeof(ImageGenerationToolCallContent),
            ["codeInterpreterToolCall"] = typeof(CodeInterpreterToolCallContent),
            ["webSearchToolCall"] = typeof(WebSearchToolCallContent)
        };

        public override bool CanConvert(Type objectType) =>
            typeof(ToolCallContent).IsAssignableFrom(objectType);

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var jsonObject = JObject.Load(reader);

            // 使用 $type 或自定义属性作为鉴别器
            if (jsonObject.TryGetValue("$type", out JToken? typeToken))
            {
                string? typeDiscriminator = typeToken.Value<string>();

                if (typeDiscriminator != null && TypeMapping.TryGetValue(typeDiscriminator, out Type? targetType))
                {
                    return jsonObject.ToObject(targetType, serializer);
                }
            }

            // 默认反序列化为基类
            return jsonObject.ToObject<ToolCallContent>(serializer);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var jsonObject = JObject.FromObject(value, serializer);

            // 添加类型鉴别器
            string typeDiscriminator = GetTypeDiscriminator(value.GetType());
            jsonObject.AddFirst(new JProperty("$type", typeDiscriminator));

            jsonObject.WriteTo(writer);
        }

        private static string GetTypeDiscriminator(Type type)
        {
            return type.Name switch
            {
                nameof(FunctionCallContent) => "functionCall",
                nameof(McpServerToolCallContent) => "mcpServerToolCall",
                nameof(ImageGenerationToolCallContent) => "imageGenerationToolCall",
                nameof(CodeInterpreterToolCallContent) => "codeInterpreterToolCall",
                nameof(WebSearchToolCallContent) => "webSearchToolCall",
                _ => throw new NotSupportedException($"Unknown type: {type.Name}")
            };
        }
    }
}