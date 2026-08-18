using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MCP.Protocol
{
    /// <summary>
    /// Provides a JSON converter for <see cref="IList{T}"/> that handles both array and single object representations.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class SingleItemOrListConverter<T> : JsonConverter<IList<T>>
    where T : class
    {
        /// <inheritdoc />
        public override IList<T> ReadJson(JsonReader reader, Type objectType, IList<T> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonToken.StartArray)
            {
                List<T> list = new List<T>();
                while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                {
                    T item = serializer.Deserialize<T>(reader);
                    if (item != null)
                    {
                        list.Add(item);
                    }
                }

                return list;
            }

            if (reader.TokenType == JsonToken.StartObject)
            {
                T item = serializer.Deserialize<T>(reader);
                return item != null ? new List<T> { item } : new List<T>();
            }

            throw new JsonException($"Unexpected token type: {reader.TokenType}. Expected StartArray or StartObject.");
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, IList<T> value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            if (value.Count == 1)
            {
                serializer.Serialize(writer, value[0]);
                return;
            }

            writer.WriteStartArray();
            foreach (var item in value)
            {
                serializer.Serialize(writer, item);
            }
            writer.WriteEndArray();
        }
    }
}