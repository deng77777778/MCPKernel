#nullable enable
using MCP.AI;
using MCP.Protocol;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MCP.Kernel.Schema
{

    public interface IMcpSchemaGenerator
    {
        /// <summary>
        /// 生成器名称
        /// </summary>
        string Name { get; }
    }

    /// <summary>
    /// MCP Schema 生成器接口
    /// </summary>
    public interface IMcpSchemaGenerator<out TResult> : IMcpSchemaGenerator
        where TResult : IBaseMetadata
    {
        /// <summary>
        /// 从类型生成 Schema 项
        /// </summary>
        IEnumerable<TResult> Generate(Type type, AIJsonSchemaCreateOptions? options = null);

        /// <summary>
        /// 从方法生成 Schema 项
        /// </summary>
        TResult? Generate(MethodInfo method, AIJsonSchemaCreateOptions? options = null);

        IEnumerable<MethodInfo> GetMethods(Type type);

    }
}
