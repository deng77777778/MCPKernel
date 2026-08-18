// Middleware/CachingMiddleware.cs
#nullable enable
using MCP.Kernel.Cache;
using System;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// 缓存中间件
    /// </summary>
    public class CachingMiddleware : ISchemaMiddleware
    {
        private static readonly UnifiedCache _cache = UnifiedCache.Instance;

        public int Priority => 800;

        public SchemaContext Process(SchemaContext context, Func<SchemaContext, SchemaContext> next)
        {
            if (!context.CacheEnabled)
                return next(context);

            // 尝试从缓存获取
            if (TryGetFromCache(context, out var cached))
            {
                return context.WithResult(cached);
            }

            // 执行后续管道
            var result = next(context);

            // 存入缓存
            if (result.Result != null)
            {
                SetToCache(context, result.Result);
            }

            return result;
        }

        private static bool TryGetFromCache(SchemaContext context, out Newtonsoft.Json.Linq.JObject result)
        {
            result = null!;

            if (context.CurrentType != null && _cache.TryGetSchema(context.CurrentType, out var schema))
            {
                result = schema;
                return true;
            }

            if (context.CurrentMethod != null && _cache.TryGetFunctionSchema(context.CurrentMethod, out var funcSchema))
            {
                result = funcSchema;
                return true;
            }

            return false;
        }

        private static void SetToCache(SchemaContext context, Newtonsoft.Json.Linq.JObject result)
        {
            if (context.CurrentType != null)
                _cache.AddSchema(context.CurrentType, result);
            else if (context.CurrentMethod != null)
                _cache.AddFunctionSchema(context.CurrentMethod, result);
        }
    }
}