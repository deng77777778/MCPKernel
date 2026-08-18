#nullable enable
using MCP.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// Schema 生成器注册表
    /// </summary>
    public class McpSchemaGeneratorRegistry
    {
        private readonly ConcurrentDictionary<Type, IMcpSchemaGenerator> _generators = new();

        /// <summary>
        /// 注册生成器
        /// </summary>
        public McpSchemaGeneratorRegistry Register<TResult>(IMcpSchemaGenerator<TResult> generator)
            where TResult : IBaseMetadata
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));

            _generators[typeof(TResult)] = generator;
            return this;
        }

        /// <summary>
        /// 获取生成器
        /// </summary>
        public IMcpSchemaGenerator<T>? GetGenerator<T>() where T : IBaseMetadata
        {
            return _generators.TryGetValue(typeof(T), out var generator)
                ? generator as IMcpSchemaGenerator<T>
                : null;
        }

        /// <summary>
        /// 获取生成器（非泛型）
        /// </summary>
        public object? GetGenerator(Type resultType)
        {
            if (resultType == null) throw new ArgumentNullException(nameof(resultType));

            _generators.TryGetValue(resultType, out var generator);
            return generator;
        }

        /// <summary>
        /// 获取所有生成器
        /// </summary>
        public IEnumerable<object> GetAllGenerators()
        {
            return _generators.Values;
        }

        /// <summary>
        /// 获取所有生成器（带类型信息）
        /// </summary>
        public IEnumerable<KeyValuePair<Type, IMcpSchemaGenerator>> GetAllGeneratorsWithType()
        {
            return _generators;
        }

        /// <summary>
        /// 检查是否已注册
        /// </summary>
        public bool HasGenerator<T>() where T : IBaseMetadata
        {
            return _generators.ContainsKey(typeof(T));
        }

        /// <summary>
        /// 检查是否已注册
        /// </summary>
        public bool HasGenerator(Type resultType)
        {
            if (resultType == null) throw new ArgumentNullException(nameof(resultType));
            return _generators.ContainsKey(resultType);
        }

        /// <summary>
        /// 移除生成器
        /// </summary>
        public bool RemoveGenerator<T>() where T : IBaseMetadata
        {
            return _generators.TryRemove(typeof(T), out _);
        }

        /// <summary>
        /// 移除生成器
        /// </summary>
        public bool RemoveGenerator(Type resultType)
        {
            if (resultType == null) throw new ArgumentNullException(nameof(resultType));
            return _generators.TryRemove(resultType, out _);
        }

        /// <summary>
        /// 清空所有生成器
        /// </summary>
        public void Clear()
        {
            _generators.Clear();
        }

        /// <summary>
        /// 获取注册数量
        /// </summary>
        public int Count => _generators.Count;

        /// <summary>
        /// 默认注册表
        /// </summary>
        public static McpSchemaGeneratorRegistry Default { get; } = new();

        static McpSchemaGeneratorRegistry()
        {
            Default
                .Register(new ToolSchemaGenerator())
                .Register(new ResourceSchemaGenerator())
                .Register(new ResourceTemplateSchemaGenerator())
                .Register(new PromptSchemaGenerator());
        }
    }
}
