#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MCP.AI
{
    /// <summary>
    /// Represents an annotation on content.
    /// </summary>
    [JsonConverter(typeof(AIAnnotationConverter))]
    public class AIAnnotation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AIAnnotation"/> class.
        /// </summary>
        public AIAnnotation()
        {
        }

        /// <summary>Gets or sets any target regions for the annotation, pointing to where in the associated <see cref="AIContent"/> this annotation applies.</summary>
        /// <remarks>
        /// The most common form of <see cref="AnnotatedRegion"/> is <see cref="TextSpanAnnotatedRegion"/>, which provides starting and ending character indices
        /// for <see cref="TextContent"/>.
        /// </remarks>
        [JsonProperty("annotatedRegions")]
        public IList<AnnotatedRegion>? AnnotatedRegions { get; set; }

        /// <summary>Gets or sets the raw representation of the annotation from an underlying implementation.</summary>
        /// <remarks>
        /// If an <see cref="AIAnnotation"/> is created to represent some underlying object from another object
        /// model, this property can be used to store that original object. This can be useful for debugging or
        /// for enabling a consumer to access the underlying object model, if needed.
        /// </remarks>
        [JsonIgnore]
        public object? RawRepresentation { get; set; }

        /// <summary>
        /// Gets or sets additional metadata specific to the provider or source type.
        /// </summary>
        [JsonProperty("additionalProperties")]
        public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }
    }


    /// <summary>
    /// 用于处理 AIAnnotation 多态序列化和反序列化的自定义 JsonConverter。
    /// </summary>
    public class AIAnnotationConverter : JsonConverter<AIAnnotation>
    {
        private static readonly Dictionary<Type, string> _typeToDiscriminatorMap = new()
        {
            [typeof(CitationAnnotation)] = "citation"
            // 根据需要添加其他注释类型
        };

        private static readonly Dictionary<string, Type> _discriminatorToTypeMap = new(StringComparer.Ordinal)
        {
            ["citation"] = typeof(CitationAnnotation)
            // 根据需要添加其他注释类型
        };

        private const string TypeDiscriminatorPropertyName = "$type";

        /// <inheritdoc />
        public override bool CanWrite => true;

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, AIAnnotation? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            // 先序列化为 JObject
            var jsonObject = JObject.FromObject(value, serializer);

            // 添加类型鉴别器
            if (_typeToDiscriminatorMap.TryGetValue(value.GetType(), out var discriminator))
            {
                jsonObject[TypeDiscriminatorPropertyName] = discriminator;
            }

            jsonObject.WriteTo(writer);
        }

        /// <inheritdoc />
        public override AIAnnotation? ReadJson(JsonReader reader, Type objectType, AIAnnotation? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var jsonObject = JObject.Load(reader);

            // 获取类型鉴别器
            var typeDiscriminator = jsonObject[TypeDiscriminatorPropertyName]?.ToString();

            if (string.IsNullOrEmpty(typeDiscriminator))
            {
                // 没有鉴别器，反序列化为基类型
                return jsonObject.ToObject<AIAnnotation>(serializer);
            }

            if (!_discriminatorToTypeMap.TryGetValue(typeDiscriminator, out var targetType))
            {
                // 未知类型，反序列化为基类型
                return jsonObject.ToObject<AIAnnotation>(serializer);
            }

            // 反序列化为目标类型
            var result = (AIAnnotation?)jsonObject.ToObject(targetType, serializer);

            // 如果目标类型有 AdditionalProperties，移除 $type 避免污染
            if (result?.AdditionalProperties is not null)
            {
                result.AdditionalProperties.Remove(TypeDiscriminatorPropertyName);
            }

            return result;
        }
    }
}
