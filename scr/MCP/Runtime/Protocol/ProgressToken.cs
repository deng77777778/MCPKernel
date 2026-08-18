#nullable enable
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Globalization;

namespace MCP.Protocol
{
    /// <summary>
    /// Represents a progress token, which can be either a string or an integer.
    /// </summary>
    [JsonConverter(typeof(Converter))]
    public readonly struct ProgressToken : IEquatable<ProgressToken>
    {
        /// <summary>Initializes a new instance of the <see cref="ProgressToken"/> with a specified value.</summary>
        /// <param name="value">The required ID value.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public ProgressToken(string? value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(value);
            }
            Token = value;
        }

        /// <summary>Initializes a new instance of the <see cref="ProgressToken"/> with a specified value.</summary>
        /// <param name="value">The required ID value.</param>
        public ProgressToken(long value)
        {
            // Box the long. Progress tokens are almost always strings in practice, so this should be rare.
            Token = value;
        }

        /// <summary>Gets the underlying object for this token.</summary>
        /// <remarks>This will either be a <see cref="string"/>, a boxed <see cref="long"/>, or <see langword="null"/>.</remarks>
        public object? Token { get; }

        /// <inheritdoc />
        public override string? ToString() =>
            Token is string stringValue ? stringValue :
            Token is long longValue ? longValue.ToString(CultureInfo.InvariantCulture) :
            null;

        /// <inheritdoc />
        public bool Equals(ProgressToken other) => Equals(Token, other.Token);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is ProgressToken other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => Token?.GetHashCode() ?? 0;

        /// <inheritdoc />
        public static bool operator ==(ProgressToken left, ProgressToken right) => left.Equals(right);

        /// <inheritdoc />
        public static bool operator !=(ProgressToken left, ProgressToken right) => !left.Equals(right);

        /// <summary>
        /// Provides a <see cref="JsonConverter"/> for <see cref="ProgressToken"/> that handles both string and number values.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public sealed class Converter : JsonConverter<ProgressToken>
        {
            public override ProgressToken ReadJson(JsonReader reader, Type objectType, ProgressToken existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                return reader.TokenType switch
                {
                    JsonToken.String => new(reader.Value?.ToString()),
                    JsonToken.Integer => new(Convert.ToInt64(reader.Value)),
                    _ => throw new JsonException("progressToken must be a string or an integer"),
                };
            }

            public override void WriteJson(JsonWriter writer, ProgressToken value, JsonSerializer serializer)
            {
                if (writer is null)
                {
                    throw new ArgumentNullException("writer");
                }
                switch (value.Token)
                {
                    case string str:
                        writer.WriteValue(str);
                        return;

                    case long longValue:
                        writer.WriteValue(longValue);
                        return;

                    case null:
                        writer.WriteValue(string.Empty);
                        return;
                }

            }
        }
    }
}