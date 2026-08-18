#nullable enable
using MCP.AI;
using MCP.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MCP.Kernel.Server
{
    /// <summary>
    /// 提供 Model Context Protocol (MCP) 类型与 Microsoft.Extensions.AI 类型之间的转换扩展方法。
    /// </summary>
    /// <remarks>
    /// 此类作为 Model Context Protocol (MCP) 类型与 Microsoft.Extensions.AI 命名空间中的
    /// <see cref="AIContent"/> 模型类型之间的适配器层。
    /// </remarks>
    public static class AIContentExtensions
    {

        /// <summary>
        /// 将指定的字典转换为 <see cref="JObject"/>。
        /// </summary>
        internal static JObject? ToJsonObject(this IReadOnlyDictionary<string, object?> properties, JsonSerializerSettings settings)
        {
            if (properties is null || properties.Count == 0)
            {
                return null;
            }

            var json = JsonConvert.SerializeObject(properties, settings);
            return JObject.Parse(json);
        }

        /// <summary>
        /// 将 <see cref="JObject"/> 转换为 <see cref="AdditionalPropertiesDictionary"/>。
        /// </summary>
        internal static AdditionalPropertiesDictionary ToAdditionalProperties(this JObject obj)
        {
            if (obj is null)
            {
                return new();
            }

            var dict = new AdditionalPropertiesDictionary();
            foreach (var kvp in obj)
            {
                dict.Add(kvp.Key, kvp.Value);
            }

            return dict;
        }



        /// <summary>
        /// 将 <see cref="ChatMessage"/> 转换为 <see cref="PromptMessage"/> 对象列表。
        /// </summary>
        public static IList<PromptMessage> ToPromptMessages(this ChatMessage chatMessage)
        {
            Throw.IfNull(chatMessage);

            Role r = chatMessage.Role == ChatRole.User ? Role.User : Role.Assistant;

            List<PromptMessage> messages = new();
            foreach (var content in chatMessage.Contents)
            {
                if (content is TextContent or DataContent)
                {
                    messages.Add(new PromptMessage { Role = r, Content = content.ToContentBlock() });
                }
            }

            return messages;
        }

        /// <summary>
        /// 从 <see cref="ContentBlock"/> 的内容创建新的 <see cref="AIContent"/>。
        /// </summary>
        public static AIContent? ToAIContent(this ContentBlock content, JsonSerializerSettings? settings = null)
        {
            Throw.IfNull(content);

            settings ??= McpJsonUtilities.DefaultSettings;

            AIContent? ac = content switch
            {
                TextContentBlock textContent => new TextContent(textContent.Text),

                ImageContentBlock imageContent => new DataContent(imageContent.DecodedData, imageContent.MimeType),

                AudioContentBlock audioContent => new DataContent(audioContent.DecodedData, audioContent.MimeType),

                EmbeddedResourceBlock resourceContent => resourceContent.Resource?.ToAIContent(),

                //ToolUseContentBlock toolUse => FunctionCallContent.CreateFromParsedArguments(
                //    toolUse.Input,
                //    toolUse.Id,
                //    toolUse.Name,
                //    json => JsonConvert.DeserializeObject<IDictionary<string, object?>>(json.ToString(), settings)),

                //ToolResultContentBlock toolResult => new FunctionResultContent(
                //    toolResult.ToolUseId,
                //    toolResult.Content.Count == 1 ? toolResult.Content[0].ToAIContent(settings) :
                //    toolResult.Content.Select(c => c.ToAIContent(settings)).OfType<AIContent>().ToList())
                //{
                //    Exception = toolResult.IsError is true ? new Exception() : null,
                //},

                _ => null,
            };

            if (ac is not null)
            {
                ac.RawRepresentation = content;
                ac.AdditionalProperties = content.Meta?.ToAdditionalProperties();
            }

            return ac;
        }

        /// <summary>
        /// 从 <see cref="ResourceContents"/> 的内容创建新的 <see cref="AIContent"/>。
        /// </summary>
        public static AIContent ToAIContent(this ResourceContents content)
        {
            Throw.IfNull(content);

            AIContent ac = content switch
            {
                BlobResourceContents blobResource => new DataContent(blobResource.DecodedData, blobResource.MimeType ?? "application/octet-stream"),
                TextResourceContents textResource => new TextContent(textResource.Text),
                _ => throw new NotSupportedException($"Resource type '{content.GetType().Name}' is not supported.")
            };

            (ac.AdditionalProperties ??= new())["uri"] = content.Uri;
            ac.RawRepresentation = content;

            return ac;
        }

        /// <summary>
        /// 从 <see cref="ContentBlock"/> 序列创建 <see cref="AIContent"/> 列表。
        /// </summary>
        public static IList<AIContent> ToAIContents(this IEnumerable<ContentBlock> contents, JsonSerializerSettings? settings = null)
        {
            Throw.IfNull(contents);

            return contents.Select(c => c.ToAIContent(settings)).OfType<AIContent>().ToList();
        }

        /// <summary>
        /// 从 <see cref="ResourceContents"/> 序列创建 <see cref="AIContent"/> 列表。
        /// </summary>
        public static IList<AIContent> ToAIContents(this IEnumerable<ResourceContents> contents)
        {
            Throw.IfNull(contents);

            return contents.Select(ToAIContent).ToList();
        }

        /// <summary>
        /// 从 <see cref="AIContent"/> 的内容创建新的 <see cref="ContentBlock"/>。
        /// </summary>
        public static ContentBlock ToContentBlock(this AIContent content, JsonSerializerSettings? settings = null)
        {
            Throw.IfNull(content);

            settings ??= McpJsonUtilities.DefaultSettings;

            ContentBlock contentBlock = content switch
            {
                TextContent textContent => new TextContentBlock
                {
                    Text = textContent.Text,
                },

                DataContent dataContent when dataContent.HasTopLevelMediaType("image") => new ImageContentBlock
                {
                    Data = EncodingUtilities.GetUtf8Bytes(dataContent.Base64Data.Span),
                    MimeType = dataContent.MediaType
                },

                DataContent dataContent when dataContent.HasTopLevelMediaType("audio") => new AudioContentBlock
                {
                    Data = EncodingUtilities.GetUtf8Bytes(dataContent.Base64Data.Span),
                    MimeType = dataContent.MediaType,
                },

                DataContent dataContent => new EmbeddedResourceBlock
                {
                    Resource = new BlobResourceContents
                    {
                        Blob = EncodingUtilities.GetUtf8Bytes(dataContent.Base64Data.Span),
                        MimeType = dataContent.MediaType,
                        Uri = string.Empty,
                    }
                },

                //FunctionCallContent callContent => new ToolUseContentBlock
                //{
                //    Id = callContent.CallId,
                //    Name = callContent.Name,
                //    Input = JObject.FromObject(callContent.Arguments, JsonSerializer.Create(settings)),
                //},

                FunctionResultContent resultContent => new ToolResultContentBlock
                {
                    ToolUseId = resultContent.CallId,
                    IsError = resultContent.Exception is not null,
                    Content =
                        resultContent.Result is AIContent c ? new List<ContentBlock>() { c.ToContentBlock(settings) } :
                        resultContent.Result is IEnumerable<AIContent> ec ? ec.Select(c => c.ToContentBlock(settings)).ToList() :
                        new List<ContentBlock>() { new TextContentBlock { Text = JsonConvert.SerializeObject(content, settings) } },
                    StructuredContent = resultContent.Result is JToken jt ? jt : null,
                },

                _ => new TextContentBlock
                {
                    Text = JsonConvert.SerializeObject(content, settings),
                }
            };

            contentBlock.Meta = content.AdditionalProperties?.ToJsonObject(settings);

            return contentBlock;
        }

        /// <summary>
        /// 将 JToken 转换为 JsonElement 的兼容对象。
        /// </summary>
        internal static JToken ToJToken(this object? value, JsonSerializerSettings settings)
        {
            if (value is null)
            {
                return JValue.CreateNull();
            }

            if (value is JToken token)
            {
                return token;
            }

            return JToken.FromObject(value, JsonSerializer.Create(settings));
        }

        private sealed class ToolAIFunctionDeclaration : AIFunctionDeclaration
        {
            private readonly Tool _tool;
            private IReadOnlyDictionary<string, object?>? _additionalProperties;

            public ToolAIFunctionDeclaration(Tool tool)
            {
                _tool = tool;
            }

            public override string Name => _tool.Name!;

            public override string Description => _tool.Description ?? "";

            public override IReadOnlyDictionary<string, object?> AdditionalProperties =>
                _additionalProperties ??= _tool.Meta is JObject meta
                    ? meta.ToObject<Dictionary<string, object?>>() ?? new Dictionary<string, object?>()
                    : new Dictionary<string, object?>();

            public override JToken JsonSchema => _tool.InputSchema;

            public override JToken? ReturnJsonSchema => _tool.OutputSchema;

            public override object? GetService(Type serviceType, object? serviceKey = null)
            {
                Throw.IfNull(serviceType);

                return
                    serviceKey is null && serviceType.IsInstanceOfType(_tool) ? _tool :
                    base.GetService(serviceType, serviceKey);
            }
        }
    }

}
