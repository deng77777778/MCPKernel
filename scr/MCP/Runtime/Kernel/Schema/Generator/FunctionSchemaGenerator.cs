// Generators/FunctionSchemaGenerator.cs
#nullable enable
using MCP.AI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// 函数 Schema 生成器
    /// </summary>
    public class FunctionSchemaGenerator : SchemaGeneratorBase<MethodInfo, JObject>
    {
        private readonly ParameterBinderChain _binderChain;
        private readonly TypeSchemaGenerator _typeGenerator;

        public FunctionSchemaGenerator(ParameterBinderChain? binderChain = null)
        {
            _binderChain = binderChain ?? new ParameterBinderChain();
            _typeGenerator = new TypeSchemaGenerator();
        }

        protected override string GeneratorName => "FunctionSchema";

        protected override JObject GenerateCore(MethodInfo method, SchemaContext context)
        {
            var builder = new SchemaBuilder()
                .Object()
                .Title(context.CustomName ?? NameHelper.GetFunctionName(method))
                .Description(context.CustomDescription ?? NameHelper.GetFunctionDescription(method));

            var options = context.Options;

            foreach (var param in method.GetParameters())
            {
                // 特殊参数过滤
                if (context.ParameterFilter?.Invoke(param) ?? TypeHelper.IsSpecialParameter(param))
                    continue;

                // 用户过滤
                if (options.IncludeParameter != null && !options.IncludeParameter(param))
                    continue;

                var paramName = NameHelper.GetParameterName(param);
                var paramSchema = GetTypeSchema(param.ParameterType, context);

                // 描述
                var desc = options.ParameterDescriptionProvider?.Invoke(param) ??
                           param.GetCustomAttribute<DescriptionAttribute>(true)?.Description;
                if (!string.IsNullOrEmpty(desc) && paramSchema is not null)
                    paramSchema["description"] = desc;

                // 默认值
                if (DefaultValueHelper.TryGetValue(param, out var defaultValue) && defaultValue != null)
                {
                    try
                    {
                        if (paramSchema is not null)
                            paramSchema["default"] = JToken.FromObject(defaultValue, JsonSerializer.Create(context.Settings));
                    }
                    catch { }
                }

                // 必需
                var isRequired = context.RequiredFilter?.Invoke(param) ?? TypeHelper.IsParameterRequired(param);
                if (paramSchema is not null)
                    builder.Property(paramName, paramSchema, isRequired);
            }

            return builder.Build();
        }

        /// <summary>
        /// 获取类型 Schema
        /// </summary>
        private JObject? GetTypeSchema(Type type, SchemaContext context)
        {
            return _typeGenerator.Generate(type, context);
        }

        /// <summary>
        /// 获取参数绑定函数 - 返回 (AIFunctionArguments, CancellationToken) => object?
        /// </summary>
        public Func<AIFunctionArguments, CancellationToken, object?> GetParameterBinder(ParameterInfo parameter)
        {
            // 创建闭包，捕获 parameter
            return (args, ct) =>
            {
                var binder = _binderChain.GetBinder(parameter);
                if (binder == null)
                {
                    // 如果没有找到绑定器，使用默认绑定器
                    var defaultBinder = new DefaultParameterBinder();
                    return defaultBinder.Bind(parameter, args, ct);
                }
                return binder.Bind(parameter, args, ct);
            };
        }

        /// <summary>
        /// 获取所有参数绑定器
        /// </summary>
        public Func<AIFunctionArguments, CancellationToken, object?>[] GetParameterBinders(MethodInfo method)
        {
            var parameters = method.GetParameters();
            var binders = new Func<AIFunctionArguments, CancellationToken, object?>[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                binders[i] = GetParameterBinder(parameters[i]);
            }

            return binders;
        }

        /// <summary>
        /// 获取参数绑定器（带参数信息）
        /// </summary>
        public Func<ParameterInfo, AIFunctionArguments, CancellationToken, object?> GetParameterBinderWithInfo()
        {
            return (param, args, ct) =>
            {
                var binder = _binderChain.GetBinder(param);
                if (binder == null)
                {
                    var defaultBinder = new DefaultParameterBinder();
                    return defaultBinder.Bind(param, args, ct);
                }
                return binder.Bind(param, args, ct);
            };
        }
    }
}