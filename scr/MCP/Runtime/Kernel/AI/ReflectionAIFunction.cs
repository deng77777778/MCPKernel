#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MCP.AI
{
    internal sealed class ReflectionAIFunction : AIFunction
    {
        private readonly ReflectionAIFunctionDescriptor _descriptor;
        private readonly object? _target;
        private readonly Func<AIFunctionArguments, object>? _createInstanceFunc;
        private readonly IReadOnlyDictionary<string, object?> _additionalProperties;
        private readonly ArrayPool<object?> _arrayPool = ArrayPool<object?>.Shared;

        public override string Name => _descriptor.Name;
        public override string Description => _descriptor.Description;
        public override MethodInfo UnderlyingMethod => _descriptor.Method;
        public override JToken JsonSchema => _descriptor.JsonSchema;
        public override JToken? ReturnJsonSchema => _descriptor.ReturnJsonSchema;
        public override JsonSerializerSettings JsonSerializerSettings => _descriptor.JsonSerializerSettings;
        public override IReadOnlyDictionary<string, object?> AdditionalProperties => _additionalProperties;

        private ReflectionAIFunction(ReflectionAIFunctionDescriptor descriptor, object? target,
            AIFunctionFactoryOptions options)
        {
            _descriptor = descriptor;
            _target = target;
            _additionalProperties = options.AdditionalProperties ?? EmptyReadOnlyDictionary<string, object?>.Instance;
        }

        private ReflectionAIFunction(ReflectionAIFunctionDescriptor descriptor,
            Func<AIFunctionArguments, object> createInstanceFunc,
            AIFunctionFactoryOptions options)
        {
            _descriptor = descriptor;
            _createInstanceFunc = createInstanceFunc;
            _additionalProperties = options.AdditionalProperties ?? EmptyReadOnlyDictionary<string, object?>.Instance;
        }

        public static ReflectionAIFunction Build(MethodInfo method, object? target, AIFunctionFactoryOptions options)
        {
            if (method.ContainsGenericParameters)
                throw new ArgumentException("Open generic methods are not supported", nameof(method));
            if (!method.IsStatic && target == null)
                throw new ArgumentNullException(nameof(target), "Target must not be null for an instance method.");

            var descriptor = ReflectionAIFunctionDescriptor.GetOrCreate(method, options);
            return new ReflectionAIFunction(descriptor, target, options);
        }

        public static ReflectionAIFunction Build(MethodInfo method,
            Func<AIFunctionArguments, object> createInstanceFunc,
            AIFunctionFactoryOptions options)
        {
            if (method.ContainsGenericParameters)
                throw new ArgumentException("Open generic methods are not supported", nameof(method));
            if (method.IsStatic)
                throw new ArgumentException("The method must be an instance method.", nameof(method));

            var descriptor = ReflectionAIFunctionDescriptor.GetOrCreate(method, options);
            return new ReflectionAIFunction(descriptor, createInstanceFunc, options);
        }

        protected override async ValueTask<object?> InvokeCoreAsync(
            AIFunctionArguments arguments,
            CancellationToken cancellationToken)
        {
            bool disposeTarget = false;
             object? target = _target;

            try
            {
                if (_createInstanceFunc != null)
                {
                    target = _createInstanceFunc(arguments);
                    if (target == null)
                        throw new InvalidOperationException("Unable to create an instance of the target type.");
                    disposeTarget = true;
                }

                // 验证未映射的参数
                if (_descriptor.JsonSerializerSettings.MissingMemberHandling == MissingMemberHandling.Error &&
                    arguments.Count > 0 && !_descriptor.HasCustomParameterBinding)
                {
                    ValidateArguments(arguments);
                }

                var marshallers = _descriptor.ParameterMarshallers;

                // 快速路径：无参数
                if (marshallers.Length == 0)
                {
                    return await _descriptor.InvokeAsync(target, null, cancellationToken).ConfigureAwait(true);
                }

                // 使用 ArrayPool 租用数组
                var args = _arrayPool.Rent(marshallers.Length);
                try
                {
                    for (int i = 0; i < marshallers.Length; i++)
                    {
                        args[i] = marshallers[i](arguments, cancellationToken);
                    }

                    return await _descriptor.InvokeAsync(target, args, cancellationToken).ConfigureAwait(true);
                }
                finally
                {
                    // 清空并归还数组
                    Array.Clear(args, 0, marshallers.Length);
                    _arrayPool.Return(args);
                }
            }
            finally
            {
                if (disposeTarget && target is IDisposable d)
                    d.Dispose();
            }
        }

        private void ValidateArguments(AIFunctionArguments arguments)
        {
            var expectedNames = _descriptor.ExpectedArgumentNames;
            int matched = 0;
            foreach (var name in expectedNames)
            {
                if (arguments.ContainsKey(name))
                    matched++;
            }

            if (matched != arguments.Count)
            {
                foreach (var kvp in arguments)
                {
                    if (!expectedNames.Contains(kvp.Key))
                    {
                        throw new ArgumentException(
                            $"The arguments dictionary contains an unexpected key '{kvp.Key}' that does not correspond to any parameter of '{Name}'.",
                            nameof(arguments));
                    }
                }
                throw new ArgumentException(
                    $"The arguments dictionary contains keys that do not correspond to any parameter of '{Name}'.",
                    nameof(arguments));
            }
        }

        public override object? GetService(Type serviceType, object? serviceKey = null)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            return serviceKey == null && serviceType.IsInstanceOfType(this) ? this : null;
        }
    }
}