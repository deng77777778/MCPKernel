#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace MCP.Protocol
{
    /// <summary>
    /// Represents content within the Model Context Protocol (MCP).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="ContentBlock"/> class is a fundamental type in the MCP that can represent different forms of content
    /// based on the <see cref="Type"/> property. Derived types like <see cref="TextContentBlock"/>, <see cref="ImageContentBlock"/>,
    /// and <see cref="EmbeddedResourceBlock"/> provide the type-specific content.
    /// </para>
    /// <para>
    /// This class is used throughout the MCP for representing content in messages, tool responses,
    /// and other communication between clients and servers.
    /// </para>
    /// <para>
    /// See the <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see> for more details.
    /// </para>
    /// </remarks>
    [JsonConverter(typeof(Converter))]
    public abstract class ContentBlock
    {
        /// <summary>Prevent external derivations.</summary>
        private protected ContentBlock()
        {
        }

        /// <summary>
        /// When overridden in a derived class, gets the type of content.
        /// </summary>
        /// <value>
        /// The type of content. Valid values include "image", "audio", "text", "resource", "resource_link", "tool_use", and "tool_result".
        /// </value>
        /// <remarks>
        /// This value determines the structure of the content object.
        /// </remarks>
        [JsonProperty("type")]
        public abstract string Type { get; }

        /// <summary>
        /// Gets or sets optional annotations for the content.
        /// </summary>
        /// <remarks>
        /// These annotations can be used to specify the intended audience (<see cref="Role.User"/>, <see cref="Role.Assistant"/>, or both)
        /// and the priority level of the content. Clients can use this information to filter or prioritize content for different roles.
        /// </remarks>
        [JsonProperty("annotations")]
        public Annotations? Annotations { get; set; }

        /// <summary>
        /// Gets or sets metadata reserved by MCP for protocol-level metadata.
        /// </summary>
        /// <remarks>
        /// Implementations must not make assumptions about its contents.
        /// </remarks>
        [JsonProperty("_meta")]
        public JObject? Meta { get; set; }

        /// <summary>
        /// Provides a <see cref="JsonConverter"/> for <see cref="ContentBlock"/>.
        /// </summary>
        /// <remarks>
        /// Provides a polymorphic converter for the <see cref="ContentBlock"/> class that doesn't require
        /// setting <see cref="JsonSerializerOptions.AllowOutOfOrderMetadataProperties"/> explicitly.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public sealed class Converter : JsonConverter<ContentBlock>
        {
            /// <inheritdoc/>
            public override ContentBlock? ReadJson(JsonReader reader, Type objectType, ContentBlock? existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }

                if (reader.TokenType != JsonToken.StartObject)
                {
                    throw new JsonException("Expected StartObject token.");
                }

                JObject obj = JObject.Load(reader);

                string? type = (string?)obj["type"];
                string? text = (string?)obj["text"];
                string? name = (string?)obj["name"];
                string? title = (string?)obj["title"];
                byte[]? data = null;
                byte[]? decodedData = null;
                string? mimeType = (string?)obj["mimeType"];
                string? uri = (string?)obj["uri"];
                string? description = (string?)obj["description"];
                long? size = (long?)obj["size"];
                IList<Icon>? icons = null;
                ResourceContents? resource = null;
                Annotations? annotations = null;
                JObject? meta = null;
                string? id = (string?)obj["id"];
                JToken? input = null;
                string? toolUseId = (string?)obj["toolUseId"];
                List<ContentBlock>? content = null;
                JToken? structuredContent = null;
                bool? isError = (bool?)obj["isError"];

                // Handle data (base64 encoded)
                JToken? dataToken = obj["data"];
                if (dataToken != null && dataToken.Type == JTokenType.String)
                {
                    string dataString = (string)dataToken!;
                    try
                    {
                        decodedData = Convert.FromBase64String(dataString);
                        data = System.Text.Encoding.UTF8.GetBytes(dataString);
                    }
                    catch (FormatException)
                    {
                        throw new JsonException("Invalid base64 data.");
                    }
                }

                // Handle icons
                JToken? iconsToken = obj["icons"];
                if (iconsToken != null && iconsToken.Type == JTokenType.Array)
                {
                    icons = new List<Icon>();
                    foreach (JToken iconToken in iconsToken)
                    {
                        Icon? icon = iconToken.ToObject<Icon>(serializer);
                        if (icon == null)
                        {
                            throw new JsonException("Unexpected null item in icons array.");
                        }
                        icons.Add(icon);
                    }
                }

                // Handle resource
                JToken? resourceToken = obj["resource"];
                if (resourceToken != null && resourceToken.Type == JTokenType.Object)
                {
                    resource = resourceToken.ToObject<ResourceContents>(serializer);
                }

                // Handle annotations
                JToken? annotationsToken = obj["annotations"];
                if (annotationsToken != null && annotationsToken.Type == JTokenType.Object)
                {
                    annotations = annotationsToken.ToObject<Annotations>(serializer);
                }

                // Handle _meta
                JToken? metaToken = obj["_meta"];
                if (metaToken != null && metaToken.Type == JTokenType.Object)
                {
                    meta = (JObject)metaToken;
                }

                // Handle input
                JToken? inputToken = obj["input"];
                if (inputToken != null)
                {
                    input = inputToken;
                }

                // Handle content
                JToken? contentToken = obj["content"];
                if (contentToken != null)
                {
                    content = new List<ContentBlock>();
                    if (contentToken.Type == JTokenType.Array)
                    {
                        foreach (JToken item in contentToken)
                        {
                            ContentBlock? block = item.ToObject<ContentBlock>(serializer);
                            if (block == null)
                            {
                                throw new JsonException("Unexpected null item in content array.");
                            }
                            content.Add(block);
                        }
                    }
                    else if (contentToken.Type == JTokenType.Object)
                    {
                        ContentBlock? block = contentToken.ToObject<ContentBlock>(serializer);
                        if (block == null)
                        {
                            throw new JsonException("Unexpected null content item.");
                        }
                        content.Add(block);
                    }
                }

                // Handle structuredContent
                JToken? structuredContentToken = obj["structuredContent"];
                if (structuredContentToken != null)
                {
                    structuredContent = structuredContentToken;
                }

                ContentBlock cb = type switch
                {
                    "text" => new TextContentBlock
                    {
                        Text = text ?? throw new JsonException("Text contents must be provided for 'text' type."),
                    },

                    "image" => decodedData != null ?
                        ImageContentBlock.FromBytes(decodedData,
                            mimeType ?? throw new JsonException("MIME type must be provided for 'image' type.")) :
                        new ImageContentBlock(data ?? throw new JsonException("Image data must be provided for 'image' type.")
                        , mimeType ?? throw new JsonException("MIME type must be provided for 'image' type.")),

                    "audio" => decodedData != null ?
                        AudioContentBlock.FromBytes(decodedData,
                            mimeType ?? throw new JsonException("MIME type must be provided for 'audio' type.")) :
                        new AudioContentBlock
                        {
                            Data = data ?? throw new JsonException("Audio data must be provided for 'audio' type."),
                            MimeType = mimeType ?? throw new JsonException("MIME type must be provided for 'audio' type."),
                        },

                    "resource" => new EmbeddedResourceBlock
                    {
                        Resource = resource ?? throw new JsonException("Resource contents must be provided for 'resource' type."),
                    },

                    "resource_link" => new ResourceLinkBlock
                    {
                        Uri = uri ?? throw new JsonException("URI must be provided for 'resource_link' type."),
                        Name = name ?? throw new JsonException("Name must be provided for 'resource_link' type."),
                        Title = title,
                        Description = description,
                        MimeType = mimeType,
                        Size = size,
                        Icons = icons,
                    },

                    "tool_use" => throw new NotSupportedException("tool_use"),

                    "tool_result" => throw new NotSupportedException("tool_result"),

                    _ => throw new JsonException($"Unknown content type: '{type}'"),
                };

                cb.Annotations = annotations;
                cb.Meta = meta;

                return cb;
            }

            /// <inheritdoc/>
            public override void WriteJson(JsonWriter writer, ContentBlock? value, JsonSerializer serializer)
            {
                if (value == null)
                {
                    writer.WriteNull();
                    return;
                }

                writer.WriteStartObject();

                writer.WritePropertyName("type");
                writer.WriteValue(value.Type);

                switch (value)
                {
                    case TextContentBlock textContent:
                        writer.WritePropertyName("text");
                        writer.WriteValue(textContent.Text);
                        break;

                    case ImageContentBlock imageContent:
                        writer.WritePropertyName("data");
                        writer.WriteValue(Convert.ToBase64String(imageContent.Data.ToArray()));
                        writer.WritePropertyName("mimeType");
                        writer.WriteValue(imageContent.MimeType);
                        break;

                    case AudioContentBlock audioContent:
                        writer.WritePropertyName("data");
                        writer.WriteValue(Convert.ToBase64String(audioContent.Data.ToArray()));
                        writer.WritePropertyName("mimeType");
                        writer.WriteValue(audioContent.MimeType);
                        break;

                    case EmbeddedResourceBlock embeddedResource:
                        writer.WritePropertyName("resource");
                        serializer.Serialize(writer, embeddedResource.Resource);
                        break;

                    case ResourceLinkBlock resourceLink:
                        writer.WritePropertyName("uri");
                        writer.WriteValue(resourceLink.Uri);
                        writer.WritePropertyName("name");
                        writer.WriteValue(resourceLink.Name);
                        if (resourceLink.Title != null)
                        {
                            writer.WritePropertyName("title");
                            writer.WriteValue(resourceLink.Title);
                        }
                        if (resourceLink.Description != null)
                        {
                            writer.WritePropertyName("description");
                            writer.WriteValue(resourceLink.Description);
                        }
                        if (resourceLink.MimeType != null)
                        {
                            writer.WritePropertyName("mimeType");
                            writer.WriteValue(resourceLink.MimeType);
                        }
                        if (resourceLink.Size.HasValue)
                        {
                            writer.WritePropertyName("size");
                            writer.WriteValue(resourceLink.Size.Value);
                        }
                        if (resourceLink.Icons != null && resourceLink.Icons.Count > 0)
                        {
                            writer.WritePropertyName("icons");
                            writer.WriteStartArray();
                            foreach (var icon in resourceLink.Icons)
                            {
                                serializer.Serialize(writer, icon);
                            }
                            writer.WriteEndArray();
                        }
                        break;

                    //case ToolUseContentBlock toolUse:
                    //    writer.WritePropertyName("id");
                    //    writer.WriteValue(toolUse.Id);
                    //    writer.WritePropertyName("name");
                    //    writer.WriteValue(toolUse.Name);
                    //    writer.WritePropertyName("input");
                    //    serializer.Serialize(writer, toolUse.Input);
                    //    break;

                    //case ToolResultContentBlock toolResult:
                    //    writer.WritePropertyName("toolUseId");
                    //    writer.WriteValue(toolResult.ToolUseId);
                    //    writer.WritePropertyName("content");
                    //    writer.WriteStartArray();
                    //    foreach (var item in toolResult.Content)
                    //    {
                    //        WriteJson(writer, item, serializer);
                    //    }
                    //    writer.WriteEndArray();
                    //    if (toolResult.StructuredContent != null)
                    //    {
                    //        writer.WritePropertyName("structuredContent");
                    //        serializer.Serialize(writer, toolResult.StructuredContent);
                    //    }
                    //    if (toolResult.IsError.HasValue)
                    //    {
                    //        writer.WritePropertyName("isError");
                    //        writer.WriteValue(toolResult.IsError.Value);
                    //    }
                    //    break;
                }

                if (value.Annotations != null)
                {
                    writer.WritePropertyName("annotations");
                    serializer.Serialize(writer, value.Annotations);
                }

                if (value.Meta != null)
                {
                    writer.WritePropertyName("_meta");
                    serializer.Serialize(writer, value.Meta);
                }

                writer.WriteEndObject();
            }
        }
    }

    /// <summary>Represents text provided to or from an LLM.</summary>
    [DebuggerDisplay("Text = \"{Text}\"")]
    public sealed class TextContentBlock : ContentBlock
    {
        /// <inheritdoc/>
        public override string Type => "text";

        /// <summary>
        /// Gets or sets the text content of the message.
        /// </summary>
        [JsonProperty("text")]
        public string? Text { get; set; }

        /// <inheritdoc/>
        public override string ToString() => Text ?? "";
    }

    /// <summary>Represents an image provided to or from an LLM.</summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public sealed class ImageContentBlock : ContentBlock
    {
        private ReadOnlyMemory<byte>? _decodedData;
        private ReadOnlyMemory<byte>? _data;

        /// <summary>
        /// Creates an <see cref="ImageContentBlock"/> from decoded image bytes.
        /// </summary>
        /// <param name="bytes">The unencoded image bytes.</param>
        /// <param name="mimeType">The MIME type of the image.</param>
        /// <returns>A new <see cref="ImageContentBlock"/> instance.</returns>
        /// <remarks>
        /// This method stores the provided bytes as <see cref="DecodedData"/> and lazily encodes them to base64 UTF-8 bytes for <see cref="Data"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="mimeType"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="mimeType"/> is empty or composed entirely of whitespace.</exception>
        public static ImageContentBlock FromBytes(ReadOnlyMemory<byte> bytes, string mimeType)
        {
            if (string.IsNullOrEmpty(mimeType))
                throw new ArgumentNullException(nameof(mimeType));

            return new(bytes, mimeType);
        }
        public ImageContentBlock()
        {
        }
        internal ImageContentBlock(ReadOnlyMemory<byte> decodedData, string mimeType)
        {
            _decodedData = decodedData;
            MimeType = mimeType;
        }

        /// <inheritdoc/>
        public override string Type => "image";

        /// <summary>
        /// Gets or sets the base64-encoded UTF-8 bytes representing the image data.
        /// </summary>
        /// <remarks>
        /// Setting this value will invalidate any cached value of <see cref="DecodedData"/>.
        /// </remarks>
        [JsonProperty("data")]
        public ReadOnlyMemory<byte> Data
        {
            get
            {
                if (_data is null)
                {
                    Debug.Assert(_decodedData is not null);
                    _data = EncodingUtilities.EncodeToBase64Utf8(_decodedData!.Value);
                }

                return _data.Value;
            }
            set
            {
                _data = value;
                _decodedData = null; // Invalidate cache
            }
        }

        /// <summary>
        /// Gets the decoded image data represented by <see cref="Data"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When getting, this member will decode the value in <see cref="Data"/> and cache the result.
        /// Subsequent accesses return the cached value unless <see cref="Data"/> is modified.
        /// </para>
        /// </remarks>
        [JsonIgnore]
        public ReadOnlyMemory<byte> DecodedData
        {
            get
            {
                if (_decodedData is null)
                {
                    _decodedData = EncodingUtilities.DecodeFromBase64Utf8(Data);
                }

                return _decodedData.Value;
            }
        }

        /// <summary>
        /// Gets or sets the MIME type (or "media type") of the content, specifying the format of the data.
        /// </summary>
        /// <remarks>
        /// Common values include "image/png" and "image/jpeg".
        /// </remarks>
        [JsonProperty("mimeType")]
        public string? MimeType { get; set; }
    }

    /// <summary>Represents audio provided to or from an LLM.</summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public sealed class AudioContentBlock : ContentBlock
    {
        private ReadOnlyMemory<byte>? _decodedData;
        private ReadOnlyMemory<byte>? _data;

        /// <summary>
        /// Creates an <see cref="AudioContentBlock"/> from decoded audio bytes.
        /// </summary>
        /// <param name="bytes">The unencoded audio bytes.</param>
        /// <param name="mimeType">The MIME type of the audio.</param>
        /// <returns>A new <see cref="AudioContentBlock"/> instance.</returns>
        /// <remarks>
        /// This method stores the provided bytes as <see cref="DecodedData"/> and lazily encodes them to base64 UTF-8 bytes for <see cref="Data"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="mimeType"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="mimeType"/> is empty or composed entirely of whitespace.</exception>
        public static AudioContentBlock FromBytes(ReadOnlyMemory<byte> bytes, string mimeType)
        {
            if (string.IsNullOrEmpty(mimeType))
                throw new ArgumentNullException(nameof(mimeType));
            return new(bytes, mimeType);
        }

        /// <summary>Initializes a new instance of the <see cref="AudioContentBlock"/> class.</summary>
        public AudioContentBlock()
        {
        }

        private AudioContentBlock(ReadOnlyMemory<byte> decodedData, string mimeType)
        {
            _decodedData = decodedData;
            MimeType = mimeType;
        }

        /// <inheritdoc/>
        public override string Type => "audio";

        /// <summary>
        /// Gets or sets the base64-encoded UTF-8 bytes representing the audio data.
        /// </summary>
        /// <remarks>
        /// Setting this value will invalidate any cached value of <see cref="DecodedData"/>.
        /// </remarks>
        [JsonProperty("data")]
        public ReadOnlyMemory<byte> Data
        {
            get
            {
                if (_data is null)
                {
                    Debug.Assert(_decodedData is not null);
                    _data = EncodingUtilities.EncodeToBase64Utf8(_decodedData!.Value);
                }

                return _data.Value;
            }
            set
            {
                _data = value;
                _decodedData = null; // Invalidate cache
            }
        }

        /// <summary>
        /// Gets the decoded audio data represented by <see cref="Data"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When getting, this member will decode the value in <see cref="Data"/> and cache the result.
        /// Subsequent accesses return the cached value unless <see cref="Data"/> is modified.
        /// </para>
        /// </remarks>
        [JsonIgnore]
        public ReadOnlyMemory<byte> DecodedData
        {
            get
            {
                if (_decodedData is null)
                {
                    _decodedData = EncodingUtilities.DecodeFromBase64Utf8(Data);
                }

                return _decodedData.Value;
            }
        }

        /// <summary>
        /// Gets or sets the MIME type (or "media type") of the content, specifying the format of the data.
        /// </summary>
        /// <remarks>
        /// Common values include "audio/wav" and "audio/mp3".
        /// </remarks>
        [JsonProperty("mimeType")]
        public string? MimeType { get; set; }
    }

    /// <summary>Represents the contents of a resource, embedded into a prompt or tool call result.</summary>
    /// <remarks>
    /// It is up to the client how best to render embedded resources for the benefit of the LLM and/or the user.
    /// </remarks>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public sealed class EmbeddedResourceBlock : ContentBlock
    {
        /// <inheritdoc/>
        public override string Type => "resource";

        /// <summary>
        /// Gets or sets the resource content of the message when <see cref="Type"/> is "resource".
        /// </summary>
        /// <remarks>
        /// <para>
        /// Resources can be either text-based (<see cref="TextResourceContents"/>) or
        /// binary (<see cref="BlobResourceContents"/>), allowing for flexible data representation.
        /// Each resource has a URI that can be used for identification and retrieval.
        /// </para>
        /// </remarks>
        [JsonProperty("resource")]
        public ResourceContents? Resource { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay => $"Uri = \"{Resource?.Uri}\"";
    }

    /// <summary>Represents a resource that the server is capable of reading, included in a prompt or tool call result.</summary>
    /// <remarks>
    /// Resource links returned by tools are not guaranteed to appear in the results of `resources/list` requests.
    /// </remarks>
    [DebuggerDisplay("Name = {Name}, Uri = \"{Uri}\"")]
    public sealed class ResourceLinkBlock : ContentBlock
    {
        /// <inheritdoc/>
        public override string Type => "resource_link";

        /// <summary>
        /// Gets or sets the URI of this resource.
        /// </summary>
        [JsonProperty("uri")]
        public string? Uri { get; set; }

        /// <summary>
        /// Gets or sets a human-readable name for this resource.
        /// </summary>
        [JsonProperty("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets a title for this resource.
        /// </summary>
        /// <remarks>
        /// This is intended for UI and end-user contexts. It is optimized to be human-readable and easily understood,
        /// even by those unfamiliar with domain-specific terminology.
        /// If not provided, <see cref="Name"/> can be used for display.
        /// </remarks>
        [JsonProperty("title")]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets a description of what this resource represents.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This description can be used by clients to improve the LLM's understanding of available resources. It can be thought of like a \"hint\" to the model.
        /// </para>
        /// <para>
        /// The description should provide clear context about the resource's content, format, and purpose.
        /// This helps AI models make better decisions about when to access or reference the resource.
        /// </para>
        /// <para>
        /// Client applications can also use this description for display purposes in user interfaces
        /// or to help users understand the available resources.
        /// </para>
        /// </remarks>
        [JsonProperty("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the MIME type of this resource.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="MimeType"/> specifies the format of the resource content, helping clients to properly interpret and display the data.
        /// Common MIME types include "text/plain" for plain text, "application/pdf" for PDF documents,
        /// "image/png" for PNG images, and "application/json" for JSON data.
        /// </para>
        /// <para>
        /// This property can be <see langword="null"/> if the MIME type is unknown or not applicable for the resource.
        /// </para>
        /// </remarks>
        [JsonProperty("mimeType")]
        public string? MimeType { get; set; }

        /// <summary>
        /// Gets or sets the size of the raw resource content (before base64 encoding), in bytes, if known.
        /// </summary>
        /// <remarks>
        /// This value can be used by applications to display file sizes and estimate context window usage.
        /// </remarks>
        [JsonProperty("size")]
        public long? Size { get; set; }

        /// <summary>
        /// Gets or sets an optional list of icons for this resource.
        /// </summary>
        /// <remarks>
        /// This can be used by clients to display the resource's icon in a user interface.
        /// </remarks>
        [JsonProperty("icons")]
        public IList<Icon>? Icons { get; set; }
    }

}
