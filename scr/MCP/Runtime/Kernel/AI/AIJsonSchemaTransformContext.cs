#nullable enable
using System;

namespace MCP.AI
{
    /// <summary>
    /// Defines the context in which a JSON schema transformation is being performed.
    /// </summary>
    public readonly struct AIJsonSchemaTransformContext
    {
        private readonly string[] _path;

        internal AIJsonSchemaTransformContext(string[] path)
        {
            _path = path ?? Array.Empty<string>();
        }

        /// <summary>
        /// Gets the path to the schema document currently being generated.
        /// </summary>
        public string[] Path => _path;

        /// <summary>
        /// Gets the containing property name if the current schema is a property of an object.
        /// </summary>
        public string? PropertyName
        {
            get
            {
                if (_path.Length >= 2 &&
                    _path[_path.Length - 2] == "properties" &&
                    _path.Length > 0)
                {
                    return _path[_path.Length - 1];
                }
                return null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current schema is a collection element.
        /// </summary>
        public bool IsCollectionElementSchema
        {
            get
            {
                if (_path.Length > 0 && _path[_path.Length - 1] == "items")
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current schema is a dictionary value.
        /// </summary>
        public bool IsDictionaryValueSchema
        {
            get
            {
                if (_path.Length > 0 && _path[_path.Length - 1] == "additionalProperties")
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Creates a new context with the specified path.
        /// </summary>
        internal static AIJsonSchemaTransformContext Create(string[] path)
        {
            return new AIJsonSchemaTransformContext(path);
        }

        /// <summary>
        /// Creates a new context with the specified path and an additional segment.
        /// </summary>
        internal AIJsonSchemaTransformContext Append(string segment)
        {
            var newPath = new string[_path.Length + 1];
            Array.Copy(_path, 0, newPath, 0, _path.Length);
            newPath[_path.Length] = segment;
            return new AIJsonSchemaTransformContext(newPath);
        }

        /// <summary>
        /// Creates a new context with the specified path and additional segments.
        /// </summary>
        internal AIJsonSchemaTransformContext Append(params string[] segments)
        {
            var newPath = new string[_path.Length + segments.Length];
            Array.Copy(_path, 0, newPath, 0, _path.Length);
            Array.Copy(segments, 0, newPath, _path.Length, segments.Length);
            return new AIJsonSchemaTransformContext(newPath);
        }

        /// <summary>
        /// Returns a string representation of the path.
        /// </summary>
        public override string ToString()
        {
            return string.Join("/", _path);
        }
    }
}