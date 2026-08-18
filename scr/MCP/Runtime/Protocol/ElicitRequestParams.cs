#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MCP.Protocol
{
    /// <summary>
    /// Represents a message issued from the server to elicit additional information from the user via the client.
    /// </summary>
    public sealed class ElicitRequestParams : RequestParams
    {
        private string mode = "form";
        /// <summary>
        /// Gets or sets the elicitation mode: "form" for in-band data collection or "url" for out-of-band URL navigation.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        ///   <item><description><b>form</b>: Client collects structured data via a form interface. Data is exposed to the client.</description></item>
        ///   <item><description><b>url</b>: Client navigates user to a URL for out-of-band interaction. Sensitive data is not exposed to the client.</description></item>
        /// </list>
        /// </remarks>
        /// <exception cref="ArgumentException">The value is not "form" or "url".</exception>
        [JsonProperty("mode")]
        public string Mode
        {
            get => mode ??= "form";
            set
            {
                if (value is not ("form" or "url"))
                {
                    throw new ArgumentException("Mode must be 'form' or 'url'.", nameof(value));
                }
                mode = value;
            }
        }

        /// <summary>
        /// Gets or sets a unique identifier for this elicitation request.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Used to track and correlate the elicitation across multiple messages, especially for out-of-band flows
        /// that may complete asynchronously.
        /// </para>
        /// <para>
        /// Required for url mode elicitation to enable progress tracking and completion detection.
        /// </para>
        /// </remarks>
        [JsonProperty("elicitationId")]
        public string? ElicitationId { get; set; }

        /// <summary>
        /// Gets or sets the URL to navigate to for out-of-band elicitation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Required when <see cref="Mode"/> is "url". The client should prompt the user for consent
        /// and then navigate to this URL in a user-agent (browser) where the user completes
        /// the required interaction.
        /// </para>
        /// <para>
        /// URLs must not appear in any other field of the elicitation request for security reasons.
        /// </para>
        /// </remarks>
        [JsonProperty("url")]
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the message to present to the user.
        /// </summary>
        /// <remarks>
        /// For form mode, this describes what information is being requested.
        /// For url mode, this explains why the user needs to navigate to the URL.
        /// </remarks>
        [JsonProperty("message")]
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the requested schema for form mode elicitation.
        /// </summary>
        /// <remarks>
        /// Only applicable when <see cref="Mode"/> is "form".
        /// </remarks>
        /// <value>
        /// Possible values are <see cref="StringSchema"/>, <see cref="NumberSchema"/>, <see cref="BooleanSchema"/>,
        /// <see cref="UntitledSingleSelectEnumSchema"/>, <see cref="TitledSingleSelectEnumSchema"/>,
        /// <see cref="UntitledMultiSelectEnumSchema"/>, <see cref="TitledMultiSelectEnumSchema"/>,
        /// </value>
        [JsonProperty("requestedSchema")]
        public RequestSchema? RequestedSchema { get; set; }

        /// <summary>Represents a request schema used in a form mode elicitation request.</summary>
        public sealed class RequestSchema
        {
            private IDictionary<string, PrimitiveSchemaDefinition>? properties;
            /// <summary>Gets the type of the schema.</summary>
            /// <remarks>This value is always "object".</remarks>
            [JsonProperty("type")]
            public string Type => "object";

            /// <summary>Gets or sets the properties of the schema.</summary>
            [JsonProperty("properties")]
            public IDictionary<string, PrimitiveSchemaDefinition> Properties
            {
                get => properties ??= new Dictionary<string, PrimitiveSchemaDefinition>();
                set
                {
                    if (value is null)
                        throw new ArgumentNullException(nameof(value));

                    properties = value;
                }
            }

            /// <summary>Gets or sets the required properties of the schema.</summary>
            [JsonProperty("required")]
            public IList<string>? Required { get; set; }
        }

        /// <summary>
        /// Represents a restricted subset of JSON Schema:
        /// <see cref="StringSchema"/>, <see cref="NumberSchema"/>, <see cref="BooleanSchema"/>,
        /// <see cref="UntitledSingleSelectEnumSchema"/>, <see cref="TitledSingleSelectEnumSchema"/>,
        /// <see cref="UntitledMultiSelectEnumSchema"/>, <see cref="TitledMultiSelectEnumSchema"/>,
        /// </summary>
        [JsonConverter(typeof(Converter))]
        public abstract class PrimitiveSchemaDefinition
        {
            /// <summary>Prevents external derivations.</summary>
            protected private PrimitiveSchemaDefinition()
            {
            }
            /// <summary>
            /// Gets the default value for this schema as a <see cref="JToken"/>, if one is defined.
            /// </summary>
            internal JToken? GetDefaultAsJsonToken()
            {
                switch (this)
                {
                    case StringSchema { Default: not null } stringSchema:
                        return new JValue(stringSchema.Default);

                    case NumberSchema { Default: not null } numberSchema:
                        return new JValue(numberSchema.Default.Value);

                    case BooleanSchema { Default: not null } booleanSchema:
                        return new JValue(booleanSchema.Default.Value);

                    case UntitledSingleSelectEnumSchema { Default: not null } untitledSingle:
                        return new JValue(untitledSingle.Default);

                    case TitledSingleSelectEnumSchema { Default: not null } titledSingle:
                        return new JValue(titledSingle.Default);

                    case UntitledMultiSelectEnumSchema { Default: not null } untitledMulti:
                        return JArray.FromObject(untitledMulti.Default);

                    case TitledMultiSelectEnumSchema { Default: not null } titledMulti:
                        return JArray.FromObject(titledMulti.Default);
                    default:
                        return null;
                }
            }
            /// <summary>Gets or sets the type of the schema.</summary>
            [JsonProperty("type")]
            public abstract string Type { get; set; }

            /// <summary>Gets or sets a title for the schema.</summary>
            [JsonProperty("title")]
            public string? Title { get; set; }

            /// <summary>Gets or sets a description for the schema.</summary>
            [JsonProperty("description")]
            public string? Description { get; set; }

            /// <summary>
            /// Provides a <see cref="JsonConverter"/> for <see cref="PrimitiveSchemaDefinition"/>.
            /// </summary>
            /// <remarks>
            /// Provides a polymorphic converter for the <see cref="PrimitiveSchemaDefinition"/> class that doesn't require
            /// setting <see cref="JsonSerializerOptions.AllowOutOfOrderMetadataProperties"/> explicitly.
            /// </remarks>
            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class Converter : JsonConverter<PrimitiveSchemaDefinition>
            {
                /// <inheritdoc/>
                public override PrimitiveSchemaDefinition? ReadJson(JsonReader reader, Type objectType, PrimitiveSchemaDefinition? existingValue, bool hasExistingValue, JsonSerializer serializer)
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
                    string? title = (string?)obj["title"];
                    string? description = (string?)obj["description"];
                    int? minLength = (int?)obj["minLength"];
                    int? maxLength = (int?)obj["maxLength"];
                    string? format = (string?)obj["format"];
                    double? minimum = (double?)obj["minimum"];
                    double? maximum = (double?)obj["maximum"];
                    bool? defaultBool = null;
                    double? defaultNumber = null;
                    string? defaultString = null;
                    IList<string>? defaultStringArray = null;
                    IList<string>? enumValues = null;
                    IList<string>? enumNames = null;
                    IList<EnumSchemaOption>? oneOf = null;
                    int? minItems = null;
                    int? maxItems = null;
                    object? items = null; // Can be UntitledEnumItemsSchema or TitledEnumItemsSchema

                    // Handle default value
                    JToken? defaultToken = obj["default"];
                    if (defaultToken != null)
                    {
                        switch (defaultToken.Type)
                        {
                            case JTokenType.Boolean:
                                defaultBool = (bool)defaultToken;
                                break;
                            case JTokenType.Integer:
                            case JTokenType.Float:
                                defaultNumber = (double)defaultToken;
                                break;
                            case JTokenType.String:
                                defaultString = (string?)defaultToken;
                                break;
                            case JTokenType.Array:
                                defaultStringArray = defaultToken.ToObject<IList<string>>(serializer);
                                break;
                        }
                    }

                    // Handle enum
                    JToken? enumToken = obj["enum"];
                    if (enumToken != null && enumToken.Type == JTokenType.Array)
                    {
                        enumValues = enumToken.ToObject<IList<string>>(serializer);
                    }

                    // Handle enumNames
                    JToken? enumNamesToken = obj["enumNames"];
                    if (enumNamesToken != null && enumNamesToken.Type == JTokenType.Array)
                    {
                        enumNames = enumNamesToken.ToObject<IList<string>>(serializer);
                    }

                    // Handle oneOf
                    JToken? oneOfToken = obj["oneOf"];
                    if (oneOfToken != null && oneOfToken.Type == JTokenType.Array)
                    {
                        oneOf = DeserializeEnumOptions(oneOfToken, serializer);
                    }

                    // Handle items
                    JToken? itemsToken = obj["items"];
                    if (itemsToken != null && itemsToken.Type == JTokenType.Object)
                    {
                        items = DeserializeEnumItemsSchema(itemsToken, serializer);
                    }

                    // Handle minItems and maxItems
                    minItems = (int?)obj["minItems"];
                    maxItems = (int?)obj["maxItems"];

                    if (type == null)
                    {
                        throw new JsonException("The 'type' property is required.");
                    }

                    PrimitiveSchemaDefinition? psd = null;
                    switch (type)
                    {
                        case "string":
                            if (oneOf != null)
                            {
                                // TitledSingleSelectEnumSchema
                                psd = new TitledSingleSelectEnumSchema
                                {
                                    OneOf = oneOf,
                                    Default = defaultString,
                                };
                            }
                            else if (enumValues != null)
                            {
                                if (enumNames == null)
                                {
                                    // UntitledSingleSelectEnumSchema
                                    psd = new UntitledSingleSelectEnumSchema
                                    {
                                        Enum = enumValues,
                                        Default = defaultString,
                                    };
                                }
                            }
                            else
                            {
                                psd = new StringSchema
                                {
                                    MinLength = minLength,
                                    MaxLength = maxLength,
                                    Format = format,
                                    Default = defaultString,
                                };
                            }
                            break;

                        case "array":
                            if (items is TitledEnumItemsSchema titledItems)
                            {
                                // TitledMultiSelectEnumSchema
                                psd = new TitledMultiSelectEnumSchema
                                {
                                    MinItems = minItems,
                                    MaxItems = maxItems,
                                    Items = titledItems,
                                    Default = defaultStringArray,
                                };
                            }
                            else if (items is UntitledEnumItemsSchema untitledItems)
                            {
                                // UntitledMultiSelectEnumSchema
                                psd = new UntitledMultiSelectEnumSchema
                                {
                                    MinItems = minItems,
                                    MaxItems = maxItems,
                                    Items = untitledItems,
                                    Default = defaultStringArray,
                                };
                            }
                            break;

                        case "integer":
                        case "number":
                            psd = new NumberSchema
                            {
                                Minimum = minimum,
                                Maximum = maximum,
                                Default = defaultNumber,
                            };
                            break;

                        case "boolean":
                            psd = new BooleanSchema
                            {
                                Default = defaultBool,
                            };
                            break;
                    }

                    if (psd != null)
                    {
                        psd.Type = type;
                        psd.Title = title;
                        psd.Description = description;
                    }

                    return psd;
                }

                private static List<EnumSchemaOption> DeserializeEnumOptions(JToken oneOfToken, JsonSerializer serializer)
                {
                    if (oneOfToken.Type != JTokenType.Array)
                    {
                        throw new JsonException("Expected array for oneOf property.");
                    }

                    var options = new List<EnumSchemaOption>();
                    foreach (JToken item in oneOfToken)
                    {
                        if (item.Type != JTokenType.Object)
                        {
                            throw new JsonException("Expected object in oneOf array.");
                        }

                        string? constValue = (string?)item["const"];
                        string? titleValue = (string?)item["title"];

                        if (constValue == null || titleValue == null)
                        {
                            throw new JsonException("Each option in oneOf must have both 'const' and 'title' properties.");
                        }

                        options.Add(new EnumSchemaOption { Const = constValue, Title = titleValue });
                    }

                    return options;
                }
                private static object DeserializeEnumItemsSchema(JToken itemsToken, JsonSerializer serializer)
                {
                    if (itemsToken.Type != JTokenType.Object)
                    {
                        throw new JsonException("Expected object for items property.");
                    }

                    string? type = (string?)itemsToken["type"];
                    IList<string>? enumValues = null;
                    IList<EnumSchemaOption>? anyOf = null;

                    // Handle enum
                    JToken? enumToken = itemsToken["enum"];
                    if (enumToken != null && enumToken.Type == JTokenType.Array)
                    {
                        enumValues = enumToken.ToObject<IList<string>>(serializer);
                    }

                    // Handle anyOf
                    JToken? anyOfToken = itemsToken["anyOf"];
                    if (anyOfToken != null && anyOfToken.Type == JTokenType.Array)
                    {
                        anyOf = DeserializeEnumOptions(anyOfToken, serializer);
                    }

                    // Determine which type to create based on the properties
                    if (anyOf != null)
                    {
                        return new TitledEnumItemsSchema { AnyOf = anyOf };
                    }
                    else if (enumValues != null)
                    {
                        return new UntitledEnumItemsSchema { Type = type ?? "string", Enum = enumValues };
                    }
                    else
                    {
                        throw new JsonException("Items schema must have either 'enum' or 'anyOf' property.");
                    }
                }
                /// <inheritdoc/>
                /// <inheritdoc/>
                public override void WriteJson(JsonWriter writer, PrimitiveSchemaDefinition? value, JsonSerializer serializer)
                {
                    if (value == null)
                    {
                        writer.WriteNull();
                        return;
                    }

                    writer.WriteStartObject();

                    writer.WritePropertyName("type");
                    writer.WriteValue(value.Type);

                    if (value.Title != null)
                    {
                        writer.WritePropertyName("title");
                        writer.WriteValue(value.Title);
                    }

                    if (value.Description != null)
                    {
                        writer.WritePropertyName("description");
                        writer.WriteValue(value.Description);
                    }

                    switch (value)
                    {
                        case StringSchema stringSchema:
                            if (stringSchema.MinLength.HasValue)
                            {
                                writer.WritePropertyName("minLength");
                                writer.WriteValue(stringSchema.MinLength.Value);
                            }
                            if (stringSchema.MaxLength.HasValue)
                            {
                                writer.WritePropertyName("maxLength");
                                writer.WriteValue(stringSchema.MaxLength.Value);
                            }
                            if (stringSchema.Format != null)
                            {
                                writer.WritePropertyName("format");
                                writer.WriteValue(stringSchema.Format);
                            }
                            if (stringSchema.Default != null)
                            {
                                writer.WritePropertyName("default");
                                writer.WriteValue(stringSchema.Default);
                            }
                            break;

                        case NumberSchema numberSchema:
                            if (numberSchema.Minimum.HasValue)
                            {
                                writer.WritePropertyName("minimum");
                                writer.WriteValue(numberSchema.Minimum.Value);
                            }
                            if (numberSchema.Maximum.HasValue)
                            {
                                writer.WritePropertyName("maximum");
                                writer.WriteValue(numberSchema.Maximum.Value);
                            }
                            if (numberSchema.Default.HasValue)
                            {
                                writer.WritePropertyName("default");
                                writer.WriteValue(numberSchema.Default.Value);
                            }
                            break;

                        case BooleanSchema booleanSchema:
                            if (booleanSchema.Default.HasValue)
                            {
                                writer.WritePropertyName("default");
                                writer.WriteValue(booleanSchema.Default.Value);
                            }
                            break;

                        case UntitledSingleSelectEnumSchema untitledSingleSelect:
                            if (untitledSingleSelect.Enum != null)
                            {
                                writer.WritePropertyName("enum");
                                serializer.Serialize(writer, untitledSingleSelect.Enum);
                            }
                            if (untitledSingleSelect.Default != null)
                            {
                                writer.WritePropertyName("default");
                                writer.WriteValue(untitledSingleSelect.Default);
                            }
                            break;

                        case TitledSingleSelectEnumSchema titledSingleSelect:
                            if (titledSingleSelect.OneOf != null && titledSingleSelect.OneOf.Count > 0)
                            {
                                writer.WritePropertyName("oneOf");
                                SerializeEnumOptions(writer, titledSingleSelect.OneOf, serializer);
                            }
                            if (titledSingleSelect.Default != null)
                            {
                                writer.WritePropertyName("default");
                                writer.WriteValue(titledSingleSelect.Default);
                            }
                            break;

                        case UntitledMultiSelectEnumSchema untitledMultiSelect:
                            if (untitledMultiSelect.MinItems.HasValue)
                            {
                                writer.WritePropertyName("minItems");
                                writer.WriteValue(untitledMultiSelect.MinItems.Value);
                            }
                            if (untitledMultiSelect.MaxItems.HasValue)
                            {
                                writer.WritePropertyName("maxItems");
                                writer.WriteValue(untitledMultiSelect.MaxItems.Value);
                            }
                            writer.WritePropertyName("items");
                            SerializeUntitledEnumItemsSchema(writer, untitledMultiSelect.Items, serializer);
                            if (untitledMultiSelect.Default != null)
                            {
                                writer.WritePropertyName("default");
                                serializer.Serialize(writer, untitledMultiSelect.Default);
                            }
                            break;

                        case TitledMultiSelectEnumSchema titledMultiSelect:
                            if (titledMultiSelect.MinItems.HasValue)
                            {
                                writer.WritePropertyName("minItems");
                                writer.WriteValue(titledMultiSelect.MinItems.Value);
                            }
                            if (titledMultiSelect.MaxItems.HasValue)
                            {
                                writer.WritePropertyName("maxItems");
                                writer.WriteValue(titledMultiSelect.MaxItems.Value);
                            }
                            if (titledMultiSelect.Items != null)
                            {
                                writer.WritePropertyName("items");
                                SerializeTitledEnumItemsSchema(writer, titledMultiSelect.Items, serializer);
                            }
                            if (titledMultiSelect.Default != null)
                            {
                                writer.WritePropertyName("default");
                                serializer.Serialize(writer, titledMultiSelect.Default);
                            }
                            break;
                        default:
                            throw new JsonException($"Unexpected schema type: {value.GetType().Name}");
                    }

                    writer.WriteEndObject();
                }

                private static void SerializeEnumOptions(JsonWriter writer, IList<EnumSchemaOption> options, JsonSerializer serializer)
                {
                    writer.WriteStartArray();
                    foreach (var option in options)
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("const");
                        writer.WriteValue(option.Const);
                        writer.WritePropertyName("title");
                        writer.WriteValue(option.Title);
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();
                }

                private static void SerializeUntitledEnumItemsSchema(JsonWriter writer, UntitledEnumItemsSchema? itemsSchema, JsonSerializer serializer)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("type");
                    writer.WriteValue(itemsSchema?.Type);
                    writer.WritePropertyName("enum");
                    serializer.Serialize(writer, itemsSchema?.Enum);
                    writer.WriteEndObject();
                }

                private static void SerializeTitledEnumItemsSchema(JsonWriter writer, TitledEnumItemsSchema itemsSchema, JsonSerializer serializer)
                {
                    if (itemsSchema.AnyOf is not null)
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("anyOf");
                        SerializeEnumOptions(writer, itemsSchema.AnyOf, serializer);
                        writer.WriteEndObject();
                    }
                }
            }
        }

        /// <summary>Represents a schema for a string type.</summary>
        public sealed class StringSchema : PrimitiveSchemaDefinition
        {
            private int? minLength;
            private int? maxLength;
            private string? format;

            /// <inheritdoc/>
            [JsonProperty("type")]
            public override string Type
            {
                get => "string";
                set
                {
                    if (value is not "string")
                    {
                        throw new ArgumentException("Type must be 'string'.", nameof(value));
                    }
                }
            }

            /// <summary>Gets or sets the minimum length for the string.</summary>
            [JsonProperty("minLength")]
            public int? MinLength
            {
                get => minLength;
                set
                {
                    if (value < 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), "Minimum length cannot be negative.");
                    }

                    minLength = value;
                }
            }

            /// <summary>Gets or sets the maximum length for the string.</summary>
            [JsonProperty("maxLength")]
            public int? MaxLength
            {
                get => maxLength;
                set
                {
                    if (value < 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), "Maximum length cannot be negative.");
                    }

                    minLength = value;
                }
            }

            /// <summary>Gets or sets a specific format for the string ("email", "uri", "date", or "date-time").</summary>
            [JsonProperty("format")]
            public string? Format
            {
                get => format;
                set
                {
                    if (value is not (null or "email" or "uri" or "date" or "date-time"))
                    {
                        throw new ArgumentException("Format must be 'email', 'uri', 'date', or 'date-time'.", nameof(value));
                    }

                    format = value;
                }
            }

            /// <summary>Gets or sets the default value for the string.</summary>
            [JsonProperty("default")]
            public string? Default { get; set; }
        }

        /// <summary>Represents a schema for a number or integer type.</summary>
        public sealed class NumberSchema : PrimitiveSchemaDefinition
        {
            private string? type;
            /// <inheritdoc/>
            public override string Type
            {
                get => type ??= "number";
                set
                {
                    if (value is not ("number" or "integer"))
                    {
                        throw new ArgumentException("Type must be 'number' or 'integer'.", nameof(value));
                    }

                    type = value;
                }
            }

            /// <summary>Gets or sets the minimum allowed value.</summary>
            [JsonProperty("minimum")]
            public double? Minimum { get; set; }

            /// <summary>Gets or sets the maximum allowed value.</summary>
            [JsonProperty("maximum")]
            public double? Maximum { get; set; }

            /// <summary>Gets or sets the default value for the number.</summary>
            [JsonProperty("default")]
            public double? Default { get; set; }
        }

        /// <summary>Represents a schema for a Boolean type.</summary>
        public sealed class BooleanSchema : PrimitiveSchemaDefinition
        {
            /// <inheritdoc/>
            [JsonProperty("type")]
            public override string Type
            {
                get => "boolean";
                set
                {
                    if (value is not "boolean")
                    {
                        throw new ArgumentException("Type must be 'boolean'.", nameof(value));
                    }
                }
            }

            /// <summary>Gets or sets the default value for the Boolean.</summary>
            [JsonProperty("default")]
            public bool? Default { get; set; }
        }

        /// <summary>
        /// Represents a schema for single-selection enumeration without display titles for options.
        /// </summary>
        public sealed class UntitledSingleSelectEnumSchema : PrimitiveSchemaDefinition
        {
            private IList<string> enumList = new List<string>();
            /// <inheritdoc/>
            [JsonProperty("type")]
            public override string Type
            {
                get => "string";
                set
                {
                    if (value is not "string")
                    {
                        throw new ArgumentException("Type must be 'string'.", nameof(value));
                    }
                }
            }

            /// <summary>Gets or sets the list of allowed string values for the enum.</summary>
            [JsonProperty("enum")]
            public IList<string> Enum
            {
                get => enumList;
                set
                {
                    if (value is null)
                        throw new ArgumentNullException(nameof(value));
                    enumList = value;
                }
            }

            /// <summary>Gets or sets the default value for the enum.</summary>
            [JsonProperty("default")]
            public string? Default { get; set; }
        }

        /// <summary>
        /// Represents a single option in a titled enum schema with a constant value and display title.
        /// </summary>
        public sealed class EnumSchemaOption
        {
            /// <summary>Gets or sets the constant value for this option.</summary>
            [JsonProperty("const")]
            public string? Const { get; set; }

            /// <summary>Gets or sets the display title for this option.</summary>
            [JsonProperty("title")]
            public string? Title { get; set; }
        }

        /// <summary>
        /// Represents a schema for single-selection enumeration with display titles for each option.
        /// </summary>
        public sealed class TitledSingleSelectEnumSchema : PrimitiveSchemaDefinition
        {
            private IList<EnumSchemaOption> oneOf = new List<EnumSchemaOption>();
            /// <inheritdoc/>
            [JsonProperty("type")]
            public override string Type
            {
                get => "string";
                set
                {
                    if (value is not "string")
                    {
                        throw new ArgumentException("Type must be 'string'.", nameof(value));
                    }
                }
            }

            /// <summary>Gets or sets the list of enum options with their constant values and display titles.</summary>
            [JsonProperty("oneOf")]
            public IList<EnumSchemaOption> OneOf
            {
                get => oneOf;
                set
                {
                    if (value is null)
                        throw new ArgumentNullException(nameof(value));
                    oneOf = value;
                }
            }

            /// <summary>Gets or sets the default value for the enum.</summary>
            [JsonProperty("default")]
            public string? Default { get; set; }
        }

        /// <summary>
        /// Represents the items schema for untitled multi-select enum arrays.
        /// </summary>
        public sealed class UntitledEnumItemsSchema
        {
            /// <summary>Gets or sets the type of the items.</summary>
            [JsonProperty("type")]
            public string Type { get; set; } = "string";

            /// <summary>Gets or sets the list of allowed string values.</summary>
            [JsonProperty("enum")]
            public IList<string>? Enum { get; set; }
        }

        /// <summary>
        /// Represents the items schema for titled multi-select enum arrays.
        /// </summary>
        public sealed class TitledEnumItemsSchema
        {
            /// <summary>Gets or sets the list of enum options with constant values and display titles.</summary>
            [JsonProperty("anyOf")]
            public IList<EnumSchemaOption>? AnyOf { get; set; }
        }

        /// <summary>
        /// Represents a schema for multiple-selection enumeration without display titles for options.
        /// </summary>
        public sealed class UntitledMultiSelectEnumSchema : PrimitiveSchemaDefinition
        {
            /// <inheritdoc/>
            [JsonProperty("type")]
            public override string Type
            {
                get => "array";
                set
                {
                    if (value is not "array")
                    {
                        throw new ArgumentException("Type must be 'array'.", nameof(value));
                    }
                }
            }

            /// <summary>Gets or sets the minimum number of items that can be selected.</summary>
            [JsonProperty("minItems")]
            public int? MinItems { get; set; }

            /// <summary>Gets or sets the maximum number of items that can be selected.</summary>
            [JsonProperty("maxItems")]
            public int? MaxItems { get; set; }

            /// <summary>Gets or sets the schema for items in the array.</summary>
            [JsonProperty("items")]
            public UntitledEnumItemsSchema? Items { get; set; }

            /// <summary>Gets or sets the default values for the enum.</summary>
            [JsonProperty("default")]
            public IList<string>? Default { get; set; }
        }

        /// <summary>
        /// Represents a schema for multiple-selection enumeration with display titles for each option.
        /// </summary>
        public sealed class TitledMultiSelectEnumSchema : PrimitiveSchemaDefinition
        {
            /// <inheritdoc/>
            [JsonProperty("type")]
            public override string Type
            {
                get => "array";
                set
                {
                    if (value is not "array")
                    {
                        throw new ArgumentException("Type must be 'array'.", nameof(value));
                    }
                }
            }

            /// <summary>Gets or sets the minimum number of items that can be selected.</summary>
            [JsonProperty("minItems")]
            public int? MinItems { get; set; }

            /// <summary>Gets or sets the maximum number of items that can be selected.</summary>
            [JsonProperty("maxItems")]
            public int? MaxItems { get; set; }

            /// <summary>Gets or sets the schema for items in the array.</summary>
            [JsonProperty("items")]
            public TitledEnumItemsSchema? Items { get; set; }

            /// <summary>Gets or sets the default values for the enum.</summary>
            [JsonProperty("default")]
            public IList<string>? Default { get; set; }
        }

    }
}
