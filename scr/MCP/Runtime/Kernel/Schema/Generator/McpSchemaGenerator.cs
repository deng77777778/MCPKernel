#nullable enable
using MCP.AI;
using MCP.Kernel.Cache;
using MCP.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MCP.Kernel.Schema
{
    public static class McpSchemaGenerator
    {
        private static McpSchemaGeneratorRegistry? _registry;

        public static McpSchemaGeneratorRegistry Registry
        {
            get
            {
                _registry ??= McpSchemaGeneratorRegistry.Default;
                return _registry;
            }
            set => _registry = value ?? throw new ArgumentNullException(nameof(value));
        }

        #region 统一泛型接口

        public static IEnumerable<T> Generate<T>(Type type, AIJsonSchemaCreateOptions? options = null)
            where T : IBaseMetadata
        {
            var generator = Registry.GetGenerator<T>();
            return generator?.Generate(type, options) ?? Enumerable.Empty<T>();
        }

        public static IEnumerable<MethodInfo> GetMethods<T>(Type type)
                              where T : IBaseMetadata
        {
            var generator = Registry.GetGenerator<T>();
            return generator?.GetMethods(type) ?? Enumerable.Empty<MethodInfo>();
        }

        public static T? Generate<T>(MethodInfo method, AIJsonSchemaCreateOptions? options = null)
            where T : IBaseMetadata
        {
            var generator = Registry.GetGenerator<T>();
            if (generator is null) return default;
            return generator.Generate(method, options);
        }

        #endregion

        #region 缓存
        private static UnifiedCache Cache => UnifiedCache.Instance;
        public static void ClearCache() => Cache.ClearAll();

        #endregion
    }
}
