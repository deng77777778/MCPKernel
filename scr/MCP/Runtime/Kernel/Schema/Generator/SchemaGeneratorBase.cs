// Generators/SchemaGeneratorBase.cs
#nullable enable
using MCP.Kernel.Cache;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// Schema 生成器基类 - 模板方法模式
    /// </summary>
    public abstract class SchemaGeneratorBase<TInput, TOutput>
        where TOutput : class
    {
        protected abstract string GeneratorName { get; }
        protected virtual bool EnableCaching => true;
        protected virtual int WarmupCount => 3;

        protected abstract TOutput? GenerateCore(TInput input, SchemaContext context);

        /// <summary>
        /// 生成 Schema（模板方法）
        /// </summary>
        public TOutput? Generate(TInput input, SchemaContext context)
        {
            // 1. 验证输入
            ValidateInput(input);

            // 2. 进入上下文
            context = EnterContext(input, context);

            try
            {
                // 3. 检查缓存
                if (EnableCaching && context.CacheEnabled && TryGetCached(input, out var cached))
                    return cached;

                // 4. 生成
                var result = GenerateCore(input, context);

                // 5. 缓存
                if (EnableCaching && context.CacheEnabled && result != null)
                    SetCached(input, result);

                // 6. 后处理
                result = PostProcess(result, context);

                return result;
            }
            finally
            {
                ExitContext(input, context);
            }
        }

        protected virtual void ValidateInput(TInput input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
        }

        protected virtual SchemaContext EnterContext(TInput input, SchemaContext context)
        {
            if (input is Type type)
                context.PushType(type);
            return context;
        }

        protected virtual void ExitContext(TInput input, SchemaContext context)
        {
            if (input is Type type)
                context.PopType(type);
        }

        protected virtual TOutput? PostProcess(TOutput? result, SchemaContext context)
        {
            return result;
        }

        #region 缓存

        private static readonly UnifiedCache _cache = UnifiedCache.Instance;

        protected virtual bool TryGetCached(TInput input, out TOutput result)
        {
            result = null!;

            if (input is Type type && _cache.TryGetSchema(type, out var schema))
            {
                if (schema is TOutput output)
                {
                    result = output;
                    return true;
                }
            }

            if (input is MethodInfo method && _cache.TryGetFunctionSchema(method, out var funcSchema))
            {
                if (funcSchema is TOutput output)
                {
                    result = output;
                    return true;
                }
            }

            return false;
        }

        protected virtual void SetCached(TInput input, TOutput result)
        {
            if (result is JObject schema)
            {
                if (input is Type type)
                    _cache.AddSchema(type, schema);
                else if (input is MethodInfo method)
                    _cache.AddFunctionSchema(method, schema);
            }
        }

        #endregion
    }
}