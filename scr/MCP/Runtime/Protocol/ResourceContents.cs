#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;

namespace MCP.Protocol
{
    /// <summary>
    /// Provides a base class representing contents of a resource in the Model Context Protocol.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ResourceContents"/> serves as the base class for different types of resources that can be 
    /// exchanged through the Model Context Protocol. Resources are identified by URIs and can contain
    /// different types of data.
    /// </para>
    /// <para>
    /// This class is abstract and has two concrete implementations:
    /// <list type="bullet">
    ///   <item><description><see cref="TextResourceContents"/> - For text-based resources</description></item>
    ///   <item><description><see cref="BlobResourceContents"/> - For binary data resources</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// See the <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see> for more details.
    /// </para>
    /// </remarks>
    [JsonConverter(typeof(Converter))]
    public abstract class ResourceContents
    {
        /// <summary>Prevent external derivations.</summary>
        private protected ResourceContents()
        {
        }

        /// <summary>
        /// Gets or sets the URI of the resource.
        /// </summary>
        [JsonProperty("uri")]
        public string? Uri { get; set; }

        /// <summary>
        /// Gets or sets the MIME type of the resource content.
        /// </summary>
        [JsonProperty("mimeType")]
        public string? MimeType { get; set; }

        /// <summary>
        /// Gets or sets metadata reserved by MCP for protocol-level metadata.
        /// </summary>
        /// <remarks>
        /// Implementations must not make assumptions about its contents.
        /// </remarks>
        [JsonProperty("_meta")]
        public JObject? Meta { get; set; }

        /// <summary>
        /// Provides a <see cref="JsonConverter"/> for <see cref="ResourceContents"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public sealed class Converter : JsonConverter<ResourceContents>
        {
            public override ResourceContents? ReadJson(JsonReader reader, Type objectType, ResourceContents? existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }

                if (reader.TokenType != JsonToken.StartObject)
                {
                    throw new JsonException();
                }
                // 加载整个 JSON 对象
                JObject obj = JObject.Load(reader);

                // 读取基础字段
                string uri = obj["uri"]?.Value<string>() ?? string.Empty;
                string? mimeType = obj["mimeType"]?.Value<string>();
                JObject? meta = obj["_meta"] as JObject;

                // 判断资源类型：有 text 字段 = TextResourceContents，有 blob 字段 = BlobResourceContents
                if (obj["text"] != null)
                {
                    return new TextResourceContents
                    {
                        Uri = uri,
                        MimeType = mimeType,
                        Text = obj["text"]?.Value<string>(),
                        Meta = meta
                    };
                }

                if (obj["blob"] != null)
                {
                    // blob 字段是 Base64 编码的字符串
                    string? blobBase64 = obj["blob"]?.Value<string>();
                    byte[] blobBytes = string.IsNullOrEmpty(blobBase64)
                        ? Array.Empty<byte>()
                        : Convert.FromBase64String(blobBase64);

                    return new BlobResourceContents
                    {
                        Uri = uri,
                        MimeType = mimeType,
                        Blob = blobBytes,
                        Meta = meta
                    };
                }

                // 如果没有 text 也没有 blob，返回 null
                return null;
            }

            public override void WriteJson(JsonWriter writer, ResourceContents? value, JsonSerializer serializer)
            {
                if (value == null)
                {
                    writer.WriteNull();
                    return;
                }

                writer.WriteStartObject();

                // 写入基础字段
                writer.WritePropertyName("uri");
                writer.WriteValue(value.Uri);

                if (!string.IsNullOrEmpty(value.MimeType))
                {
                    writer.WritePropertyName("mimeType");
                    writer.WriteValue(value.MimeType);
                }

                // 根据具体类型写入内容字段
                if (value is BlobResourceContents blobResource)
                {
                    writer.WritePropertyName("blob");
                    // 将 byte[] 转为 Base64 字符串
                    string blobBase64 = blobResource.Blob.Span != null
                        ? Convert.ToBase64String(blobResource.Blob.Span)
                        : string.Empty;
                    writer.WriteValue(blobBase64);
                }
                else if (value is TextResourceContents textResource)
                {
                    writer.WritePropertyName("text");
                    writer.WriteValue(textResource.Text);
                }
                else
                {
                    throw new JsonSerializationException(
                        $"Unknown ResourceContents type: {value.GetType().Name}");
                }

                // 写入元数据
                if (value.Meta != null)
                {
                    writer.WritePropertyName("_meta");
                    value.Meta.WriteTo(writer);
                }

                writer.WriteEndObject();
            }
        }
    }
}
