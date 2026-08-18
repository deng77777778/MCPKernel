#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MCP.Protocol
{
    /// <summary>
    /// Represents a notification message in the JSON-RPC protocol.
    /// </summary>
    /// <remarks>
    /// Notifications are messages that do not require a response and are not matched with a response message.
    /// They are useful for one-way communication, such as log notifications and progress updates.
    /// Unlike requests, notifications do not include an ID field, since there will be no response to match with it.
    /// </remarks>
    public sealed class JsonRpcNotification : JsonRpcMessage
    {
        /// <summary>
        /// Gets or sets the name of the notification method.
        /// </summary>
        [JsonProperty("method")]
        public string? Method { get; set; }

        /// <summary>
        /// Gets or sets optional parameters for the notification.
        /// </summary>
        [JsonProperty("params")]
        public JToken? Params { get; set; }
    }
}
