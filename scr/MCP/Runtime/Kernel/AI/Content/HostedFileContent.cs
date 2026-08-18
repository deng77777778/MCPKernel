#nullable enable
using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace MCP.AI
{
    /// <summary>
    /// Represents a file that is hosted by the AI service.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="DataContent"/> which contains the data for a file or blob, this class represents a file that is hosted
    /// by the AI service and referenced by an identifier. Such identifiers are specific to the provider.
    /// </remarks>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public sealed class HostedFileContent : AIContent
    {
        private string? _mediaType;
        private string? _purposeCore;
        private string? _scopeCore;

        /// <summary>
        /// Initializes a new instance of the <see cref="HostedFileContent"/> class.
        /// </summary>
        /// <param name="fileId">The ID of the hosted file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fileId"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="fileId"/> is empty or composed entirely of whitespace.</exception>
        [JsonConstructor]
        public HostedFileContent(string fileId)
        {
            FileId = Throw.IfNullOrWhitespace(fileId);
        }

        /// <summary>
        /// Gets or sets the ID of the hosted file.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is empty or composed entirely of whitespace.</exception>
        [JsonProperty("fileId", Required = Required.Always)]
        public string FileId { get; set; }

        /// <summary>Gets or sets an optional media type (also known as MIME type) associated with the file.</summary>
        /// <exception cref="ArgumentException"><paramref name="value"/> represents an invalid media type.</exception>
        [JsonProperty("mediaType", NullValueHandling = NullValueHandling.Ignore)]
        public string? MediaType
        {
            get => _mediaType;
            set => _mediaType = value is not null ? DataUriParser.ThrowIfInvalidMediaType(value) : value;
        }

        /// <summary>Gets or sets an optional name associated with the file.</summary>
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string? Name { get; set; }

        /// <summary>Gets or sets the size of the file in bytes.</summary>
        [JsonProperty("sizeInBytes", NullValueHandling = NullValueHandling.Ignore)]
        public long? SizeInBytes { get; set; }

        /// <summary>Gets or sets when the file was created.</summary>
        [JsonProperty("createdAt", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? CreatedAt { get; set; }

        /// <summary>Gets or sets the purpose for which the file was uploaded.</summary>
        /// <remarks>
        /// Common values include "assistants", "fine-tune", "batch", or "vision",
        /// but the specific values supported depend on the provider.
        /// </remarks>
        [JsonIgnore]
        public string? Purpose
        {
            get => _purposeCore;
            set => _purposeCore = value;
        }

        [JsonProperty("purpose", NullValueHandling = NullValueHandling.Ignore)]
        internal string? PurposeCore
        {
            get => _purposeCore;
            set => _purposeCore = value;
        }

        /// <summary>Gets or sets the scope (e.g. container ID) in which the file resides.</summary>
        /// <remarks>
        /// When set, file operations such as downloading will target this scope.
        /// For example, files created by a code interpreter tool are stored in a container,
        /// and the container ID is the scope needed to access them.
        /// </remarks>
        [JsonIgnore]
        public string? Scope
        {
            get => _scopeCore;
            set => _scopeCore = value;
        }

        [JsonProperty("scope", NullValueHandling = NullValueHandling.Ignore)]
        internal string? ScopeCore
        {
            get => _scopeCore;
            set => _scopeCore = value;
        }

        /// <summary>
        /// Determines whether the <see cref="MediaType"/>'s top-level type matches the specified <paramref name="topLevelType"/>.
        /// </summary>
        /// <param name="topLevelType">The type to compare against <see cref="MediaType"/>.</param>
        /// <returns><see langword="true"/> if the type portion of <see cref="MediaType"/> matches the specified value; otherwise, false.</returns>
        /// <remarks>
        /// <para>
        /// A media type is primarily composed of two parts, a "type" and a "subtype", separated by a slash ("/").
        /// The type portion is also referred to as the "top-level type"; for example,
        /// "image/png" has a top-level type of "image". <see cref="HasTopLevelMediaType"/> compares
        /// the specified <paramref name="topLevelType"/> against the type portion of <see cref="MediaType"/>.
        /// </para>
        /// <para>
        /// If <see cref="MediaType"/> is <see langword="null"/>, this method returns <see langword="false"/>.
        /// </para>
        /// </remarks>
        public bool HasTopLevelMediaType(string topLevelType) => MediaType is not null && DataUriParser.HasTopLevelMediaType(MediaType, topLevelType);

        /// <summary>Gets a string representing this instance to display in the debugger.</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get
            {
                string display = $"FileId = {FileId}";

                if (MediaType is string mediaType)
                {
                    display += $", MediaType = {mediaType}";
                }

                if (Name is string name)
                {
                    display += $", Name = \"{name}\"";
                }

                if (SizeInBytes is not null)
                {
                    display += $", Size = {SizeInBytes} bytes";
                }

                return display;
            }
        }
    }
}