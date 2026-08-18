#nullable enable
using System.Collections.Generic;

namespace MCP.AI
{
    /// <summary>
    /// Represents the result of a MCP server tool call.
    /// </summary>
    /// <remarks>
    /// This content type is used to represent the result of an invocation of an MCP server tool by a hosted service.
    /// It is informational only.
    /// </remarks>
    public sealed class McpServerToolResultContent : ToolResultContent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="McpServerToolResultContent"/> class.
        /// </summary>
        /// <param name="callId">The tool call ID.</param>
        /// <exception cref="ArgumentNullException"><paramref name="callId"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="callId"/> is empty or composed entirely of whitespace.</exception>
        public McpServerToolResultContent(string callId)
            : base(Throw.IfNullOrWhitespace(callId))
        {
        }

        /// <summary>
        /// Gets or sets the output contents of the tool call.
        /// </summary>
        public IList<AIContent>? Outputs { get; set; }
    }

}