#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace MCP.Protocol
{
    /// <summary>
    /// Provides a base class for all request parameters.
    /// </summary>
    /// <remarks>
    /// See the <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see> for details.
    /// </remarks>
    public abstract class RequestParams
    {
        /// <summary>Initializes the base request parameter type.</summary>
        protected RequestParams()
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

        /// <summary>
        /// Gets or sets the responses to server-initiated input requests from a previous <see cref="InputRequiredResult"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property is populated when retrying a request after receiving an <see cref="InputRequiredResult"/>.
        /// Each key corresponds to a key from the <see cref="InputRequiredResult.InputRequests"/> map, and
        /// the value is the client's response to that input request.
        /// </para>
        /// </remarks>
        [JsonProperty("inputResponses")]
        public IDictionary<string, InputResponse>? InputResponses { get; set; }

        /// <summary>
        /// Gets or sets opaque request state echoed back from a previous <see cref="InputRequiredResult"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property is populated when retrying a request after receiving an <see cref="InputRequiredResult"/>
        /// that included a <see cref="InputRequiredResult.RequestState"/> value. The client must echo back the
        /// exact value without modification.
        /// </para>
        /// </remarks>
        [JsonProperty("requestState")]
        public string? RequestState { get; set; }

        /// <summary>
        /// Gets the opaque token that will be attached to any subsequent progress notifications.
        /// </summary>
        [JsonIgnore]
        public ProgressToken? ProgressToken
        {
            get
            {
                if (Meta?["progressToken"] is JToken progressToken)
                {
                    switch (progressToken.Type)
                    {
                        case JTokenType.String:
                            return new ProgressToken(progressToken.Value<string>());
                        case JTokenType.Integer:
                            return new ProgressToken(progressToken.Value<long>());
                    }
                }

                return null;
            }
        }
    }
}
