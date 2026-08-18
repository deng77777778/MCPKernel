using Newtonsoft.Json;
using System.Diagnostics;

namespace MCP.AI
{
    /// <summary>Describes a location in the associated <see cref="AIContent"/> based on starting and ending character indices.</summary>
    /// <remarks>This <see cref="AnnotatedRegion"/> typically applies to <see cref="TextContent"/>.</remarks>
    [DebuggerDisplay("[{StartIndex}, {EndIndex})")]
    public sealed class TextSpanAnnotatedRegion : AnnotatedRegion
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextSpanAnnotatedRegion"/> class.
        /// </summary>
        public TextSpanAnnotatedRegion()
        {
        }

        /// <summary>
        /// Gets or sets the start character index (inclusive) of the annotated span in the <see cref="AIContent"/>.
        /// </summary>
        /// </summary>
        [JsonProperty("start")]
        public int? StartIndex { get; set; }

        /// <summary>
        /// Gets or sets the end character index (exclusive) of the annotated span in the <see cref="AIContent"/>.
        /// </summary>
        [JsonProperty("end")]
        public int? EndIndex { get; set; }
    }
}