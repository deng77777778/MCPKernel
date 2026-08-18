using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;

namespace MCP.Protocol
{
    /// <summary>
    /// Represents an out-of-band notification used to inform the receiver of a progress update for a long-running request.
    /// </summary>
    /// <remarks>
    /// See the <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see> for more details.
    /// </remarks>
    [JsonConverter(typeof(Converter))]
    public sealed class ProgressNotificationParams : NotificationParams
    {
        /// <summary>
        /// Gets or sets the progress token that was given in the initial request that's used to associate this notification with
        /// the corresponding request.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This token acts as a correlation identifier that links progress updates to their corresponding request.
        /// </para>
        /// <para>
        /// When an endpoint initiates a request with a <see cref="ProgressToken"/> in its metadata,
        /// the receiver can send progress notifications using this same token. This allows both sides to
        /// correlate the notifications with the original request.
        /// </para>
        /// </remarks>
        public ProgressToken ProgressToken { get; set; }

        ///// <summary>
        ///// Gets or sets the progress thus far.
        ///// </summary>
        ///// <remarks>
        ///// This value should increase for each notification issued as part of the same request, even if the total is unknown.
        ///// </remarks>
        //public ProgressNotificationValue Progress { get; set; }

        /// <summary>
        /// Provides a <see cref="JsonConverter"/> for <see cref="ProgressNotificationParams"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public sealed class Converter : JsonConverter<ProgressNotificationParams>
        {
            /// <inheritdoc />
            public override ProgressNotificationParams ReadJson(JsonReader reader, Type objectType, ProgressNotificationParams existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                JObject obj = JObject.Load(reader);

                ProgressToken? progressToken = null;
                float? progress = null;
                float? total = null;
                string message = null;
                JObject meta = null;

                foreach (var property in obj.Properties())
                {
                    switch (property.Name)
                    {
                        case "progressToken":
                            progressToken = property.Value.ToObject<ProgressToken>(serializer);
                            break;

                        case "progress":
                            progress = (float)property.Value;
                            break;

                        case "total":
                            total = (float)property.Value;
                            break;

                        case "message":
                            message = (string)property.Value;
                            break;

                        case "_meta":
                            if (property.Value is JObject metaObj)
                            {
                                meta = metaObj;
                            }
                            break;
                    }
                }

                if (progress == null)
                {
                    throw new JsonException("Missing required property 'progress'.");
                }

                if (progressToken == null)
                {
                    throw new JsonException("Missing required property 'progressToken'.");
                }

                return new ProgressNotificationParams
                {
                    ProgressToken = progressToken.Value,
                    //Progress = new ProgressNotificationValue
                    //{
                    //    Progress = progress.Value,
                    //    Total = total,
                    //    Message = message,
                    //},
                    Meta = meta,
                };
            }

            /// <inheritdoc />
            public override void WriteJson(JsonWriter writer, ProgressNotificationParams value, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("progressToken");
                serializer.Serialize(writer, value.ProgressToken);

                //writer.WritePropertyName("progress");
                //writer.WriteValue(value.Progress.Progress);

                //if (value.Progress.Total.HasValue)
                //{
                //    writer.WritePropertyName("total");
                //    writer.WriteValue(value.Progress.Total.Value);
                //}

                //if (value.Progress.Message != null)
                //{
                //    writer.WritePropertyName("message");
                //    writer.WriteValue(value.Progress.Message);
                //}

                if (value.Meta != null)
                {
                    writer.WritePropertyName("_meta");
                    serializer.Serialize(writer, value.Meta);
                }

                writer.WriteEndObject();
            }
        }
    }
}
