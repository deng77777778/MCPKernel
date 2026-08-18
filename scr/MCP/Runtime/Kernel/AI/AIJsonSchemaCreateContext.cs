#nullable enable
using System;
using System.Linq;
using System.Reflection;
#pragma warning disable CA1815 // Override equals and operator equals on value types

namespace MCP.AI
{
    /// <summary>
    /// Defines the context in which a JSON schema within a type graph is being generated.
    /// </summary>
    /// <remarks>
    /// This struct is being passed to the user-provided <see cref="AIJsonSchemaCreateOptions.TransformSchemaNode"/>
    /// callback by the <see cref="AIJsonUtilities.CreateJsonSchema"/> method and cannot be instantiated directly.
    /// </remarks>
    public readonly struct AIJsonSchemaCreateContext
    {
        private readonly object _context;

        internal AIJsonSchemaCreateContext(object context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the path to the schema document currently being generated.
        /// </summary>
        public ReadOnlySpan<string> Path
        {
            get
            {
                if (_context is TypeSchemaContext typeCtx)
                {
                    return typeCtx.Path;
                }
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Gets the type being processed.
        /// </summary>
        public Type TypeInfo
        {
            get
            {
                if (_context is TypeSchemaContext typeCtx)
                {
                    return typeCtx.Type;
                }
                return typeof(object);
            }
        }

        /// <summary>
        /// Gets the type info for the polymorphic base type if generated as a derived type.
        /// </summary>
        public Type? BaseTypeInfo
        {
            get
            {
                if (_context is TypeSchemaContext typeCtx)
                {
                    return typeCtx.BaseType;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the <see cref="JsonPropertyInfo"/> if the schema is being generated for a property.
        /// </summary>
        public PropertyInfo? PropertyInfo
        {
            get
            {
                if (_context is PropertySchemaContext propCtx)
                {
                    return propCtx.Property;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the declaring type of the property or parameter being processed.
        /// </summary>
        public Type? DeclaringType
        {
            get
            {
                if (_context is PropertySchemaContext propCtx)
                {
                    return propCtx.Property?.DeclaringType;
                }
                if (_context is TypeSchemaContext typeCtx)
                {
                    return typeCtx.Type;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the <see cref="ICustomAttributeProvider"/> corresponding to the property or field being processed.
        /// </summary>
        public ICustomAttributeProvider? PropertyAttributeProvider
        {
            get
            {
                if (_context is PropertySchemaContext propCtx)
                {
                    return propCtx.Property;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the <see cref="System.Reflection.ICustomAttributeProvider"/> of the
        /// constructor parameter associated with the accompanying <see cref="PropertyInfo"/>.
        /// </summary>
        public ICustomAttributeProvider? ParameterAttributeProvider
        {
            get
            {
                if (_context is PropertySchemaContext propCtx)
                {
                    return propCtx.Parameter;
                }
                return null;
            }
        }

        /// <summary>
        /// Retrieves a custom attribute of a specified type that is applied to the specified schema node context.
        /// </summary>
        /// <typeparam name="TAttribute">The type of attribute to search for.</typeparam>
        /// <param name="inherit">If <see langword="true"/>, specifies to also search the ancestors of the context members for custom attributes.</param>
        /// <returns>The first occurrence of <typeparamref name="TAttribute"/> if found, or <see langword="null"/> otherwise.</returns>
        /// <remarks>
        /// This helper method resolves attributes from context locations in the following order:
        /// <list type="number">
        /// <item>Attributes specified on the property of the context, if specified.</item>
        /// <item>Attributes specified on the constructor parameter of the context, if specified.</item>
        /// <item>Attributes specified on the type of the context.</item>
        /// </list>
        /// </remarks>
        public TAttribute? GetCustomAttribute<TAttribute>(bool inherit = false)
            where TAttribute : Attribute
        {
            return GetCustomAttr(PropertyAttributeProvider) ??
                GetCustomAttr(ParameterAttributeProvider) ??
                GetCustomAttr(TypeInfo);

            TAttribute? GetCustomAttr(ICustomAttributeProvider? provider) =>
                (TAttribute?)provider?.GetCustomAttributes(typeof(TAttribute), inherit).FirstOrDefault();
        }

        /// <summary>
        /// Creates a new context for a type.
        /// </summary>
        internal static AIJsonSchemaCreateContext CreateTypeContext(Type type, Type? baseType = null, string[]? path = null)
        {
            return new AIJsonSchemaCreateContext(new TypeSchemaContext(type, baseType, path ?? Array.Empty<string>()));
        }

        /// <summary>
        /// Creates a new context for a property.
        /// </summary>
        internal static AIJsonSchemaCreateContext CreatePropertyContext(PropertyInfo property, ParameterInfo? parameter = null, string[]? path = null)
        {
            return new AIJsonSchemaCreateContext(new PropertySchemaContext(property, parameter, path ?? Array.Empty<string>()));
        }

        /// <summary>
        /// Internal context for type schema generation.
        /// </summary>
        private sealed class TypeSchemaContext
        {
            public Type Type { get; }
            public Type? BaseType { get; }
            public string[] Path { get; }

            public TypeSchemaContext(Type type, Type? baseType, string[] path)
            {
                Type = type;
                BaseType = baseType;
                Path = path;
            }
        }

        /// <summary>
        /// Internal context for property schema generation.
        /// </summary>
        private sealed class PropertySchemaContext
        {
            public PropertyInfo Property { get; }
            public ParameterInfo? Parameter { get; }
            public string[] Path { get; }

            public PropertySchemaContext(PropertyInfo property, ParameterInfo? parameter, string[] path)
            {
                Property = property;
                Parameter = parameter;
                Path = path;
            }
        }
    }
}
