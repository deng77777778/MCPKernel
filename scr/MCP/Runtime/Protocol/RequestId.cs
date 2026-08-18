#nullable enable
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Globalization;

namespace MCP.Protocol
{
    /// <summary>
    /// Represents a JSON-RPC request identifier, which can be either a string or an integer.
    /// </summary>
    [JsonConverter(typeof(Converter))]
    public readonly struct RequestId : IEquatable<RequestId>
    {
        /// <summary>Initializes a new instance of the <see cref="RequestId"/> with a specified value.</summary>
        /// <param name="value">The required ID value.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public RequestId(string value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            Id = value;
        }

        /// <summary>Initializes a new instance of the <see cref="RequestId"/> with a specified value.</summary>
        /// <param name="value">The required ID value.</param>
        public RequestId(long value)
        {
            // Box the long. Request IDs are almost always strings in practice, so this should be rare.
            Id = value;
        }

        /// <summary>Gets the underlying object for this ID.</summary>
        /// <remarks>This object will either be a <see cref="string"/>, a boxed <see cref="long"/>, or <see langword="null"/>.</remarks>
        public object Id { get; }

        /// <inheritdoc />
        public override string ToString() =>
            Id is string stringValue ? stringValue :
            Id is long longValue ? longValue.ToString(CultureInfo.InvariantCulture) :
            string.Empty;

        /// <inheritdoc />
        public bool Equals(RequestId other) => Equals(Id, other.Id);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is RequestId other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => Id?.GetHashCode() ?? 0;

        /// <inheritdoc />
        public static bool operator ==(RequestId left, RequestId right) => left.Equals(right);

        /// <inheritdoc />
        public static bool operator !=(RequestId left, RequestId right) => !left.Equals(right);

        /// <summary>
        /// Provides a <see cref="JsonConverter"/> for <see cref="RequestId"/> that handles both string and number values.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public sealed class Converter : JsonConverter<RequestId>
        {
            /// <inheritdoc />
            public override RequestId ReadJson(JsonReader reader, Type objectType, RequestId existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                return reader.TokenType switch
                {
                    JsonToken.String => new(reader.Value!.ToString()),
                    JsonToken.Integer => new(Convert.ToInt64(reader.Value)),
                    JsonToken.Null => default,
                    _ => throw new JsonException("requestId must be a string, integer, or null"),
                };
            }

            public override void WriteJson(JsonWriter writer, RequestId value, JsonSerializer serializer)
            {
                if(writer is  null)
                    throw new ArgumentNullException(nameof(writer));

                switch (value.Id)
                {
                    case string str:
                        writer.WriteValue(str);
                        return;

                    case long longValue:
                        writer.WriteValue(longValue);
                        return;

                    case null:
                        // A null Id represents a JSON-RPC error response whose request id could not be
                        // determined (JSON-RPC 2.0 §5; the MCP base protocol permits an error response to a
                        // malformed request to carry a null id). Emit JSON null — not "" — so the wire form
                        // is spec-conformant and round-trips losslessly with the Null-accepting Read above.
                        writer.WriteNull();
                        return;
                }
            }
        }
    }
}
