using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MCP.Protocol
{
    /// <summary>
    /// Represents the type of role in the Model Context Protocol conversation.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Role
    {
        /// <summary>
        /// Corresponds to a human user in the conversation.
        /// </summary>
        [JsonProperty("user")]
        User,

        /// <summary>
        /// Corresponds to the AI assistant in the conversation.
        /// </summary>
        [JsonProperty("assistant")] 
        Assistant
    }
}