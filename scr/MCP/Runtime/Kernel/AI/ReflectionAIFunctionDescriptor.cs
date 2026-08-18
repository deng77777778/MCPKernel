#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MCP.Kernel.Schema;

namespace MCP.AI
{
    public sealed class ReflectionAIFunctionDescriptor
    {
        #region 缓存

        private static readonly ConcurrentDictionary<string, ReflectionAIFunctionDescriptor> _descriptorCache = new();
        private const int CacheSoftLimit = 512;
        private static readonly object?[] _emptyArgs = Array.Empty<object?>();

        #endregion

        #region 属性

        public string Name { get; }
        public string Description { get; }
        public MethodInfo Method { get; }
        public JsonSerializerSettings JsonSerializerSettings { get; }
        public JObject JsonSchema { get; }
        public JObject? ReturnJsonSchema { get; }
        public HashSet<string> ExpectedArgumentNames { get; }
        public bool HasCustomParameterBinding { get; private set; }
        public bool IsAsyncMethod { get; }
        public Type ReturnType { get; }
        public Type? UnwrappedReturnType { get; }
        public bool IsAIContentRelated { get; }

        public Func<object?, object?[], object?> SyncInvoker { get; }
        public Func<object?, object?[], ValueTask<object?>>? AsyncInvoker { get; }
        public Func<AIFunctionArguments, CancellationToken, object?>[] ParameterMarshallers { get; }

        #endregion

        #region 构造函数

        private ReflectionAIFunctionDescriptor(MethodInfo method, AIFunctionFactoryOptions options)
        {
            Method = method ?? throw new ArgumentNullException(nameof(method));
            ReturnType = method.ReturnType;
            UnwrappedReturnType = SchemaHelpers.GetUnwrappedReturnType(ReturnType);
            IsAsyncMethod = SchemaHelpers.IsAsyncMethod(method);
            IsAIContentRelated = UnwrappedReturnType != null && SchemaHelpers.IsAIContentRelatedType(UnwrappedReturnType);

            JsonSerializerSettings = options.SerializerOptions ?? AIJsonUtilities.DefaultSettings;
            Name = GetFunctionName(method, options);
            Description = GetFunctionDescription(method, options);

            var parameters = method.GetParameters();
            (ParameterMarshallers, ExpectedArgumentNames, HasCustomParameterBinding) =
                BuildParameterMarshallers(parameters, options);

            SyncInvoker = CreateSyncInvoker(method);

            if (IsAsyncMethod)
            {
                AsyncInvoker = CreateAsyncInvokerExpression(method);
            }

            var schemaOptions = options.JsonSchemaCreateOptions ?? AIJsonSchemaCreateOptions.Default;

            // 使用 SchemaHelpers 生成 Schema
            JsonSchema = SchemaHelpers.CreateFunctionJsonSchema(
                method,
                Name,
                Description,
                JsonSerializerSettings,
                schemaOptions,
                IsSpecialParameter,
                IsParameterRequired);

            ReturnJsonSchema = SchemaHelpers.CreateReturnJsonSchema(
                method,
                JsonSerializerSettings,
                schemaOptions,
                options.ExcludeResultSchema);
        }

        #endregion

        #region 工厂方法

        public static ReflectionAIFunctionDescriptor GetOrCreate(MethodInfo method, AIFunctionFactoryOptions options)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            string cacheKey = BuildCacheKey(method, options);

            if (ShouldCache(options))
            {
                return _descriptorCache.GetOrAdd(cacheKey, _ => new ReflectionAIFunctionDescriptor(method, options));
            }

            return new ReflectionAIFunctionDescriptor(method, options);
        }

        private static string BuildCacheKey(MethodInfo method, AIFunctionFactoryOptions options)
        {
            var key = $"{method.Module.Name}:{method.MetadataToken}";
            if (options.Name != null || options.Description != null ||
                options.ConfigureParameterBinding != null || options.MarshalResult != null)
            {
                key = $"{key}:{options.GetHashCode()}";
            }
            return key;
        }

        private static bool ShouldCache(AIFunctionFactoryOptions options)
        {
            return options.Name == null && options.Description == null &&
                   options.ConfigureParameterBinding == null && options.MarshalResult == null;
        }

        #endregion

        #region 名称和描述

        private static string GetFunctionName(MethodInfo method, AIFunctionFactoryOptions options)
        {
            return options.Name ??
                   method.GetCustomAttribute<AIFunctionNameAttribute>(true)?.Name ??
                   method.GetCustomAttribute<DisplayNameAttribute>(true)?.DisplayName ??
                   SchemaHelpers.GetFunctionName(method);
        }

        private static string GetFunctionDescription(MethodInfo method, AIFunctionFactoryOptions options)
        {
            return options.Description ??
                   method.GetCustomAttribute<DescriptionAttribute>(true)?.Description ??
                   string.Empty;
        }

        #endregion

        #region 同步调用器（表达式树）

        private static Func<object?, object?[], object?> CreateSyncInvoker(MethodInfo method)
        {
            var targetParam = Expression.Parameter(typeof(object), "target");
            var argsParam = Expression.Parameter(typeof(object[]), "args");

            var parameters = method.GetParameters();
            var argExpressions = new Expression[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var index = Expression.Constant(i);
                var paramType = parameters[i].ParameterType;
                var argAccess = Expression.ArrayIndex(argsParam, index);
                argExpressions[i] = Expression.Convert(argAccess, paramType);
            }

            var call = Expression.Call(
                method.IsStatic ? null : Expression.Convert(targetParam, method.DeclaringType!),
                method,
                argExpressions);

            if (method.ReturnType == typeof(void))
            {
                var block = Expression.Block(call, Expression.Constant(null));
                return Expression.Lambda<Func<object?, object?[], object?>>(block, targetParam, argsParam).Compile();
            }

            var convert = Expression.Convert(call, typeof(object));
            return Expression.Lambda<Func<object?, object?[], object?>>(convert, targetParam, argsParam).Compile();
        }

        #endregion

        #region 异步调用器

        private static Func<object?, object?[], ValueTask<object?>> CreateAsyncInvokerExpression(MethodInfo method)
        {
            var targetParam = Expression.Parameter(typeof(object), "target");
            var argsParam = Expression.Parameter(typeof(object[]), "args");

            var parameters = method.GetParameters();
            var argExpressions = new Expression[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var index = Expression.Constant(i);
                var paramType = parameters[i].ParameterType;
                var argAccess = Expression.ArrayIndex(argsParam, index);
                argExpressions[i] = Expression.Convert(argAccess, paramType);
            }

            var call = Expression.Call(
                method.IsStatic ? null : Expression.Convert(targetParam, method.DeclaringType!),
                method,
                argExpressions);

            var returnType = method.ReturnType;

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var taskResultType = returnType.GetGenericArguments()[0];
                var helperMethod = typeof(AsyncHelper)
                    .GetMethod(nameof(AsyncHelper.WrapTask), BindingFlags.Public | BindingFlags.Static)!
                    .MakeGenericMethod(taskResultType);

                var lambda = Expression.Lambda<Func<object?, object?[], ValueTask<object?>>>(
                    Expression.Call(helperMethod, call),
                    targetParam, argsParam);
                return lambda.Compile();
            }

            if (returnType == typeof(Task))
            {
                var helperMethod = typeof(AsyncHelper)
                    .GetMethod(nameof(AsyncHelper.WrapTaskVoid), BindingFlags.Public | BindingFlags.Static)!;

                var lambda = Expression.Lambda<Func<object?, object?[], ValueTask<object?>>>(
                    Expression.Call(helperMethod, call),
                    targetParam, argsParam);
                return lambda.Compile();
            }

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                var taskResultType = returnType.GetGenericArguments()[0];
                var helperMethod = typeof(AsyncHelper)
                    .GetMethod(nameof(AsyncHelper.WrapValueTask), BindingFlags.Public | BindingFlags.Static)!
                    .MakeGenericMethod(taskResultType);

                var lambda = Expression.Lambda<Func<object?, object?[], ValueTask<object?>>>(
                    Expression.Call(helperMethod, call),
                    targetParam, argsParam);
                return lambda.Compile();
            }

            if (returnType == typeof(ValueTask))
            {
                var helperMethod = typeof(AsyncHelper)
                    .GetMethod(nameof(AsyncHelper.WrapValueTaskVoid), BindingFlags.Public | BindingFlags.Static)!;

                var lambda = Expression.Lambda<Func<object?, object?[], ValueTask<object?>>>(
                    Expression.Call(helperMethod, call),
                    targetParam, argsParam);
                return lambda.Compile();
            }

            var convertCall = Expression.Convert(call, typeof(object));
            var convertTask = Expression.Convert(convertCall, typeof(ValueTask<object?>));
            return Expression.Lambda<Func<object?, object?[], ValueTask<object?>>>(
                convertTask, targetParam, argsParam).Compile();
        }

        public static class AsyncHelper
        {
            public static async ValueTask<object?> WrapTask<T>(Task<T> task)
            {
                var result = await task.ConfigureAwait(true);
                return result;
            }

            public static async ValueTask<object?> WrapTaskVoid(Task task)
            {
                await task.ConfigureAwait(true);
                return null;
            }

            public static async ValueTask<object?> WrapValueTask<T>(ValueTask<T> task)
            {
                var result = await task.ConfigureAwait(true);
                return result;
            }

            public static async ValueTask<object?> WrapValueTaskVoid(ValueTask task)
            {
                await task.ConfigureAwait(true);
                return null;
            }
        }

        #endregion

        #region 参数绑定

        private (Func<AIFunctionArguments, CancellationToken, object?>[] Marshallers,
                HashSet<string> ExpectedNames, bool HasCustomBinding)
            BuildParameterMarshallers(ParameterInfo[] parameters, AIFunctionFactoryOptions options)
        {
            var marshallers = new Func<AIFunctionArguments, CancellationToken, object?>[parameters.Length];
            var expectedNames = new HashSet<string>(StringComparer.Ordinal);
            bool hasCustomBinding = false;

            var bindingOptions = PrecomputeBindingOptions(parameters, options);

            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var bindingOpt = bindingOptions?[i] ?? default;

                if (bindingOpt.BindParameter != null)
                {
                    hasCustomBinding = true;
                }

                marshallers[i] = CreateParameterMarshaller(parameter, bindingOpt, options);
                CollectExpectedArgumentName(parameter, expectedNames);
            }

            return (marshallers, expectedNames, hasCustomBinding);
        }

        private AIFunctionFactoryOptions.ParameterBindingOptions[]? PrecomputeBindingOptions(
            ParameterInfo[] parameters, AIFunctionFactoryOptions options)
        {
            if (parameters.Length == 0 || options.ConfigureParameterBinding == null)
            {
                return null;
            }

            var bindingOptions = new AIFunctionFactoryOptions.ParameterBindingOptions[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                bindingOptions[i] = options.ConfigureParameterBinding(parameters[i]);
            }
            return bindingOptions;
        }

        private void CollectExpectedArgumentName(ParameterInfo parameter, HashSet<string> expectedNames)
        {
            var paramType = parameter.ParameterType;

            if (paramType == typeof(CancellationToken) ||
                paramType == typeof(AIFunctionArguments) ||
                paramType == typeof(IServiceProvider))
            {
                return;
            }

            if (!string.IsNullOrEmpty(parameter.Name))
            {
                var name = SchemaHelpers.GetParameterSchemaName(parameter);
                if (!expectedNames.Add(name))
                {
                    throw new ArgumentException(
                        $"Multiple parameters are mapped to the same name '{name}'.");
                }
            }
        }

        private Func<AIFunctionArguments, CancellationToken, object?> CreateParameterMarshaller(
            ParameterInfo parameter,
            AIFunctionFactoryOptions.ParameterBindingOptions bindingOptions,
            AIFunctionFactoryOptions options)
        {
            var paramType = parameter.ParameterType;
            var paramName = SchemaHelpers.GetParameterSchemaName(parameter);
            var hasDefault = SchemaHelpers.TryGetEffectiveDefaultValue(parameter, out var defaultValue);

            if (bindingOptions.BindParameter != null)
            {
                return (args, _) => bindingOptions.BindParameter(parameter, args);
            }

            if (paramType == typeof(CancellationToken))
            {
                return (_, cancellationToken) => cancellationToken;
            }

            if (paramType == typeof(AIFunctionArguments))
            {
                return (args, _) => args;
            }

            if (paramType == typeof(IServiceProvider))
            {
                var paramNameLocal = paramName;
                return (args, _) =>
                {
                    var services = args.Services;
                    if (services == null && !hasDefault)
                    {
                        throw new ArgumentNullException(
                            nameof(AIFunctionArguments.Services),
                            $"Services are required for parameter '{paramNameLocal}'.");
                    }
                    return services;
                };
            }

            return CreateArgumentMarshaller(parameter, paramName, paramType,
                hasDefault, defaultValue, options);
        }

        private Func<AIFunctionArguments, CancellationToken, object?> CreateArgumentMarshaller(
            ParameterInfo parameter,
            string paramName,
            Type paramType,
            bool hasDefault,
            object? defaultValue,
            AIFunctionFactoryOptions options)
        {
            var settings = JsonSerializerSettings;
            var type = paramType;
            var name = paramName;
            var defaultVal = defaultValue;
            var hasDef = hasDefault;

            return (args, _) =>
            {
                if (args.TryGetValue(name, out var value))
                {
                    return ConvertArgumentValue(value, type, settings);
                }

                if (!hasDef)
                {
                    throw new ArgumentException(
                        $"The arguments dictionary is missing a value for the required parameter '{name}'.");
                }

                return defaultVal;
            };
        }

        private static object? ConvertArgumentValue(object? value, Type targetType, JsonSerializerSettings settings)
        {
            if (value == null || targetType.IsInstanceOfType(value))
            {
                return value;
            }

            try
            {
                if (value is JToken token)
                {
                    return token.ToObject(targetType, JsonSerializer.Create(settings));
                }

                if (value is string str)
                {
                    return ConvertStringArgument(str, targetType, settings);
                }

                return ConvertViaJsonSerialization(value, targetType, settings);
            }
            catch
            {
                return value;
            }
        }

        private static object? ConvertStringArgument(string str, Type targetType, JsonSerializerSettings settings)
        {
            if (targetType == typeof(string))
            {
                return str;
            }

            if (AIJsonUtilities.IsPotentiallyJson(str))
            {
                try
                {
                    return JsonConvert.DeserializeObject(str, targetType, settings);
                }
                catch (JsonException) { }
            }

            try
            {
                return Convert.ChangeType(str, targetType);
            }
            catch
            {
                return JsonConvert.DeserializeObject($"\"{str}\"", targetType, settings);
            }
        }

        private static object? ConvertViaJsonSerialization(object value, Type targetType, JsonSerializerSettings settings)
        {
            var json = JsonConvert.SerializeObject(value, settings);
            return JsonConvert.DeserializeObject(json, targetType, settings);
        }

        #endregion

        #region 特殊参数判断（委托给 SchemaHelpers）

        private static bool IsSpecialParameter(ParameterInfo parameter)
        {
            return SchemaHelpers.IsSpecialParameter(parameter);
        }

        private static bool IsParameterRequired(ParameterInfo parameter)
        {
            return SchemaHelpers.IsParameterRequired(parameter);
        }

        #endregion

        #region 返回值处理

        public async ValueTask<object?> InvokeAsync(
            object? target,
            object?[]? args,
            CancellationToken cancellationToken = default)
        {
            args ??= _emptyArgs;

            try
            {
                if (AsyncInvoker != null)
                {
                    return await AsyncInvoker(target, args).ConfigureAwait(true);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"AsyncInvoker failed: {ex.Message}, falling back to sync");
            }

            var syncResult = SyncInvoker(target, args);
            return await HandleSyncResultAsync(syncResult, cancellationToken).ConfigureAwait(true);
        }

        private async ValueTask<object?> HandleSyncResultAsync(object? result, CancellationToken cancellationToken)
        {
            if (result == null)
            {
                return null;
            }

            var resultType = result.GetType();

            if (SchemaHelpers.IsTaskType(resultType))
            {
                if (result is Task task)
                {
                    await task.ConfigureAwait(true);

                    if (UnwrappedReturnType == null)
                    {
                        return null;
                    }

                    var resultProp = resultType.GetProperty("Result");
                    if (resultProp != null)
                    {
                        var taskResult = resultProp.GetValue(task);
                        return SerializeOrReturnResult(taskResult);
                    }

                    return null;
                }
            }

            if (SchemaHelpers.IsValueTaskType(resultType))
            {
                if (result is ValueTask valueTask)
                {
                    await valueTask.ConfigureAwait(true);

                    if (UnwrappedReturnType == null)
                    {
                        return null;
                    }

                    var resultProp = resultType.GetProperty("Result");
                    if (resultProp != null)
                    {
                        var vtResult = resultProp.GetValue(result);
                        return SerializeOrReturnResult(vtResult);
                    }

                    return null;
                }
            }

            return SerializeOrReturnResult(result);
        }

        private object? SerializeOrReturnResult(object? result)
        {
            if (result == null)
            {
                return null;
            }

            if (IsAIContentRelated)
            {
                return result;
            }

            var type = result.GetType();
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
                type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
                type == typeof(Guid) || type == typeof(Uri) || type == typeof(TimeSpan) ||
                type.IsEnum)
            {
                return result;
            }

            try
            {
                var serializer = JsonSerializer.Create(JsonSerializerSettings);
                return JToken.FromObject(result, serializer);
            }
            catch
            {
                return result.ToString();
            }
        }

        public object? InvokeSync(object? target, object?[]? args)
        {
            args ??= _emptyArgs;
            return SyncInvoker(target, args);
        }

        #endregion

        #region 工具方法

        public static void ClearCache() => _descriptorCache.Clear();
        public static (int Count, int SoftLimit) GetCacheStats() => (_descriptorCache.Count, CacheSoftLimit);
        public override string ToString() => $"{Name} ({Method.Name}) -> {ReturnType.Name}";

        #endregion
    }
}