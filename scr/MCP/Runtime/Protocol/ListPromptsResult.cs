using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MCP.Protocol
{
    /// <summary>
    /// Represents a server's response to a <see cref="RequestMethods.PromptsList"/> request from the client, containing available prompts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This result is returned when a client sends a <see cref="RequestMethods.PromptsList"/> request to discover available prompts on the server.
    /// </para>
    /// <para>
    /// It inherits from <see cref="PaginatedResult"/>, allowing for paginated responses when there are many prompts.
    /// The server can provide the <see cref="PaginatedResult.NextCursor"/> property to indicate there are more
    /// prompts available beyond what was returned in the current response.
    /// </para>
    /// <para>
    /// See the <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see> for details.
    /// </para>
    /// </remarks>
    public sealed class ListPromptsResult : PaginatedResult, ICacheableResult
    {
        /// <summary>
        /// Gets or sets a list of prompts or prompt templates that the server offers.
        /// </summary>
        [JsonProperty("prompts")]
        public IList<Prompt> Prompts { get; set; } = new List<Prompt>();

        /// <inheritdoc />
        [JsonProperty("ttlMs")]
        [JsonConverter(typeof(TimeSpanMillisecondsConverter))]
        public TimeSpan? TimeToLive { get; set; }

        /// <inheritdoc />
        [JsonProperty("cacheScope")]
        [JsonConverter(typeof(CacheScopeConverter))]
        public CacheScope? CacheScope { get; set; }
    }
}
