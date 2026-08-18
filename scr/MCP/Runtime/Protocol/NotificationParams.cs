#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MCP.Protocol
{
    /// <summary>
    /// Provides a base class for notification parameters.
    /// </summary>
    public abstract class NotificationParams
    {
        /// <summary>Initializes the base notification parameter type.</summary>
        protected NotificationParams()
        {
        }

        /// <summary>
        /// Gets or sets metadata reserved by MCP for protocol-level metadata.
        /// </summary>
        /// <remarks>
        /// Implementations must not make assumptions about its contents.
        /// </remarks>
        [JsonProperty("_meta")]
        public JObject? Meta { get; set; }
    }
}