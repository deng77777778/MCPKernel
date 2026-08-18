// Middleware/ValidationMiddleware.cs
#nullable enable
using System;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// 验证中间件
    /// </summary>
    public class ValidationMiddleware : ISchemaMiddleware
    {
        public int Priority => 1000;

        public SchemaContext Process(SchemaContext context, Func<SchemaContext, SchemaContext> next)
        {
            if (context.CurrentType == null && context.CurrentMethod == null)
                throw new InvalidOperationException("No type or method specified for schema generation");

            if (context.CurrentMethod != null && context.CurrentMethod.ContainsGenericParameters)
                throw new NotSupportedException("Open generic methods are not supported");

            return next(context);
        }
    }
}