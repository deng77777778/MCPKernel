#nullable enable
using Newtonsoft.Json;
using System.Collections.Generic;

namespace MCP.Protocol
{

    /// <summary>
    /// Represents the capabilities that a client supports.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Capabilities define the features and functionality that a client can handle when communicating with an MCP server.
    /// These are advertised to the server during the initialize handshake.
    /// </para>
    /// <para>
    /// See the <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see> for details.
    /// </para>
    /// </remarks>
    public sealed class ClientCapabilities
    {
        /// <summary>
        /// Gets or sets experimental, non-standard capabilities that the client supports.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="Experimental"/> dictionary allows clients to advertise support for features that are not yet
        /// standardized in the Model Context Protocol specification. This extension mechanism enables
        /// future protocol enhancements while maintaining backward compatibility.
        /// </para>
        /// <para>
        /// Values in this dictionary are implementation-specific and should be coordinated between client
        /// and server implementations. Servers should not assume the presence of any experimental capability
        /// without checking for it first.
        /// </para>
        /// </remarks>
        [JsonProperty("experimental")]
        public IDictionary<string, object>? Experimental { get; set; }

        /// <summary>
        /// Gets or sets the client's elicitation capability, which indicates whether the client
        /// supports elicitation of additional information from the user on behalf of the server.
        /// </summary>
        [JsonProperty("elicitation")]
        public ElicitationCapability? Elicitation { get; set; }

        /// <summary>
        /// Gets or sets optional MCP extensions that the client supports.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Keys are extension identifiers in reverse domain notation with an extension name
        /// (e.g., <c>"io.modelcontextprotocol/oauth-client-credentials"</c>), and values are
        /// per-extension settings objects. An empty object indicates support with no additional settings.
        /// </para>
        /// <para>
        /// Extensions provide a framework for extending the Model Context Protocol while maintaining
        /// interoperability. Clients advertise extension support via this field during the initialization handshake.
        /// </para>
        /// </remarks>
        [JsonProperty("extensions")]
        public IDictionary<string, object>? Extensions { get; set; }
    }
}
