using Newtonsoft.Json;
using System.Collections.Generic;

namespace MCP.Protocol
{
    /// <summary>
    /// Represents the payload for the <c>URL_ELICITATION_REQUIRED</c> JSON-RPC error.
    /// </summary>
    public sealed class UrlElicitationRequiredErrorData
    {
        /// <summary>
        /// Gets or sets the elicitations that must be completed before retrying the original request.
        /// </summary>
        [JsonProperty("elicitations")]
        public IList<ElicitRequestParams> Elicitations { get; set; }
    }
}
