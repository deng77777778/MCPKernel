// Middleware/CircularReferenceMiddleware.cs
#nullable enable
using System;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// 循环引用检测中间件
    /// </summary>
    public class CircularReferenceMiddleware : ISchemaMiddleware
    {
        public int Priority => 900;

        public SchemaContext Process(SchemaContext context, Func<SchemaContext, SchemaContext> next)
        {
            if (!context.Options.DetectCircularReferences)
                return next(context);

            var type = context.CurrentType;
            if (type != null && context.IsTypeInStack(type))
            {
                return context.WithResult(new Newtonsoft.Json.Linq.JObject
                {
                    ["$ref"] = $"#/$defs/{type.Name}",
                    ["description"] = "Circular reference detected"
                });
            }

            return next(context);
        }
    }
}