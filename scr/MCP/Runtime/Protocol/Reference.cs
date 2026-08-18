#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;

namespace MCP.Protocol
{
    /// <summary>
    /// Represents a reference to a resource or prompt in the Model Context Protocol.
    /// </summary>
    /// <remarks>
    /// <para>
    /// References are commonly used with <see cref="McpClient.CompleteAsync(Reference, string, string, ModelContextProtocol.RequestOptions?, CancellationToken)"/>
    /// to request completion suggestions for arguments, and with other methods that need to reference resources or prompts.
    /// </para>
    /// <para>
    /// See the <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see> for details.
    /// </para>
    /// </remarks>
    [JsonConverter(typeof(Converter))]
    public abstract class Reference
    {
        /// <summary>Prevent external derivations.</summary>
        private protected Reference()
        {
        }

        /// <summary>
        /// When overridden in a derived class, gets the type of content.
        /// </summary>
        /// <value>
        /// "ref/resource" or "ref/prompt".
        /// </value>
        [JsonProperty("type")]
        public abstract string Type { get; }

        /// <summary>
        /// Provides a <see cref="JsonConverter"/> for <see cref="Reference"/>.
        /// </summary>
        /// <remarks>
        /// Provides a polymorphic converter for the <see cref="Reference"/> class that doesn't require
        /// setting <see cref="JsonSerializerOptions.AllowOutOfOrderMetadataProperties"/> explicitly.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public sealed class Converter : JsonConverter<Reference>
        {
            /// <inheritdoc/>
            public override Reference? ReadJson(JsonReader reader, Type objectType, Reference? existingValue, bool hasExistingValue, JsonSerializer serializer)
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
                string? name = (string?)obj["name"];
                string? title = (string?)obj["title"];
                string? uri = (string?)obj["uri"];

                switch (type)
                {
                    case "ref/prompt":
                        if (name == null)
                        {
                            throw new JsonException("Prompt references must have a 'name' property.");
                        }

                        return new PromptReference { Name = name, Title = title };

                    case "ref/resource":
                        if (uri == null)
                        {
                            throw new JsonException("Resource references must have a 'uri' property.");
                        }

                        return new ResourceTemplateReference { Uri = uri };

                    default:
                        throw new JsonException($"Unknown reference type: '{type}'");
                }
            }

            /// <inheritdoc/>
            public override void WriteJson(JsonWriter writer, Reference? value, JsonSerializer serializer)
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
                    case PromptReference pr:
                        writer.WritePropertyName("name");
                        writer.WriteValue(pr.Name);
                        if (pr.Title != null)
                        {
                            writer.WritePropertyName("title");
                            writer.WriteValue(pr.Title);
                        }
                        break;

                    case ResourceTemplateReference rtr:
                        writer.WritePropertyName("uri");
                        writer.WriteValue(rtr.Uri);
                        break;
                }

                writer.WriteEndObject();
            }
        }

        /// <summary>
        /// Represents a reference to a prompt, identified by its name.
        /// </summary>
        public sealed class PromptReference : Reference, IBaseMetadata
        {
            /// <inheritdoc />
            public override string Type => "ref/prompt";

            /// <inheritdoc />
            [JsonProperty("name")]
            public string? Name { get; set; }

            /// <inheritdoc />
            [JsonProperty("title")]
            public string? Title { get; set; }

            /// <inheritdoc />
            public override string ToString() => $"\"{Type}\": \"{Name}\"";
        }

        /// <summary>
        /// Represents a reference to a resource or resource template definition.
        /// </summary>
        public sealed class ResourceTemplateReference : Reference
        {
            /// <inheritdoc />
            public override string Type => "ref/resource";

            /// <summary>
            /// Gets or sets the URI or URI template of the resource.
            /// </summary>
            [JsonProperty("uri")]
            public string? Uri { get; set; }

            /// <inheritdoc />
            public override string ToString() => $"\"{Type}\": \"{Uri}\"";
        }
    }
}