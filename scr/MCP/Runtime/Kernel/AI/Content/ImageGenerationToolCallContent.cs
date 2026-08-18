namespace MCP.AI
{
    /// <summary>
    /// Represents the invocation of an image generation tool call by a hosted service.
    /// </summary>
    public sealed class ImageGenerationToolCallContent : ToolCallContent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageGenerationToolCallContent"/> class.
        /// </summary>
        /// <param name="callId">The tool call ID.</param>
        public ImageGenerationToolCallContent(string callId)
            : base(callId)
        {
        }
    }
}
