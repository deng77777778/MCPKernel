#nullable enable
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace MCP.AI
{
    /// <summary>
    /// 描述聊天交互中消息的预期用途。
    /// </summary>
    [JsonConverter(typeof(ChatRoleConverter))]
    [DebuggerDisplay("{Value,nq}")]
    public readonly struct ChatRole : IEquatable<ChatRole>
    {
        /// <summary>
        /// 获取指导或设置系统行为的角色。
        /// </summary>
        public static ChatRole System { get; } = new("system");

        /// <summary>
        /// 获取对系统指导、用户提示输入提供响应的角色。
        /// </summary>
        public static ChatRole Assistant { get; } = new("assistant");

        /// <summary>
        /// 获取为聊天交互提供用户输入的角色。
        /// </summary>
        public static ChatRole User { get; } = new("user");

        /// <summary>
        /// 获取响应工具使用请求提供附加信息和引用的角色。
        /// </summary>
        public static ChatRole Tool { get; } = new("tool");

        /// <summary>
        /// 获取与此 <see cref="ChatRole"/> 关联的值。
        /// </summary>
        /// <remarks>
        /// 该值将被序列化到聊天消息格式的 "role" 消息字段中。
        /// </remarks>
        public string Value { get; }

        /// <summary>
        /// 使用提供的值初始化 <see cref="ChatRole"/> 结构的新实例。
        /// </summary>
        /// <param name="value">要与此 <see cref="ChatRole"/> 关联的值。</param>
        [JsonConstructor]
        public ChatRole(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
            }
            Value = value;
        }

        /// <summary>
        /// 返回一个值，指示两个 <see cref="ChatRole"/> 实例是否等效，
        /// 通过不区分大小写的值比较确定。
        /// </summary>
        public static bool operator ==(ChatRole left, ChatRole right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 返回一个值，指示两个 <see cref="ChatRole"/> 实例是否不等效，
        /// 通过不区分大小写的值比较确定。
        /// </summary>
        public static bool operator !=(ChatRole left, ChatRole right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is ChatRole otherRole && Equals(otherRole);
        }

        /// <inheritdoc/>
        public bool Equals(ChatRole other)
        {
            return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
        }

        /// <inheritdoc/>
        public override string ToString() => Value;

        /// <summary>
        /// 从字符串隐式转换为 <see cref="ChatRole"/>。
        /// </summary>
        public static implicit operator ChatRole(string value) => new(value);

        /// <summary>
        /// 从 <see cref="ChatRole"/> 隐式转换为字符串。
        /// </summary>
        public static implicit operator string(ChatRole role) => role.Value;
    }

    /// <summary>
    /// 提供用于序列化 <see cref="ChatRole"/> 实例的 <see cref="JsonConverter{ChatRole}"/>。
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class ChatRoleConverter : JsonConverter<ChatRole>
    {
        /// <inheritdoc />
        public override ChatRole ReadJson(JsonReader reader, Type objectType, ChatRole existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                throw new JsonSerializationException("Cannot deserialize null value to ChatRole.");
            }

            var value = reader.Value?.ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new JsonSerializationException("ChatRole value cannot be null or whitespace.");
            }

            return new ChatRole(value);
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, ChatRole value, JsonSerializer serializer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            writer.WriteValue(value.Value);
        }

        /// <summary>
        /// 指示此转换器是否可以读取 JSON。
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// 指示此转换器是否可以写入 JSON。
        /// </summary>
        public override bool CanWrite => true;
    }
}
