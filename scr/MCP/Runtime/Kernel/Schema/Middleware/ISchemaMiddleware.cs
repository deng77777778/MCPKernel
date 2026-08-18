// Middleware/ISchemaMiddleware.cs
#nullable enable
using System;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// Schema 中间件接口
    /// </summary>
    public interface ISchemaMiddleware
    {
        /// <summary>
        /// 优先级
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 处理上下文
        /// </summary>
        SchemaContext Process(SchemaContext context, Func<SchemaContext, SchemaContext> next);
    }
}