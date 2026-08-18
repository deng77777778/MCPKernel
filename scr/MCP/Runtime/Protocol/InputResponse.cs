using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace MCP.Protocol
{
    /// <summary>
    /// Represents a client's response to a server-initiated <see cref="InputRequest"/> as part of an MRTR
    /// (Multi Round-Trip Request) flow.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An <see cref="InputResponse"/> wraps the result of a server-to-client request such as
    /// <see cref="CreateMessageResult"/>, <see cref="ElicitResult"/>, or <see cref="ListRootsResult"/>.
    /// The type of the inner response corresponds to the <see cref="InputRequest.Method"/> of the
    /// associated input request.
    /// </para>
    /// <para>
    /// The input response does not carry its own type discriminator in JSON. The type is determined by
    /// the corresponding <see cref="InputRequest.Method"/> key in the <see cref="InputRequiredResult.InputRequests"/> map.
    /// </para>
    /// </remarks>
    [JsonConverter(typeof(Converter))]
    public sealed class InputResponse
    {
        /// <summary>
        /// Gets or sets the raw JSON element representing the response.
        /// </summary>
        /// <remarks>
        /// Use <see cref="Deserialize{T}"/> with the <c>JsonTypeInfo&lt;T&gt;</c> matching the
        /// associated <see cref="InputRequest.Method"/> - for elicitation, sampling, or roots see
        /// <see cref="ElicitResultJsonTypeInfo"/>, <see cref="CreateMessageResultJsonTypeInfo"/>, and
        /// <see cref="ListRootsResultJsonTypeInfo"/>.
        /// </remarks>
        [JsonIgnore]
        public JToken RawValue { get; set; }

        public T Deserialize<T>()
        {
            if (RawValue == null)
            {
                return default;
            }

            try
            {
                return RawValue.ToObject<T>();
            }
            catch (JsonException)
            {
                return default;
            }
        }
        public T Deserialize<T>(JsonSerializer serializer)
        {
            if (RawValue == null)
            {
                return default;
            }

            try
            {
                return RawValue.ToObject<T>(serializer);
            }
            catch (JsonException)
            {
                return default;
            }
        }

        /// <summary>
        /// Creates an <see cref="InputResponse"/> from an <see cref="ElicitResult"/>.
        /// </summary>
        /// <param name="result">The elicitation result.</param>
        /// <returns>A new <see cref="InputResponse"/> instance.</returns>
        public static InputResponse FromElicitResult(ElicitResult result)
        {
            if (result is null)
                throw new System.ArgumentNullException(nameof(result));
            return new()

            {
                RawValue = JToken.FromObject(result)
            };
        }


        /// <summary>Provides JSON serialization support for <see cref="InputResponse"/>.</summary>
        public sealed class Converter : JsonConverter<InputResponse>
        {
            /// <inheritdoc/>
            public override InputResponse ReadJson(JsonReader reader, Type objectType, InputResponse existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                JToken token = JToken.Load(reader);
                return new InputResponse { RawValue = token };
            }

            public override void WriteJson(JsonWriter writer, InputResponse value, JsonSerializer serializer)
            {
                value.RawValue.WriteTo(writer);
            }
        }
    }
}
