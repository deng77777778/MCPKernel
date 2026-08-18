#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MCP.AI
{
    /// <summary>
    /// Represents text content in a chat.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public sealed class TextContent : AIContent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextContent"/> class.
        /// </summary>
        /// <param name="text">The text content.</param>
        public TextContent(string? text)
        {
            Text = text;
        }

        /// <summary>
        /// Gets or sets the text content.
        /// </summary>
        [AllowNull]
        public string Text
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override string ToString() => Text;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay => $"Text = \"{Text}\"";
    }
}