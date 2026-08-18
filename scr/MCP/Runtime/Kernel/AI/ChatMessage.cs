#nullable enable
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MCP.AI
{
    /// <summary>
    /// 表示由 <see cref="IChatClient"/> 使用的聊天消息。
    /// </summary>
    /// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/quickstarts/build-chat-app">使用 .NET 构建 AI 聊天应用。</related>
    [DebuggerDisplay("[{Role}] {ContentForDebuggerDisplay}{EllipsesForDebuggerDisplay,nq}")]
    [JsonObject(MemberSerialization.OptIn)]
    public class ChatMessage
    {
        private IList<AIContent>? _contents;
        private string? _authorName;

        /// <summary>
        /// 初始化 <see cref="ChatMessage"/> 类的新实例。
        /// </summary>
        /// <remarks>实例默认角色为 <see cref="ChatRole.User"/>。</remarks>
        [JsonConstructor]
        public ChatMessage()
        {
        }

        /// <summary>
        /// 初始化 <see cref="ChatMessage"/> 类的新实例。
        /// </summary>
        /// <param name="role">消息作者的角色。</param>
        /// <param name="content">消息的文本内容。</param>
        public ChatMessage(ChatRole role, string? content)
            : this(role, content is null ? new List<AIContent>() : new List<AIContent>() { new TextContent(content) })
        {
        }

        /// <summary>
        /// 初始化 <see cref="ChatMessage"/> 类的新实例。
        /// </summary>
        /// <param name="role">消息作者的角色。</param>
        /// <param name="contents">此消息的内容。</param>
        public ChatMessage(ChatRole role, IList<AIContent>? contents)
        {
            Role = role;
            _contents = contents;
        }

        /// <summary>
        /// 克隆 <see cref="ChatMessage"/> 到新的 <see cref="ChatMessage"/> 实例。
        /// </summary>
        /// <returns>原始消息对象的浅表克隆。</returns>
        /// <remarks>
        /// 这是浅表克隆。返回的实例与原始实例不同，但所有属性
        /// 都引用与原始实例相同的对象。
        /// </remarks>
        public ChatMessage Clone() =>
            new()
            {
                AdditionalProperties = AdditionalProperties,
                _authorName = _authorName,
                _contents = _contents,
                CreatedAt = CreatedAt,
                RawRepresentation = RawRepresentation,
                Role = Role,
                MessageId = MessageId,
            };

        /// <summary>
        /// 获取或设置消息作者的名称。
        /// </summary>
        [JsonProperty("authorName")]
        public string? AuthorName
        {
            get => _authorName;
            set => _authorName = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        /// <summary>
        /// 获取或设置聊天消息的时间戳。
        /// </summary>
        [JsonProperty("createdAt")]
        public DateTimeOffset? CreatedAt { get; set; }

        /// <summary>
        /// 获取或设置消息作者的角色。
        /// </summary>
        [JsonProperty("role")]
        public ChatRole Role { get; set; } = ChatRole.User;

        /// <summary>
        /// 获取此消息的文本。
        /// </summary>
        /// <remarks>
        /// 此属性连接 <see cref="Contents"/> 中所有 <see cref="TextContent"/> 对象的文本。
        /// </remarks>
        [JsonIgnore]
        public string Text => Contents.ConcatText();

        /// <summary>
        /// 获取或设置聊天消息内容项。
        /// </summary>
        [AllowNull]
        [JsonProperty("contents")]
        public IList<AIContent> Contents
        {
            get => _contents ??= new List<AIContent>();
            set => _contents = value;
        }

        /// <summary>
        /// 获取或设置聊天消息的 ID。
        /// </summary>
        [JsonProperty("messageId")]
        public string? MessageId { get; set; }

        /// <summary>
        /// 获取或设置来自底层实现的聊天消息的原始表示。
        /// </summary>
        /// <remarks>
        /// 如果创建 <see cref="ChatMessage"/> 来表示另一个对象模型中的某个底层对象，
        /// 则此属性可用于存储该原始对象。这对于调试或使消费者能够
        /// 在需要时访问底层对象模型非常有用。
        /// </remarks>
        [JsonIgnore]
        public object? RawRepresentation { get; set; }

        /// <summary>
        /// 获取或设置与消息关联的任何附加属性。
        /// </summary>
        [JsonProperty("additionalProperties")]
        public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

        /// <inheritdoc/>
        public override string ToString() => Text;

        /// <summary>
        /// 获取要在调试器显示中显示的 <see cref="AIContent"/> 对象。
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private AIContent? ContentForDebuggerDisplay
        {
            get
            {
                string text = Text;
                return
                    !string.IsNullOrWhiteSpace(text) ? new TextContent(text) :
                    _contents is { Count: > 0 } ? _contents[0] :
                    null;
            }
        }

        /// <summary>
        /// 获取调试器显示中是否有更多内容的指示。
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string EllipsesForDebuggerDisplay => _contents is { Count: > 1 } ? ", ..." : string.Empty;
    }
}
