// SchemaPipeline.cs
#nullable enable
using MCP.AI;
using MCP.Kernel.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// Schema 管道 - 统一入口
    /// </summary>
    public static class SchemaPipeline
    {
        private static readonly SchemaMiddlewarePipeline _pipeline;
        private static readonly TypeSchemaGenerator _typeGenerator;
        private static readonly FunctionSchemaGenerator _functionGenerator;
        private static readonly ReturnSchemaGenerator _returnGenerator;

        static SchemaPipeline()
        {
            _pipeline = new SchemaMiddlewarePipeline()
                .Use<ValidationMiddleware>()
                .Use<CircularReferenceMiddleware>()
                .Use<CachingMiddleware>();

            _typeGenerator = new TypeSchemaGenerator();
            _functionGenerator = new FunctionSchemaGenerator();
            _returnGenerator = new ReturnSchemaGenerator();
        }

        /// <summary>
        /// 处理 Schema 生成请求
        /// </summary>
        public static SchemaContext Process(SchemaContext context)
        {
            return _pipeline.Execute(context, ctx =>
            {
                if (ctx.CurrentType != null && !ctx.IsReturnType)
                {
                    var result = _typeGenerator.Generate(ctx.CurrentType, ctx);
                    ctx.Result = result;
                }
                else if (ctx.CurrentMethod != null && !ctx.IsReturnType)
                {
                    var result = _functionGenerator.Generate(ctx.CurrentMethod, ctx);
                    ctx.Result = result;
                }
                else if (ctx.CurrentMethod != null && ctx.IsReturnType)
                {
                    var result = _returnGenerator.Generate(ctx.CurrentMethod, ctx);
                    ctx.Result = result;
                }
                return ctx;
            });
        }

        /// <summary>
        /// 获取类型 Schema
        /// </summary>
        public static JObject GetTypeSchema(Type type, AIJsonSchemaCreateOptions? options = null)
        {
            var context = new SchemaContext(type, options);
            var result = Process(context);
            return result.Result ?? new JObject();
        }

        /// <summary>
        /// 获取函数 Schema
        /// </summary>
        public static JObject GetFunctionSchema(
            MethodInfo method,
            string? name = null,
            string? description = null,
            JsonSerializerSettings? settings = null,
            AIJsonSchemaCreateOptions? options = null,
            Func<ParameterInfo, bool>? isSpecialParameter = null,
            Func<ParameterInfo, bool>? isParameterRequired = null)
        {
            var context = new SchemaContext(method, options)
                .WithName(name)
                .WithDescription(description)
                .WithSettings(settings)
                .WithParameterFilter(isSpecialParameter)
                .WithRequiredFilter(isParameterRequired);

            var result = Process(context);
            return result.Result ?? new JObject();
        }

        /// <summary>
        /// 获取返回类型 Schema
        /// </summary>
        public static JObject? GetReturnSchema(
            MethodInfo method,
            JsonSerializerSettings? settings = null,
            AIJsonSchemaCreateOptions? options = null,
            bool excludeResultSchema = false)
        {
            if (excludeResultSchema) return null;

            var context = new SchemaContext(method, options)
                .WithSettings(settings)
                .ForReturnType();

            var result = Process(context);
            return result.Result;
        }

        /// <summary>
        /// 重置管道
        /// </summary>
        public static void ResetPipeline()
        {
            _pipeline.Reset();
            _pipeline.Use<ValidationMiddleware>()
                     .Use<CircularReferenceMiddleware>()
                     .Use<CachingMiddleware>();
        }

        /// <summary>
        /// 获取管道中间件
        /// </summary>
        public static IReadOnlyList<ISchemaMiddleware> GetMiddlewareList()
        {
            return _pipeline.GetMiddlewares();
        }
    }
}