#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace MCP.Protocol
{
    /// <summary>
    /// Represents a server-initiated request that the client must fulfill as part of an MRTR
    /// (Multi Round-Trip Request) flow.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An <see cref="InputRequest"/> wraps a server-to-client request such as
    /// <see cref="RequestMethods.SamplingCreateMessage"/>, <see cref="RequestMethods.ElicitationCreate"/>,
    /// or <see cref="RequestMethods.RootsList"/>. It is included in an <see cref="InputRequiredResult"/>
    /// when the server needs additional input before it can complete a client-initiated request.
    /// </para>
    /// <para>
    /// The <see cref="Method"/> property identifies the type of request, and the corresponding
    /// parameters can be accessed via the typed accessor properties.
    /// </para>
    /// </remarks>
    [JsonConverter(typeof(Converter))]
    public sealed class InputRequest
    {
        /// <summary>
        /// Gets or sets the method name identifying the type of this input request.
        /// </summary>
        /// <remarks>
        /// Standard values include:
        /// <list type="bullet">
        ///   <item><term><see cref="RequestMethods.SamplingCreateMessage"/></term><description>A sampling request.</description></item>
        ///   <item><term><see cref="RequestMethods.ElicitationCreate"/></term><description>An elicitation request.</description></item>
        ///   <item><term><see cref="RequestMethods.RootsList"/></term><description>A roots list request.</description></item>
        /// </list>
        /// </remarks>
        [JsonProperty("method")]
        public string? Method { get; set; }

        /// <summary>
        /// Gets or sets the raw JSON parameters for this input request.
        /// </summary>
        /// <remarks>
        /// Use the typed accessor properties (<see cref="SamplingParams"/>, <see cref="ElicitationParams"/>,
        /// <see cref="RootsParams"/>) for convenient strongly-typed access.
        /// </remarks>
        [JsonProperty("params")]
        public JToken? Params { get; set; }

        /// <summary>
        /// Gets the parameters as <see cref="ElicitRequestParams"/> when <see cref="Method"/>
        /// is <see cref="RequestMethods.ElicitationCreate"/>.
        /// </summary>
        /// <returns>The deserialized elicitation parameters, or <see langword="null"/> if the method does not match or params are absent.</returns>
        [JsonIgnore]
        public ElicitRequestParams? ElicitationParams =>
            string.Equals(Method, RequestMethods.ElicitationCreate, StringComparison.Ordinal) && Params is { } p
                ? p.ToObject<ElicitRequestParams>(JsonSerializer.Create(McpJsonUtilities.DefaultSettings))
                : null;

        /// <summary>
        /// Creates an <see cref="InputRequest"/> for an elicitation request.
        /// </summary>
        /// <param name="requestParams">The elicitation request parameters.</param>
        /// <returns>A new <see cref="InputRequest"/> instance.</returns>
        public static InputRequest ForElicitation(ElicitRequestParams requestParams)
        {
            if (requestParams is null)
                throw new ArgumentNullException(nameof(requestParams));

            return new()
            {
                Method = RequestMethods.ElicitationCreate,
                Params = JToken.FromObject(requestParams, JsonSerializer.Create(McpJsonUtilities.DefaultSettings)),
            };
        }

        /// <summary>Provides JSON serialization support for <see cref="InputRequest"/>.</summary>
        public sealed class Converter : JsonConverter<InputRequest>
        {
            /// <inheritdoc/>
            public override InputRequest ReadJson(JsonReader reader, Type objectType, InputRequest? existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                if (reader.TokenType != JsonToken.StartObject)
                {
                    throw new JsonException("Expected StartObject token.");
                }

                JObject obj = JObject.Load(reader);

                string? method = (string?)obj["method"];
                JToken? parameters = obj["params"];

                if (method == null)
                {
                    throw new JsonException("InputRequest must have a 'method' property.");
                }

                return new InputRequest
                {
                    Method = method,
                    Params = parameters,
                };
            }

            /// <inheritdoc/>
            public override void WriteJson(JsonWriter writer, InputRequest? value, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("method");
                writer.WriteValue(value?.Method);

                if (value?.Params != null)
                {
                    writer.WritePropertyName("params");
                    value.Params.WriteTo(writer);
                }

                writer.WriteEndObject();
            }
        }
    }
}
