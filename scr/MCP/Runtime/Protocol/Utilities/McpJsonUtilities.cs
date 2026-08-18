using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace MCP.Protocol
{
    public static partial class McpJsonUtilities
    {
        /// <summary>
        /// Gets the <see cref="JsonSerializerSettings"/> singleton used as the default in JSON serialization operations.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This instance configures the following settings:
        /// <list type="number">
        /// <item>Enables camelCase property naming to match web defaults.</item>
        /// <item>Enables <see cref="NullValueHandling.Ignore"/> as the default handling for null properties.</item>
        /// <item>Enables reading numbers from strings.</item>
        /// </list>
        /// </para>
        /// </remarks>
        public static JsonSerializerSettings DefaultSettings { get; } = CreateDefaultSettings();

        public static JsonSerializer DefaultSerializer { get; } = CreateDefaultSettingsSerializer();

        /// <summary>
        /// Creates default serializer settings for MCP-related serialization.
        /// </summary>
        /// <returns>The configured settings.</returns>
        private static JsonSerializerSettings CreateDefaultSettings()
        {
            var settings = new JsonSerializerSettings
            {
                // Web-like defaults (camelCase)
                ContractResolver = new CamelCasePropertyNamesContractResolver(),

                // Ignore null values when writing
                NullValueHandling = NullValueHandling.Ignore,

                // Allow reading numbers from strings
                FloatParseHandling = FloatParseHandling.Decimal,

                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,

                // Add common converters
                Converters = new List<JsonConverter>
            {
                new StringEnumConverter(),
                new IsoDateTimeConverter(),
                //new JsonRpcMessage.Converter()
            }
            };

            return settings;
        }

        public static JsonSerializer CreateDefaultSettingsSerializer() => JsonSerializer.Create(DefaultSettings);

        public static JToken DefaultJsonSchema { get; } = JToken.Parse("{}");

        public static JToken AsToken(this object value)
        {
            if (value == null)
                return JValue.CreateNull();

            if (value is JToken token)
                return token;

            return JToken.FromObject(value, DefaultSerializer);
        }
        public static JObject AsJObject(this object value)
        {
            if (value is JObject token)
                return token;

            return JObject.FromObject(value, DefaultSerializer);
        }
        internal static bool IsValidMcpToolSchema(JToken element)
        {
            if (element == null || element.Type != JTokenType.Object)
            {
                return false;
            }

            JObject obj = (JObject)element;
            JToken typeToken = obj["type"];

            if (typeToken != null)
            {
                if (typeToken.Type != JTokenType.String ||
                    (string)typeToken != "object")
                {
                    return false;
                }

                return true; // No need to check other properties
            }

            return false; // No type keyword found.
        }

        // Per SEP-2106, a tool's outputSchema may be any valid JSON Schema document — not just
        // schemas with type:"object". Validation is therefore reduced to a structural check
        // matching JSON Schema 2020-12: a schema may be either a JSON object (the usual form
        // with keywords like "type", "properties", etc.) or a boolean (`true` matches anything,
        // `false` matches nothing). Stricter keyword-level validation is intentionally not
        // performed. Pre-2026-07-28 clients still receive the legacy wrapped wire shape — that
        // wiring lives in AIFunctionMcpServerTool.CreateStructuredResponse and McpServerImpl's
        // listToolsHandler.
        internal static bool IsValidToolOutputSchema(JToken element)
        {
            if (element == null)
                return false;

            return element.Type == JTokenType.Object ||
                   element.Type == JTokenType.Boolean;
        }

        private static JToken ParseJsonElement(string json)
        {
            try
            {
                return JToken.Parse(json);
            }
            catch (JsonReaderException)
            {
                return JValue.CreateNull();
            }
        }
    }
}