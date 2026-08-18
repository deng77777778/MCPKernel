#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;


namespace MCP.AI
{
    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.

    /// <summary>Represents content used by AI services.</summary>
    [JsonConverter(typeof(AIContentConverter))]
    public class AIContent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AIContent"/> class.
        /// </summary>
        public AIContent()
        {
        }

        /// <summary>
        /// Gets or sets a list of annotations on this content.
        /// </summary>
        [JsonProperty("annotations", NullValueHandling = NullValueHandling.Ignore)]
        public IList<AIAnnotation>? Annotations { get; set; }

        /// <summary>Gets or sets the raw representation of the content from an underlying implementation.</summary>
        /// <remarks>
        /// If an <see cref="AIContent"/> is created to represent some underlying object from another object
        /// model, this property can be used to store that original object. This can be useful for debugging or
        /// for enabling a consumer to access the underlying object model, if needed.
        /// </remarks>
        [JsonIgnore]
        public object? RawRepresentation { get; set; }

        /// <summary>Gets or sets additional properties for the content.</summary>
        [JsonProperty("additionalProperties", NullValueHandling = NullValueHandling.Ignore)]
        public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
    }

    /// <summary>
    /// Custom JSON converter for polymorphic AIContent serialization/deserialization.
    /// </summary>
    public class AIContentConverter : JsonConverter
    {
        private static readonly Dictionary<string, Type> TypeMapping = new(StringComparer.Ordinal)
        {
            ["data"] = typeof(DataContent),
            ["error"] = typeof(ErrorContent),
            ["functionCall"] = typeof(FunctionCallContent),
            ["functionResult"] = typeof(FunctionResultContent),
            ["hostedFile"] = typeof(HostedFileContent),
            ["hostedVectorStore"] = typeof(HostedVectorStoreContent),
            ["text"] = typeof(TextContent),
            ["reasoning"] = typeof(TextReasoningContent),
            ["uri"] = typeof(UriContent),
            ["usage"] = typeof(UsageContent),
            ["toolCall"] = typeof(ToolCallContent),
            ["toolResult"] = typeof(ToolResultContent),
            ["inputRequest"] = typeof(InputRequestContent),
            ["inputResponse"] = typeof(InputResponseContent),
            ["toolApprovalRequest"] = typeof(ToolApprovalRequestContent),
            ["toolApprovalResponse"] = typeof(ToolApprovalResponseContent),
            ["mcpServerToolCall"] = typeof(McpServerToolCallContent),
            ["mcpServerToolResult"] = typeof(McpServerToolResultContent),
            ["imageGenerationToolCall"] = typeof(ImageGenerationToolCallContent),
            ["imageGenerationToolResult"] = typeof(ImageGenerationToolResultContent),
            ["codeInterpreterToolCall"] = typeof(CodeInterpreterToolCallContent),
            ["codeInterpreterToolResult"] = typeof(CodeInterpreterToolResultContent),
            ["webSearchToolCall"] = typeof(WebSearchToolCallContent),
            ["webSearchToolResult"] = typeof(WebSearchToolResultContent)
        };

        private static readonly Dictionary<Type, string> ReverseTypeMapping = new()
        {
            [typeof(DataContent)] = "data",
            [typeof(ErrorContent)] = "error",
            [typeof(FunctionCallContent)] = "functionCall",
            [typeof(FunctionResultContent)] = "functionResult",
            [typeof(HostedFileContent)] = "hostedFile",
            [typeof(HostedVectorStoreContent)] = "hostedVectorStore",
            [typeof(TextContent)] = "text",
            [typeof(TextReasoningContent)] = "reasoning",
            [typeof(UriContent)] = "uri",
            [typeof(UsageContent)] = "usage",
            [typeof(ToolCallContent)] = "toolCall",
            [typeof(ToolResultContent)] = "toolResult",
            [typeof(InputRequestContent)] = "inputRequest",
            [typeof(InputResponseContent)] = "inputResponse",
            [typeof(ToolApprovalRequestContent)] = "toolApprovalRequest",
            [typeof(ToolApprovalResponseContent)] = "toolApprovalResponse",
            [typeof(McpServerToolCallContent)] = "mcpServerToolCall",
            [typeof(McpServerToolResultContent)] = "mcpServerToolResult",
            [typeof(ImageGenerationToolCallContent)] = "imageGenerationToolCall",
            [typeof(ImageGenerationToolResultContent)] = "imageGenerationToolResult",
            [typeof(CodeInterpreterToolCallContent)] = "codeInterpreterToolCall",
            [typeof(CodeInterpreterToolResultContent)] = "codeInterpreterToolResult",
            [typeof(WebSearchToolCallContent)] = "webSearchToolCall",
            [typeof(WebSearchToolResultContent)] = "webSearchToolResult"
        };

        public override bool CanConvert(Type objectType)
        {
            return typeof(AIContent).IsAssignableFrom(objectType);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var jsonObject = JObject.Load(reader);

            // 尝试从 $type 属性获取类型鉴别器
            if (jsonObject.TryGetValue("$type", out var typeToken))
            {
                string? typeDiscriminator = typeToken.Value<string>();
                if (typeDiscriminator != null && TypeMapping.TryGetValue(typeDiscriminator, out Type? targetType))
                {
                    using var subReader = jsonObject.CreateReader();
                    return serializer.Deserialize(subReader, targetType);
                }
            }

            // 如果没有鉴别器或未知类型，尝试从属性推断
            return InferAndDeserialize(jsonObject, serializer);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
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
                if (!TryGetBaseTypeDiscriminator(valueType, out typeDiscriminator))
                {
                    throw new NotSupportedException($"Unknown AIContent type: {valueType.Name}");
                }
            }

            // 关键：直接使用传入的 serializer 写入，而不是创建 JObject
            // 先写入对象的属性，然后添加 $type 字段
            writer.WriteStartObject();

            // 写入 $type 鉴别器
            writer.WritePropertyName("$type");
            writer.WriteValue(typeDiscriminator);

            // 获取对象的序列化属性
            var contract = serializer.ContractResolver.ResolveContract(valueType) as JsonObjectContract;
            if (contract != null)
            {
                foreach (var property in contract.Properties)
                {
                    if (property.Ignored || !property.Readable)
                        continue;

                    // 跳过 AdditionalProperties 等特殊处理
                    var propertyValue = property.ValueProvider?.GetValue(value);

                    // 检查是否有 JsonIgnore 属性
                    var attributes = property.AttributeProvider?.GetAttributes(typeof(JsonIgnoreAttribute), true);
                    if (attributes.Any())
                        continue;

                    var propertyName = property.PropertyName ?? property.UnderlyingName;
                    if (propertyName is not null)
                        writer.WritePropertyName(propertyName);
                    serializer.Serialize(writer, propertyValue);
                }

                // 处理 AdditionalProperties
                if (value is AIContent aiContent && aiContent.AdditionalProperties != null)
                {
                    foreach (var kvp in aiContent.AdditionalProperties)
                    {
                        writer.WritePropertyName(kvp.Key);
                        serializer.Serialize(writer, kvp.Value);
                    }
                }
            }

            writer.WriteEndObject();
        }
        /// <summary>
        /// 尝试获取基类型的鉴别器。
        /// </summary>
        private static bool TryGetBaseTypeDiscriminator(Type type, out string? discriminator)
        {
            discriminator = null;

            // 检查是否是 ToolCallContent 的派生类
            if (typeof(ToolCallContent).IsAssignableFrom(type) && type != typeof(ToolCallContent))
            {
                discriminator = "toolCall";
                return true;
            }

            // 检查是否是 ToolResultContent 的派生类
            if (typeof(ToolResultContent).IsAssignableFrom(type) && type != typeof(ToolResultContent))
            {
                discriminator = "toolResult";
                return true;
            }

            // 检查是否是 InputRequestContent 的派生类
            if (typeof(InputRequestContent).IsAssignableFrom(type) && type != typeof(InputRequestContent))
            {
                discriminator = "inputRequest";
                return true;
            }

            // 检查是否是 InputResponseContent 的派生类
            if (typeof(InputResponseContent).IsAssignableFrom(type) && type != typeof(InputResponseContent))
            {
                discriminator = "inputResponse";
                return true;
            }

            return false;
        }

        /// <summary>
        /// 从 JSON 属性推断类型并反序列化。
        /// </summary>
        private static AIContent? InferAndDeserialize(JObject jsonObject, JsonSerializer serializer)
        {
            // 如果包含 Text 属性，推断为 TextContent
            if (jsonObject.ContainsKey("text"))
            {
                using var reader = jsonObject.CreateReader();
                return serializer.Deserialize<TextContent>(reader);
            }

            // 如果包含 Message 属性，推断为 ErrorContent
            if (jsonObject.ContainsKey("message"))
            {
                using var reader = jsonObject.CreateReader();
                return serializer.Deserialize<ErrorContent>(reader);
            }

            // 如果包含 Details 属性，推断为 UsageContent
            if (jsonObject.ContainsKey("details"))
            {
                using var reader = jsonObject.CreateReader();
                return serializer.Deserialize<UsageContent>(reader);
            }

            // 如果包含 FileId 属性，推断为 HostedFileContent
            if (jsonObject.ContainsKey("fileId"))
            {
                using var reader = jsonObject.CreateReader();
                return serializer.Deserialize<HostedFileContent>(reader);
            }

            // 如果包含 VectorStoreId 属性，推断为 HostedVectorStoreContent
            if (jsonObject.ContainsKey("vectorStoreId"))
            {
                using var reader = jsonObject.CreateReader();
                return serializer.Deserialize<HostedVectorStoreContent>(reader);
            }

            // 如果包含 CallId 和 Name 属性，推断为 FunctionCallContent
            if (jsonObject.ContainsKey("callId") && jsonObject.ContainsKey("name"))
            {
                using var reader = jsonObject.CreateReader();
                return serializer.Deserialize<FunctionCallContent>(reader);
            }

            // 如果包含 CallId 和 Result 属性，推断为 FunctionResultContent
            if (jsonObject.ContainsKey("callId") && jsonObject.ContainsKey("result"))
            {
                using var reader = jsonObject.CreateReader();
                return serializer.Deserialize<FunctionResultContent>(reader);
            }

            // 如果包含 CallId 和 ServerName 属性，推断为 McpServerToolCallContent
            if (jsonObject.ContainsKey("callId") && jsonObject.ContainsKey("serverName"))
            {
                using var reader = jsonObject.CreateReader();
                return serializer.Deserialize<McpServerToolCallContent>(reader);
            }

            // 如果包含 CallId 和 Outputs 属性，推断为 ToolResultContent 的派生类
            if (jsonObject.ContainsKey("callId") && jsonObject.ContainsKey("outputs"))
            {
                using var reader = jsonObject.CreateReader();

                // 尝试推断具体的类型
                if (jsonObject.ContainsKey("serverName"))
                {
                    return serializer.Deserialize<McpServerToolResultContent>(reader);
                }
                // 使用基类
                return serializer.Deserialize<ToolResultContent>(reader);
            }

            // 如果包含 RequestId 和 ToolCall 属性，推断为 ToolApprovalRequestContent
            if (jsonObject.ContainsKey("requestId") && jsonObject.ContainsKey("toolCall"))
            {
                using var reader = jsonObject.CreateReader();
                return serializer.Deserialize<ToolApprovalRequestContent>(reader);
            }

            // 如果包含 RequestId 和 Approved 属性，推断为 ToolApprovalResponseContent
            if (jsonObject.ContainsKey("requestId") && jsonObject.ContainsKey("approved"))
            {
                using var reader = jsonObject.CreateReader();
                return serializer.Deserialize<ToolApprovalResponseContent>(reader);
            }

            // 如果包含 RequestId 属性，推断为 InputRequestContent 或 InputResponseContent
            if (jsonObject.ContainsKey("requestId"))
            {
                using var reader = jsonObject.CreateReader();
                return serializer.Deserialize<InputRequestContent>(reader);
            }

            // 如果包含 CallId 和 Queries 属性，推断为 WebSearchToolCallContent
            if (jsonObject.ContainsKey("callId") && jsonObject.ContainsKey("queries"))
            {
                using var reader = jsonObject.CreateReader();
                return serializer.Deserialize<WebSearchToolCallContent>(reader);
            }

            // 如果包含 CallId 和 Inputs 属性，推断为 CodeInterpreterToolCallContent
            if (jsonObject.ContainsKey("callId") && jsonObject.ContainsKey("inputs"))
            {
                using var reader = jsonObject.CreateReader();
                return serializer.Deserialize<CodeInterpreterToolCallContent>(reader);
            }

            // 如果包含 CallId 属性但不知道具体类型，使用 ToolCallContent
            if (jsonObject.ContainsKey("callId"))
            {
                using var reader = jsonObject.CreateReader();
                return serializer.Deserialize<ToolCallContent>(reader);
            }

            // 如果包含 Uri 或 MediaType 属性，推断为 DataContent
            if (jsonObject.ContainsKey("uri") || jsonObject.ContainsKey("mediaType"))
            {
                using var reader = jsonObject.CreateReader();
                return serializer.Deserialize<DataContent>(reader);
            }

            // 如果包含 absoluteUri 属性，推断为 UriContent
            if (jsonObject.ContainsKey("absoluteUri"))
            {
                using var reader = jsonObject.CreateReader();
                return serializer.Deserialize<UriContent>(reader);
            }

            // 默认使用基类型
            using var defaultReader = jsonObject.CreateReader();
            return serializer.Deserialize<AIContent>(defaultReader);
        }
    }

}