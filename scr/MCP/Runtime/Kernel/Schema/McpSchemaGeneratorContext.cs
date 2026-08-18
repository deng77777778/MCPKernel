#nullable enable
using MCP.AI;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// Schema 生成上下文
    /// </summary>
    public class McpSchemaGeneratorContext
    {
        /// <summary>
        /// 当前正在处理的类型
        /// </summary>
        public Type? CurrentType { get; set; }

        /// <summary>
        /// 当前正在处理的方法
        /// </summary>
        public MethodInfo? CurrentMethod { get; set; }

        /// <summary>
        /// 生成选项
        /// </summary>
        public AIJsonSchemaCreateOptions Options { get; set; }

        /// <summary>
        /// 已处理的类型（用于循环引用检测）
        /// </summary>
        public HashSet<Type> VisitedTypes { get; } = new();

        /// <summary>
        /// 额外数据
        /// </summary>
        public Dictionary<string, object> Data { get; } = new();

        public McpSchemaGeneratorContext(AIJsonSchemaCreateOptions? options = null)
        {
            Options = options ?? AIJsonSchemaCreateOptions.Default;
        }

        public T? GetData<T>(string key) where T : class
        {
            return Data.TryGetValue(key, out var value) ? value as T : null;
        }

        public void SetData(string key, object value)
        {
            Data[key] = value;
        }
    }
}
