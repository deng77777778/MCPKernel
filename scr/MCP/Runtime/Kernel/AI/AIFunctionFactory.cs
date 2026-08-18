#nullable enable

using MCP.AI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MCP.AI
{
    /// <summary>
    /// 提供创建常用 AIFunction 实现的工厂方法，使用 Newtonsoft.Json
    /// </summary>
    public static partial class AIFunctionFactory
    {
        private static readonly AIFunctionFactoryOptions _defaultOptions = new();

        public static AIFunction Create(Delegate method, AIFunctionFactoryOptions? options)
        {
            Throw.IfNull(method);
            return ReflectionAIFunction.Build(method.Method, method.Target, options ?? _defaultOptions);
        }

        public static AIFunction Create(Delegate method, string? name = null, string? description = null, JsonSerializerSettings? serializerSettings = null)
        {
            Throw.IfNull(method);
            var options = serializerSettings is null && name is null && description is null
                ? _defaultOptions
                : new AIFunctionFactoryOptions
                {
                    Name = name,
                    Description = description,
                    SerializerOptions = serializerSettings,
                };
            return ReflectionAIFunction.Build(method.Method, method.Target, options);
        }

        public static AIFunction Create(MethodInfo method, object? target, AIFunctionFactoryOptions? options)
        {
            Throw.IfNull(method);
            return ReflectionAIFunction.Build(method, target, options ?? _defaultOptions);
        }

        public static AIFunction Create(MethodInfo method, object? target, string? name = null, string? description = null, JsonSerializerSettings? serializerSettings = null)
        {
            Throw.IfNull(method);
            var options = serializerSettings is null && name is null && description is null
                ? _defaultOptions
                : new AIFunctionFactoryOptions
                {
                    Name = name,
                    Description = description,
                    SerializerOptions = serializerSettings,
                };
            return ReflectionAIFunction.Build(method, target, options);
        }

        public static AIFunction Create(MethodInfo method, Func<AIFunctionArguments, object> createInstanceFunc, AIFunctionFactoryOptions? options = null)
        {
            return ReflectionAIFunction.Build(method, createInstanceFunc, options ?? _defaultOptions);
        }

        public static AIFunctionDeclaration CreateDeclaration(string name, string? description, JToken jsonSchema, JToken? returnJsonSchema = null)
        {
            Throw.IfNullOrEmpty(name);
            return new DefaultAIFunctionDeclaration(name, description ?? string.Empty, jsonSchema, returnJsonSchema);
        }

        private sealed class DefaultAIFunctionDeclaration : AIFunctionDeclaration
        {
            public override string Name { get; }
            public override string Description { get; }
            public override JToken JsonSchema { get; }
            public override JToken? ReturnJsonSchema { get; }

            public DefaultAIFunctionDeclaration(string name, string description, JToken jsonSchema, JToken? returnJsonSchema)
            {
                Name = name;
                Description = description;
                JsonSchema = jsonSchema;
                ReturnJsonSchema = returnJsonSchema;
            }
        }

        /// <summary>
        /// 核心实现 - 使用 Newtonsoft.Json 的 ReflectionAIFunction
        /// </summary>
        private sealed class ReflectionAIFunction : AIFunction
        {
            public static ReflectionAIFunction Build(MethodInfo method, object? target, AIFunctionFactoryOptions options)
            {
                Throw.IfNull(method);

                if (method.ContainsGenericParameters)
                {
                    Throw.ArgumentException(nameof(method), "Open generic methods are not supported");
                }

                if (!method.IsStatic && target is null)
                {
                    Throw.ArgumentNullException(nameof(target), "Target must not be null for an instance method.");
                }

                var descriptor = ReflectionAIFunctionDescriptor.GetOrCreate(method, options);

                if (target is null && options.AdditionalProperties is null)
                {
                    return descriptor.CachedDefaultInstance ??= new ReflectionAIFunction(descriptor, target, options);
                }

                return new ReflectionAIFunction(descriptor, target, options);
            }

            public static ReflectionAIFunction Build(MethodInfo method, Func<AIFunctionArguments, object> createInstanceFunc, AIFunctionFactoryOptions options)
            {
                Throw.IfNull(method);
                Throw.IfNull(createInstanceFunc);

                if (method.ContainsGenericParameters)
                {
                    Throw.ArgumentException(nameof(method), "Open generic methods are not supported");
                }

                if (method.IsStatic)
                {
                    Throw.ArgumentException(nameof(method), "The method must be an instance method.");
                }

                return new ReflectionAIFunction(ReflectionAIFunctionDescriptor.GetOrCreate(method, options), createInstanceFunc, options);
            }

            private ReflectionAIFunction(ReflectionAIFunctionDescriptor descriptor, object? target, AIFunctionFactoryOptions options)
            {
                FunctionDescriptor = descriptor;
                Target = target;
                AdditionalProperties = options.AdditionalProperties ?? EmptyReadOnlyDictionary<string, object?>.Instance;
            }

            private ReflectionAIFunction(ReflectionAIFunctionDescriptor descriptor, Func<AIFunctionArguments, object> createInstanceFunc, AIFunctionFactoryOptions options)
            {
                FunctionDescriptor = descriptor;
                CreateInstanceFunc = createInstanceFunc;
                AdditionalProperties = options.AdditionalProperties ?? EmptyReadOnlyDictionary<string, object?>.Instance;
            }

            public ReflectionAIFunctionDescriptor FunctionDescriptor { get; }
            public object? Target { get; }
            public Func<AIFunctionArguments, object>? CreateInstanceFunc { get; }

            public override IReadOnlyDictionary<string, object?> AdditionalProperties { get; }
            public override string Name => FunctionDescriptor.Name;
            public override string Description => FunctionDescriptor.Description;
            public override MethodInfo UnderlyingMethod => FunctionDescriptor.Method;
            public override JToken JsonSchema => FunctionDescriptor.JsonSchema;
            public override JToken? ReturnJsonSchema => FunctionDescriptor.ReturnJsonSchema;
            public override JsonSerializerSettings JsonSerializerSettings => FunctionDescriptor.JsonSerializerSettings;

            protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
            {
                bool disposeTarget = false;
                object? target = Target;

                try
                {
                    if (CreateInstanceFunc is { } func)
                    {
                        Debug.Assert(target is null, "Expected target to be null when we have a non-null target type");
                        Debug.Assert(!FunctionDescriptor.Method.IsStatic, "Expected an instance method");

                        target = func(arguments);
                        if (target is null)
                        {
                            Throw.InvalidOperationException("Unable to create an instance of the target type.");
                        }
                        disposeTarget = true;
                    }

                    var paramMarshallers = FunctionDescriptor.ParameterMarshallers;
                    object?[] args = paramMarshallers.Length != 0 ? new object?[paramMarshallers.Length] : Array.Empty<object?>();

                    // 验证未映射的成员
                    if (FunctionDescriptor.JsonSerializerSettings.MissingMemberHandling == MissingMemberHandling.Error &&
                        arguments.Count > 0 &&
                        !FunctionDescriptor.HasCustomParameterBinding)
                    {
                        var expectedNames = FunctionDescriptor.ExpectedArgumentNames;
                        int matched = 0;
                        foreach (string name in expectedNames)
                        {
                            if (arguments.ContainsKey(name))
                            {
                                matched++;
                            }
                        }

                        if (matched != arguments.Count)
                        {
                            foreach (var kvp in arguments)
                            {
                                if (!expectedNames.Contains(kvp.Key))
                                {
                                    Throw.ArgumentException(nameof(arguments), $"The arguments dictionary contains an unexpected key '{kvp.Key}' that does not correspond to any parameter of '{Name}'.");
                                }
                            }
                            Throw.ArgumentException(nameof(arguments), $"The arguments dictionary contains keys that do not correspond to any parameter of '{Name}'.");
                        }
                    }

                    for (int i = 0; i < args.Length; i++)
                    {
                        args[i] = paramMarshallers[i](arguments, cancellationToken);
                    }

                    return await FunctionDescriptor.ReturnParameterMarshaller(
                        ReflectionInvoke(FunctionDescriptor.Method, target, args), cancellationToken).ConfigureAwait(true);
                }
                finally
                {
                    if (disposeTarget)
                    {
                        if (target is IAsyncDisposable ad)
                        {
                            await ad.DisposeAsync().ConfigureAwait(true);
                        }
                        else if (target is IDisposable d)
                        {
                            d.Dispose();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 使用 Newtonsoft.Json 的反射函数描述符
        /// </summary>
        private sealed class ReflectionAIFunctionDescriptor
        {
            private const int InnerCacheSoftLimit = 512;
            private static readonly ConditionalWeakTable<JsonSerializerSettings, ConcurrentDictionary<DescriptorKey, ReflectionAIFunctionDescriptor>> _descriptorCache = new();

            private static readonly object _boxedDefaultCancellationToken = default(CancellationToken);

            public static ReflectionAIFunctionDescriptor GetOrCreate(MethodInfo method, AIFunctionFactoryOptions options)
            {
                var serializerSettings = options.SerializerOptions ?? MCP.AI.AIJsonUtilities.DefaultSettings;
                var schemaOptions = options.JsonSchemaCreateOptions ?? AIJsonSchemaCreateOptions.Default;

                var innerCache = _descriptorCache.GetOrCreateValue(serializerSettings);

                var key = new DescriptorKey(method, options.Name, options.Description, options.ConfigureParameterBinding, options.MarshalResult, options.ExcludeResultSchema, schemaOptions);

                if (innerCache.TryGetValue(key, out var descriptor))
                {
                    return descriptor;
                }

                descriptor = new ReflectionAIFunctionDescriptor(key, serializerSettings);
                return innerCache.Count < InnerCacheSoftLimit
                    ? innerCache.GetOrAdd(key, descriptor)
                    : descriptor;
            }

            private ReflectionAIFunctionDescriptor(DescriptorKey key, JsonSerializerSettings serializerSettings)
            {
                var parameters = key.Method.GetParameters();

                // 确定每个参数的绑定方式
                AIFunctionFactoryOptions.ParameterBindingOptions[]? boundParameters = null;
                if (parameters.Length != 0 && key.GetBindParameterOptions is not null)
                {
                    boundParameters = new AIFunctionFactoryOptions.ParameterBindingOptions[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        boundParameters[i] = key.GetBindParameterOptions(parameters[i]);
                    }
                }

                // 用于模式生成
                var schemaOptions = key.SchemaOptions;

                // 获取参数编组委托
                ParameterMarshallers = parameters.Length > 0 ? new Func<AIFunctionArguments, CancellationToken, object?>[parameters.Length] : Array.Empty<Func<AIFunctionArguments, CancellationToken, object?>>();
                var expectedArgumentNames = new HashSet<string>(StringComparer.Ordinal);
                bool hasCustomParameterBinding = false;

                for (int i = 0; i < parameters.Length; i++)
                {
                    var options = boundParameters is not null ? boundParameters[i] : default;
                    ParameterMarshallers[i] = GetParameterMarshaller(serializerSettings, options, parameters[i]);

                    if (options.BindParameter is not null)
                    {
                        hasCustomParameterBinding = true;
                    }

                    var pType = parameters[i].ParameterType;
                    if (pType != typeof(CancellationToken) &&
                        pType != typeof(AIFunctionArguments) &&
                        pType != typeof(IServiceProvider) &&
                        !string.IsNullOrEmpty(parameters[i].Name))
                    {
                        string effectiveName = AIJsonUtilities.GetParameterSchemaName(parameters[i]);
                        if (!expectedArgumentNames.Add(effectiveName))
                        {
                            Throw.ArgumentException("method", $"Multiple parameters are mapped to the same name '{effectiveName}'.");
                        }
                    }
                }

                ExpectedArgumentNames = expectedArgumentNames;
                HasCustomParameterBinding = hasCustomParameterBinding;

                ReturnParameterMarshaller = GetReturnParameterMarshaller(key, serializerSettings, out var returnType);
                Method = key.Method;
                Name = key.Name ?? key.Method.GetCustomAttribute<AIFunctionNameAttribute>(inherit: true)?.Name ??
                       key.Method.GetCustomAttribute<DisplayNameAttribute>(inherit: true)?.DisplayName ??
                       GetFunctionName(key.Method);
                Description = key.Description ?? key.Method.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description ?? string.Empty;
                JsonSerializerSettings = serializerSettings;

                ReturnJsonSchema = returnType is null || key.ExcludeResultSchema
                    ? null
                    : AIJsonUtilities.CreateJsonSchema(NormalizeReturnType(returnType, serializerSettings),
                        settings: serializerSettings,
                        options: schemaOptions);

                JsonSchema = AIJsonUtilities.CreateFunctionJsonSchema(key.Method,
                    title: string.Empty,
                    description: string.Empty,
                    settings: serializerSettings,
                    options: schemaOptions);
            }

            public string Name { get; }
            public string Description { get; }
            public MethodInfo Method { get; }
            public JsonSerializerSettings JsonSerializerSettings { get; }
            public JToken JsonSchema { get; }
            public JToken? ReturnJsonSchema { get; }
            public Func<AIFunctionArguments, CancellationToken, object?>[] ParameterMarshallers { get; }
            public Func<object?, CancellationToken, ValueTask<object?>> ReturnParameterMarshaller { get; }
            public HashSet<string> ExpectedArgumentNames { get; }
            public bool HasCustomParameterBinding { get; }
            public ReflectionAIFunction? CachedDefaultInstance { get; set; }

            private static string GetFunctionName(MethodInfo method)
            {
                string name = SanitizeMemberName(method.Name);
                const string AsyncSuffix = "Async";

                if (IsAsyncMethod(method))
                {
                    int asyncIndex = name.LastIndexOf(AsyncSuffix, StringComparison.Ordinal);
                    if (asyncIndex > 0 &&
                        (asyncIndex + AsyncSuffix.Length == name.Length ||
                         ((asyncIndex + AsyncSuffix.Length < name.Length) && (name[asyncIndex + AsyncSuffix.Length] == '_'))))
                    {
                        name = string.Concat(new string(name.AsSpan(0, asyncIndex)), new string(name.AsSpan(asyncIndex + AsyncSuffix.Length)));
                    }
                }

                return name;

                static bool IsAsyncMethod(MethodInfo method)
                {
                    var t = method.ReturnType;
                    if (t == typeof(Task) || t == typeof(ValueTask))
                        return true;
                    if (t.IsGenericType)
                    {
                        var def = t.GetGenericTypeDefinition();
                        if (def == typeof(Task<>) || def == typeof(ValueTask<>) || def == typeof(IAsyncEnumerable<>))
                            return true;
                    }
                    return false;
                }
            }

            private static Func<AIFunctionArguments, CancellationToken, object?> GetParameterMarshaller(
                JsonSerializerSettings serializerSettings,
                AIFunctionFactoryOptions.ParameterBindingOptions bindingOptions,
                ParameterInfo parameter)
            {
                if (string.IsNullOrWhiteSpace(parameter.Name))
                {
                    Throw.ArgumentException(nameof(parameter), "Parameter is missing a name.");
                }

                var parameterType = parameter.ParameterType;

                // CancellationToken
                if (parameterType == typeof(CancellationToken))
                {
                    return static (_, cancellationToken) =>
                        cancellationToken == default ? _boxedDefaultCancellationToken : cancellationToken;
                }

                // 自定义绑定
                if (bindingOptions.BindParameter is { } bindParameter)
                {
                    return (arguments, _) => bindParameter(parameter, arguments);
                }

                // AIFunctionArguments
                if (parameterType == typeof(AIFunctionArguments))
                {
                    return static (arguments, _) => arguments;
                }

                // IServiceProvider
                if (parameterType == typeof(IServiceProvider))
                {
                    bool hasDefault = AIJsonUtilities.TryGetEffectiveDefaultValue(parameter, out _);
                    return (arguments, _) =>
                    {
                        var services = arguments.Services;
                        if (!hasDefault && services is null)
                        {
                            Throw.ArgumentNullException($"arguments.{nameof(AIFunctionArguments.Services)}", $"Services are required for parameter '{parameter.Name}'.");
                        }
                        return services;
                    };
                }

                // 其他参数 - 从字典获取
                bool hasDefaultValue = AIJsonUtilities.TryGetEffectiveDefaultValue(parameter, out object? effectiveDefaultValue);
                string argumentName = AIJsonUtilities.GetParameterSchemaName(parameter);

                return (arguments, _) =>
                {
                    if (arguments.TryGetValue(argumentName, out object? value))
                    {
                        return value switch
                        {
                            null => null,
                            _ when parameterType.IsInstanceOfType(value) => value,
                            JToken token => token.ToObject(parameterType, JsonSerializer.Create(serializerSettings)),
                            _ => MarshallViaJsonRoundtrip(value),
                        };

                        object? MarshallViaJsonRoundtrip(object value)
                        {
                            try
                            {
                                var json = JsonConvert.SerializeObject(value, serializerSettings);
                                return JsonConvert.DeserializeObject(json, parameterType, serializerSettings);
                            }
                            catch
                            {
                                return value;
                            }
                        }
                    }

                    if (!hasDefaultValue)
                    {
                        Throw.ArgumentException(nameof(arguments), $"The arguments dictionary is missing a value for the required parameter '{argumentName}'.");
                    }

                    return effectiveDefaultValue;
                };
            }

            private static Func<object?, CancellationToken, ValueTask<object?>> GetReturnParameterMarshaller(
                DescriptorKey key, JsonSerializerSettings serializerSettings, out Type? returnType)
            {
                returnType = key.Method.ReturnType;
                var rType = returnType;
                Func<object?, Type?, CancellationToken, ValueTask<object?>>? marshalResult = key.MarshalResult;

                // Void
                if (returnType == typeof(void))
                {
                    returnType = null;
                    if (marshalResult is not null)
                    {
                        return (result, cancellationToken) => marshalResult(null, null, cancellationToken);
                    }
                    return static (_, _) => new ValueTask<object?>((object?)null);
                }

                // Task
                if (returnType == typeof(Task))
                {
                    returnType = null;
                    if (marshalResult is not null)
                    {
                        return async (result, cancellationToken) =>
                        {
                            await ((Task)ThrowIfNullResult(result)).ConfigureAwait(true);
                            return await marshalResult(null, null, cancellationToken).ConfigureAwait(true);
                        };
                    }
                    return async static (result, _) =>
                    {
                        await ((Task)ThrowIfNullResult(result)).ConfigureAwait(true);
                        return null;
                    };
                }

                // ValueTask
                if (returnType == typeof(ValueTask))
                {
                    returnType = null;
                    if (marshalResult is not null)
                    {
                        return async (result, cancellationToken) =>
                        {
                            await ((ValueTask)ThrowIfNullResult(result)).ConfigureAwait(true);
                            return await marshalResult(null, null, cancellationToken).ConfigureAwait(true);
                        };
                    }
                    return async static (result, _) =>
                    {
                        await ((ValueTask)ThrowIfNullResult(result)).ConfigureAwait(true);
                        return null;
                    };
                }

                // 泛型 Task<T> / ValueTask<T>
                if (returnType.IsGenericType)
                {
                    var genericDef = returnType.GetGenericTypeDefinition();

                    // Task<T>
                    if (genericDef == typeof(Task<>))
                    {
                        var taskResultGetter = GetMethodFromGenericMethodDefinition(returnType, _taskGetResult);
                        returnType = taskResultGetter.ReturnType;

                        if (marshalResult is not null)
                        {
                            return async (taskObj, cancellationToken) =>
                            {
                                await ((Task)ThrowIfNullResult(taskObj)).ConfigureAwait(true);
                                object? result = ReflectionInvoke(taskResultGetter, taskObj, null);
                                return await marshalResult(result, taskResultGetter.ReturnType, cancellationToken).ConfigureAwait(true);
                            };
                        }

                        if (IsAIContentRelatedType(returnType))
                        {
                            return async (taskObj, cancellationToken) =>
                            {
                                await ((Task)ThrowIfNullResult(taskObj)).ConfigureAwait(true);
                                return ReflectionInvoke(taskResultGetter, taskObj, null);
                            };
                        }
                        rType = returnType;

                        return async (taskObj, cancellationToken) =>
                        {
                            await ((Task)ThrowIfNullResult(taskObj)).ConfigureAwait(true);
                            object? result = ReflectionInvoke(taskResultGetter, taskObj, null);
                            return await SerializeResultAsync(result, rType, serializerSettings, cancellationToken).ConfigureAwait(true);
                        };
                    }

                    // ValueTask<T>
                    if (genericDef == typeof(ValueTask<>))
                    {
                        var valueTaskAsTask = GetMethodFromGenericMethodDefinition(returnType, _valueTaskAsTask);
                        var asTaskResultGetter = GetMethodFromGenericMethodDefinition(valueTaskAsTask.ReturnType, _taskGetResult);
                        returnType = asTaskResultGetter.ReturnType;

                        if (marshalResult is not null)
                        {
                            return async (taskObj, cancellationToken) =>
                            {
                                var task = (Task)ReflectionInvoke(valueTaskAsTask, ThrowIfNullResult(taskObj), null)!;
                                await task.ConfigureAwait(true);
                                object? result = ReflectionInvoke(asTaskResultGetter, task, null);
                                return await marshalResult(result, asTaskResultGetter.ReturnType, cancellationToken).ConfigureAwait(true);
                            };
                        }

                        if (IsAIContentRelatedType(returnType))
                        {
                            return async (taskObj, cancellationToken) =>
                            {
                                var task = (Task)ReflectionInvoke(valueTaskAsTask, ThrowIfNullResult(taskObj), null)!;
                                await task.ConfigureAwait(true);
                                return ReflectionInvoke(asTaskResultGetter, task, null);
                            };
                        }
                        rType = returnType;
                        return async (taskObj, cancellationToken) =>
                        {
                            var task = (Task)ReflectionInvoke(valueTaskAsTask, ThrowIfNullResult(taskObj), null)!;
                            await task.ConfigureAwait(true);
                            object? result = ReflectionInvoke(asTaskResultGetter, task, null);
                            return await SerializeResultAsync(result, rType, serializerSettings, cancellationToken).ConfigureAwait(true);
                        };
                    }
                }

                // 非异步返回
                if (marshalResult is not null)
                {
                    var returnTypeCopy = returnType;
                    return (result, cancellationToken) => marshalResult(result, returnTypeCopy, cancellationToken);
                }

                if (IsAIContentRelatedType(returnType))
                {
                    return static (result, _) => new ValueTask<object?>(result);
                }
                rType = returnType;
                return (result, cancellationToken) => SerializeResultAsync(result, rType, serializerSettings, cancellationToken);

                static async ValueTask<object?> SerializeResultAsync(object? result, Type resultType, JsonSerializerSettings settings, CancellationToken cancellationToken)
                {
                    // 使用 Newtonsoft.Json 序列化为 JToken
                    var serializer = JsonSerializer.Create(settings);

                    // 对于 IAsyncEnumerable，需要特殊处理
                    if (result is IAsyncEnumerable<object> asyncEnumerable)
                    {
                        var list = new List<object>();
                        await foreach (var item in asyncEnumerable.WithCancellation(cancellationToken))
                        {
                            list.Add(item);
                        }
                        result = list;
                    }

                    using var stringWriter = new StringWriter();
                    using var jsonWriter = new JsonTextWriter(stringWriter);
                    serializer.Serialize(jsonWriter, result, resultType);
                    await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(true);

                    return JToken.Parse(stringWriter.ToString());
                }

                static object ThrowIfNullResult(object? result) =>
                    result ?? throw new InvalidOperationException("Function returned null unexpectedly.");
            }

            private static readonly MethodInfo _taskGetResult = typeof(Task<>).GetProperty(nameof(Task<int>.Result), BindingFlags.Instance | BindingFlags.Public)!.GetMethod!;
            private static readonly MethodInfo _valueTaskAsTask = typeof(ValueTask<>).GetMethod(nameof(ValueTask<int>.AsTask), BindingFlags.Instance | BindingFlags.Public)!;

            private static MethodInfo GetMethodFromGenericMethodDefinition(Type specializedType, MethodInfo genericMethodDefinition)
            {
#if NET
                return (MethodInfo)specializedType.GetMemberWithSameMetadataDefinitionAs(genericMethodDefinition);
#else
                const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
                return specializedType.GetMethods(All).First(m => m.MetadataToken == genericMethodDefinition.MetadataToken);
#endif
            }

            private static bool IsAIContentRelatedType(Type type) =>
                typeof(AIContent).IsAssignableFrom(type) ||
                typeof(IEnumerable<AIContent>).IsAssignableFrom(type);

            private static string? GetReturnParameterDescription(MethodInfo method)
            {
                try
                {
                    return method.ReturnParameter.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description;
                }
                catch
                {
                    return null;
                }
            }

            private static Type NormalizeReturnType(Type type, JsonSerializerSettings? settings)
            {
                settings ??= MCP.AI.AIJsonUtilities.DefaultSettings;

                if (settings == MCP.AI.AIJsonUtilities.DefaultSettings)
                {
                    if (typeof(IEnumerable<AIContent>).IsAssignableFrom(type))
                        return typeof(IEnumerable<AIContent>);
                    if (typeof(IEnumerable<ChatMessage>).IsAssignableFrom(type))
                        return typeof(IEnumerable<ChatMessage>);
                    if (typeof(IEnumerable<string>).IsAssignableFrom(type))
                        return typeof(IEnumerable<string>);
                }

                return type;
            }

            private struct DescriptorKey : IEquatable<DescriptorKey>
            {
                public MethodInfo Method { get; }
                public string? Name { get; }
                public string? Description { get; }
                public Func<ParameterInfo, AIFunctionFactoryOptions.ParameterBindingOptions>? GetBindParameterOptions { get; }
                public Func<object?, Type?, CancellationToken, ValueTask<object?>>? MarshalResult { get; }
                public bool ExcludeResultSchema { get; }
                public AIJsonSchemaCreateOptions SchemaOptions { get; }

                public DescriptorKey(
                    MethodInfo method,
                    string? name,
                    string? description,
                    Func<ParameterInfo, AIFunctionFactoryOptions.ParameterBindingOptions>? getBindParameterOptions,
                    Func<object?, Type?, CancellationToken, ValueTask<object?>>? marshalResult,
                    bool excludeResultSchema,
                    AIJsonSchemaCreateOptions schemaOptions)
                {
                    Method = method;
                    Name = name;
                    Description = description;
                    GetBindParameterOptions = getBindParameterOptions;
                    MarshalResult = marshalResult;
                    ExcludeResultSchema = excludeResultSchema;
                    SchemaOptions = schemaOptions;
                }

                public readonly bool Equals(DescriptorKey other)
                {
                    return Equals(Method, other.Method) &&
                           Name == other.Name &&
                           Description == other.Description &&
                           Equals(GetBindParameterOptions, other.GetBindParameterOptions) &&
                           Equals(MarshalResult, other.MarshalResult) &&
                           ExcludeResultSchema == other.ExcludeResultSchema &&
                           Equals(SchemaOptions, other.SchemaOptions);
                }

                public override bool Equals(object? obj)
                {
                    return obj is DescriptorKey other && Equals(other);
                }

                public readonly override int GetHashCode()
                {
                    unchecked
                    {
                        var hashCode = Method?.GetHashCode() ?? 0;
                        hashCode = (hashCode * 397) ^ (Name?.GetHashCode() ?? 0);
                        hashCode = (hashCode * 397) ^ (Description?.GetHashCode() ?? 0);
                        hashCode = (hashCode * 397) ^ (GetBindParameterOptions?.GetHashCode() ?? 0);
                        hashCode = (hashCode * 397) ^ (MarshalResult?.GetHashCode() ?? 0);
                        hashCode = (hashCode * 397) ^ ExcludeResultSchema.GetHashCode();
                        hashCode = (hashCode * 397) ^ (SchemaOptions?.GetHashCode() ?? 0);
                        return hashCode;
                    }
                }

                public static bool operator ==(DescriptorKey left, DescriptorKey right) => left.Equals(right);
                public static bool operator !=(DescriptorKey left, DescriptorKey right) => !left.Equals(right);
            }
        }

        private static string SanitizeMemberName(string memberName)
        {
            // 处理编译器生成的名称
            var match = CompilerGeneratedNameRegex().Match(memberName);
            if (match.Success)
            {
                memberName = $"{match.Groups[1].Value}_{match.Groups[2].Value}";
            }

            return InvalidNameCharsRegex().Replace(memberName, "_");
        }

        private static Regex CompilerGeneratedNameRegex() => _compilerGeneratedNameRegex;
        private static readonly Regex _compilerGeneratedNameRegex = new(@"^<([^>]+)>\w__(.+)", RegexOptions.Compiled);
        private static Regex InvalidNameCharsRegex() => _invalidNameCharsRegex;
        private static readonly Regex _invalidNameCharsRegex = new("[^0-9A-Za-z]+", RegexOptions.Compiled);

        private static readonly Regex _potentiallyJsonRegex = new(PotentiallyJsonRegexString, RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

        private const string PotentiallyJsonRegexString =
            @"^\s*        # Optional whitespace at the start of the string
    ( null   # null literal
    | false  # false literal
    | true   # true literal
    | -?[0-9]# number
    | ""      # string
    | \[     # start array
    | {      # start object
    | //     # Start of single-line comment
    | /\*    # Start of multi-line comment
    )";

        private static bool IsPotentiallyJson(string value) => _potentiallyJsonRegex.IsMatch(value);

        private static Func<MethodInfo, object?, object?[]?, object?> ReflectionInvoke = (method, target, arguments) =>
        {
#if NET
            return method.Invoke(target, BindingFlags.DoNotWrapExceptions, binder: null, arguments, culture: null);
#else
            try
            {
                return method.Invoke(target, BindingFlags.Default, binder: null, arguments, culture: null);
            }
            catch (TargetInvocationException e) when (e.InnerException is not null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                throw;
            }
#endif
        };

    }
}