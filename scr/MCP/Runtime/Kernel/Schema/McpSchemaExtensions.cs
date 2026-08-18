#nullable enable
using MCP.AI;
using MCP.Protocol;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MCP.Kernel.Schema
{
    public static class McpSchemaExtensions
    {
        public static IEnumerable<T> GenerateSchema<T>(this Type type, AIJsonSchemaCreateOptions? options = null)
            where T : IBaseMetadata
            => McpSchemaGenerator.Generate<T>(type, options);

        public static IEnumerable<MethodInfo> GetMethods<T>(this Type type)
            where T : IBaseMetadata
            => McpSchemaGenerator.GetMethods<T>(type);


        public static T? GenerateSchema<T>(this MethodInfo method, AIJsonSchemaCreateOptions? options = null)
            where T : IBaseMetadata
            => McpSchemaGenerator.Generate<T>(method, options);
    }
}
