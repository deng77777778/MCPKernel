// Middleware/SchemaMiddlewarePipeline.cs
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// Schema 中间件管道
    /// </summary>
    public class SchemaMiddlewarePipeline
    {
        private readonly List<ISchemaMiddleware> _middlewares = new();
        private bool _built;

        public SchemaMiddlewarePipeline Use<T>() where T : ISchemaMiddleware, new()
        {
            if (_built)
                throw new InvalidOperationException("Pipeline already built");

            _middlewares.Add(new T());
            return this;
        }

        public SchemaMiddlewarePipeline Use(ISchemaMiddleware middleware)
        {
            if (_built)
                throw new InvalidOperationException("Pipeline already built");

            _middlewares.Add(middleware);
            return this;
        }

        public SchemaMiddlewarePipeline Insert<T>(int index) where T : ISchemaMiddleware, new()
        {
            if (_built)
                throw new InvalidOperationException("Pipeline already built");

            _middlewares.Insert(index, new T());
            return this;
        }

        public SchemaMiddlewarePipeline Remove<T>() where T : ISchemaMiddleware
        {
            if (_built)
                throw new InvalidOperationException("Pipeline already built");

            _middlewares.RemoveAll(m => m is T);
            return this;
        }

        public SchemaContext Execute(SchemaContext context, Func<SchemaContext, SchemaContext> final)
        {
            _built = true;

            // 按优先级排序（数字越小越先执行）
            var ordered = _middlewares.OrderBy(m => m.Priority).ToList();

            Func<SchemaContext, SchemaContext> pipeline = final;

            for (int i = ordered.Count - 1; i >= 0; i--)
            {
                var middleware = ordered[i];
                var next = pipeline;
                pipeline = ctx => middleware.Process(ctx, next);
            }

            return pipeline(context);
        }

        public void Reset()
        {
            _middlewares.Clear();
            _built = false;
        }

        public IReadOnlyList<ISchemaMiddleware> GetMiddlewares()
        {
            return _middlewares.AsReadOnly();
        }
    }
}