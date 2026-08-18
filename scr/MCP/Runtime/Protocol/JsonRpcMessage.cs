#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;

namespace MCP.Protocol
{
    /// <summary>
    /// Represents any JSON-RPC message used in the Model Context Protocol (MCP).
    /// </summary>
    /// <remarks>
    /// This interface serves as the foundation for all message types in the JSON-RPC 2.0 protocol
    /// used by MCP, including requests, responses, notifications, and errors. JSON-RPC is a stateless,
    /// lightweight remote procedure call (RPC) protocol that uses JSON as its data format.
    /// </remarks>
    public abstract class JsonRpcMessage
    {
        /// <summary>Prevent external derivations.</summary>
        private protected JsonRpcMessage()
        {
        }

        /// <summary>
        /// Gets or sets the JSON-RPC protocol version used.
        /// </summary>
        /// <inheritdoc />
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        /// <summary>
        /// Gets or sets the contextual information for this JSON-RPC message.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property contains transport-specific and runtime context information that accompanies
        /// JSON-RPC messages but is not serialized as part of the JSON-RPC payload. This includes
        /// transport references, execution context, and authenticated user information.
        /// </para>
        /// <para>
        /// This property should only be set when implementing a custom <see cref="ITransport"/>
        /// that needs to pass additional per-message context or to pass a <see cref="JsonRpcMessageContext.User"/>
        /// to <see cref="StreamableHttpServerTransport.HandlePostRequestAsync(JsonRpcMessage, Stream, CancellationToken)"/>
        /// or <see cref="SseResponseStreamTransport.OnMessageReceivedAsync(JsonRpcMessage, CancellationToken)"/>.
        /// </para>
        /// </remarks>
        [JsonIgnore]
        public JsonRpcMessageContext? Context { get; set; }

        /// <summary>
        /// Provides a <see cref="JsonConverter"/> for <see cref="JsonRpcMessage"/> messages,
        /// handling polymorphic deserialization of different message types.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This converter is responsible for correctly deserializing JSON-RPC messages into their appropriate
        /// concrete types based on the message structure. It analyzes the JSON payload and determines if it
        /// represents a request, notification, successful response, or error response.
        /// </para>
        /// <para>
        /// The type determination rules follow the JSON-RPC 2.0 specification:
        /// <list type="bullet">
        /// <item><description>Messages with "method" and "id" properties are deserialized as <see cref="JsonRpcRequest"/>.</description></item>
        /// <item><description>Messages with "method" but no "id" property are deserialized as <see cref="JsonRpcNotification"/>.</description></item>
        /// <item><description>Messages with "id" and "result" properties are deserialized as <see cref="JsonRpcResponse"/>.</description></item>
        /// <item><description>Messages with "id" and "error" properties are deserialized as <see cref="JsonRpcError"/>.</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public sealed class Converter : JsonConverter<JsonRpcMessage>
        {
            /// <inheritdoc/>
            public override JsonRpcMessage ReadJson(JsonReader reader, Type objectType, JsonRpcMessage? existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                if (reader.TokenType != JsonToken.StartObject)
                {
                    throw new JsonException("Expected StartObject token");
                }

                JObject obj = JObject.Load(reader);

                // Local variables for parsed message data
                bool hasJsonRpc = false;
                RequestId id = default;
                bool hasId = false;
                string? method = null;
                JToken? parameters = null;
                JsonRpcErrorDetail? error = null;
                JToken? result = null;
                bool hasResult = false;

                // Check jsonrpc version
                JToken? jsonrpcToken = obj["jsonrpc"];
                if (jsonrpcToken != null)
                {
                    if (jsonrpcToken.Type != JTokenType.String || (string?)jsonrpcToken != "2.0")
                    {
                        throw new JsonException("Invalid jsonrpc version");
                    }
                    hasJsonRpc = true;
                }

                // Parse id
                JToken? idToken = obj["id"];
                if (idToken != null)
                {
                    hasId = true;
                    if (idToken.Type == JTokenType.String)
                    {
                        id = new RequestId((string)idToken!);
                    }
                    else if (idToken.Type == JTokenType.Integer)
                    {
                        id = new RequestId((long)idToken);
                    }
                    else if (idToken.Type == JTokenType.Null)
                    {
                        id = default;
                    }
                    else
                    {
                        throw new JsonException("Invalid id type. Must be string, number, or null.");
                    }
                }

                // Parse method
                JToken? methodToken = obj["method"];
                if (methodToken != null && methodToken.Type == JTokenType.String)
                {
                    method = (string?)methodToken;
                }

                // Parse params
                JToken? paramsToken = obj["params"];
                if (paramsToken != null)
                {
                    parameters = paramsToken;
                }

                // Parse error
                JToken? errorToken = obj["error"];
                if (errorToken != null && errorToken.Type == JTokenType.Object)
                {
                    error = errorToken.ToObject<JsonRpcErrorDetail>(serializer);
                }

                // Parse result
                JToken? resultToken = obj["result"];
                if (resultToken != null)
                {
                    result = resultToken;
                    hasResult = true;
                }

                // All JSON-RPC messages must have a jsonrpc property with value "2.0"
                if (!hasJsonRpc)
                {
                    throw new JsonException("Missing jsonrpc version");
                }

                // Determine message type based on presence of id and method properties
                if (method != null)
                {
                    if (hasId && id.Id == null)
                    {
                        // A request that carries an explicit `id: null` is malformed.
                        throw new JsonException("Request id must not be null. Per MCP, a request id must be a non-null string or number; omit the id member entirely to send a notification.");
                    }

                    if (id.Id != null)
                    {
                        // Messages with both method and id are requests
                        return new JsonRpcRequest
                        {
                            Id = id,
                            Method = method,
                            Params = parameters
                        };
                    }
                    else
                    {
                        // Messages with a method but no id member are notifications
                        return new JsonRpcNotification
                        {
                            Method = method,
                            Params = parameters
                        };
                    }
                }

                if (id.Id != null)
                {
                    if (error != null)
                    {
                        // Messages with an error and id are error responses
                        return new JsonRpcError
                        {
                            Id = id,
                            Error = error
                        };
                    }

                    if (hasResult)
                    {
                        // Messages with a result and id are success responses
                        return new JsonRpcResponse
                        {
                            Id = id,
                            Result = result
                        };
                    }

                    // Error: Messages with an id but no method, error, or result are invalid
                    throw new JsonException("Response must have either result or error");
                }

                if (error != null)
                {
                    // Per JSON-RPC 2.0, when an error occurs before the request id can be determined
                    // (e.g. parse error or invalid request), the server MUST respond with id=null.
                    return new JsonRpcError
                    {
                        Id = id,
                        Error = error
                    };
                }

                // Error: Messages with neither id nor method are invalid
                throw new JsonException("Invalid JSON-RPC message format");
            }

            /// <inheritdoc/>
            public override void WriteJson(JsonWriter writer, JsonRpcMessage? value, JsonSerializer serializer)
            {
                if (value == null)
                {
                    writer.WriteNull();
                    return;
                }


                switch (value)
                {
                    case JsonRpcRequest request:
                        serializer.Serialize(writer, request);
                        break;
                    case JsonRpcNotification notification:
                        serializer.Serialize(writer, notification);
                        break;
                    case JsonRpcResponse response:
                        serializer.Serialize(writer, response);
                        break;
                    case JsonRpcError error:
                        serializer.Serialize(writer, error);
                        break;
                    default:
                        throw new JsonException($"Unknown JSON-RPC message type: {value.GetType()}");
                }
            }
        }
    }
}
