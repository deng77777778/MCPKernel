#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MCP.AI
{
    [JsonConverter(typeof(AnnotatedRegionConverter))]
    public class AnnotatedRegion
    {
        /// <summary>
        /// 初始化 <see cref="AnnotatedRegion"/> 类的新实例。
        /// </summary>
        public AnnotatedRegion()
        {
        }
    }

    /// <summary>
    /// 用于处理 AnnotatedRegion 多态序列化和反序列化的自定义 JsonConverter。
    /// </summary>
    public class AnnotatedRegionConverter : JsonConverter<AnnotatedRegion>
    {
        private static readonly Dictionary<Type, string> _typeToDiscriminatorMap = new()
        {
            [typeof(TextSpanAnnotatedRegion)] = "textSpan"
            // 根据需要添加其他区域类型
        };

        private static readonly Dictionary<string, Type> _discriminatorToTypeMap = new(StringComparer.Ordinal)
        {
            ["textSpan"] = typeof(TextSpanAnnotatedRegion)
            // 根据需要添加其他区域类型
        };

        private const string TypeDiscriminatorPropertyName = "$type";

        /// <inheritdoc />
        public override bool CanWrite => true;

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, AnnotatedRegion? value, JsonSerializer serializer)
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
        public override AnnotatedRegion? ReadJson(JsonReader reader, Type objectType, AnnotatedRegion? existingValue, bool hasExistingValue, JsonSerializer serializer)
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
                // 没有鉴别器，尝试从属性推断
                return InferAndDeserialize(jsonObject, serializer);
            }

            if (!_discriminatorToTypeMap.TryGetValue(typeDiscriminator, out var targetType))
            {
                // 未知类型，尝试从属性推断
                return InferAndDeserialize(jsonObject, serializer);
            }

            // 反序列化为目标类型
            var result = (AnnotatedRegion?)jsonObject.ToObject(targetType, serializer);

            // 移除已处理的 $type 属性，避免污染
            if (result is not null)
            {
                // 如果目标类型有 AdditionalProperties，可以移除 $type
                // 但对于 AnnotatedRegion，我们不需要额外处理
            }

            return result;
        }

        /// <summary>
        /// 从 JSON 属性推断类型并反序列化。
        /// </summary>
        private static AnnotatedRegion? InferAndDeserialize(JObject jsonObject, JsonSerializer serializer)
        {
            // 如果包含 "start" 或 "end" 属性，则推断为 TextSpanAnnotatedRegion
            if (jsonObject.ContainsKey("start") || jsonObject.ContainsKey("end"))
            {
                return jsonObject.ToObject<TextSpanAnnotatedRegion>(serializer);
            }

            // 默认为基类型
            return jsonObject.ToObject<AnnotatedRegion>(serializer);
        }
    }
}
