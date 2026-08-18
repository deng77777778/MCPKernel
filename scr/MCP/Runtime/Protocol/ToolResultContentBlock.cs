#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MCP.Protocol
{
    /// <summary>Represents the result of a tool use, provided by the user back to the assistant.</summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    // Sampling support type: this content block only appears inside sampling messages (a tool result returned to
    // the assistant), so it is deprecated together with sampling per SEP-2577.
    [Obsolete]
    public sealed class ToolResultContentBlock : ContentBlock
    {
        /// <inheritdoc/>
        public override string Type => "tool_result";

        /// <summary>
        /// Gets or sets the ID of the tool use this result corresponds to.
        /// </summary>
        /// <remarks>
        /// This value must match the ID from a previous <see cref="ToolUseContentBlock"/>.
        /// </remarks>
        [JsonProperty("toolUseId")]
        public string? ToolUseId { get; set; }

        /// <summary>
        /// Gets or sets the unstructured result content of the tool use.
        /// </summary>
        /// <remarks>
        /// This value has the same format as CallToolResult.Content and can include text, images,
        /// audio, resource links, and embedded resources.
        /// </remarks>
        [JsonProperty("content")]
        public IList<ContentBlock>? Content { get; set; }

        /// <summary>
        /// Gets or sets an optional structured result object.
        /// </summary>
        /// <remarks>
        /// If the tool defined an outputSchema, this object should conform to that schema.
        /// </remarks>
        [JsonProperty("structuredContent")]
        public JToken? StructuredContent { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the tool use resulted in an error.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the tool use resulted in an error; <see langword="false"/> if it succeeded. The default is <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// If <see langword="true"/>, the content typically describes the error that occurred.
        /// </remarks>
        [JsonProperty("isError")]
        public bool? IsError { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get
            {
                if (IsError == true)
                {
                    return $"ToolUseId = {ToolUseId}, IsError = true";
                }
                if (Content is null)
                {
                    return $"ToolUseId = {ToolUseId}";
                }
                // Try to show the result content
                if (Content.Count == 1 && Content[0] is TextContentBlock textBlock)
                {
                    return $"ToolUseId = {ToolUseId}, Result = \"{textBlock.Text}\"";
                }

                if (StructuredContent is not null)
                {
                    try
                    {
                        string json = JsonConvert.SerializeObject(StructuredContent, Formatting.None);
                        return $"ToolUseId = {ToolUseId}, Result = {json}";
                    }
                    catch
                    {
                        // Fall back to content count if GetRawText fails
                    }
                }

                return $"ToolUseId = {ToolUseId}, ContentCount = {Content.Count}";
            }
        }
    }
}

