#nullable enable
using MCP.AI;
using MCP.Kernel.Cache;
using MCP.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// MCP Schema 生成器基类
    /// </summary>
    public abstract class McpSchemaGeneratorBase<TResult, TAttribute> : IMcpSchemaGenerator<TResult>
        where TResult : IBaseMetadata, new()
        where TAttribute : Attribute
    {
        private static readonly Type _typeAttributeType;
        private static readonly Func<Type, bool> _hasTypeAttribute;
        private static readonly Func<MethodInfo, TAttribute?> _getMethodAttribute;

        static McpSchemaGeneratorBase()
        {
            _typeAttributeType = GetTypeAttributeType();
            _hasTypeAttribute = type => type.GetCustomAttribute(_typeAttributeType) != null;
            _getMethodAttribute = method => method.GetCustomAttribute<TAttribute>();
        }

        private static Type GetTypeAttributeType()
        {
            var typeName = typeof(TAttribute).Name.Replace("Attribute", "TypeAttribute");
            var type = typeof(TAttribute).Assembly.GetType($"{typeof(TAttribute).Namespace}.{typeName}");
            return type ?? typeof(object);
        }

        /// <inheritdoc />
        public virtual string Name => GetType().Name.Replace("SchemaGenerator", "");

        private AIJsonSchemaCreateOptions? _currentOptions;

        /// <summary>
        /// 获取当前选项
        /// </summary>
        protected AIJsonSchemaCreateOptions CurrentOptions => _currentOptions ?? AIJsonSchemaCreateOptions.Default;

        /// <inheritdoc />
        public virtual IEnumerable<TResult> Generate(Type type, AIJsonSchemaCreateOptions? options = null)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            _currentOptions = options ?? AIJsonSchemaCreateOptions.Default;

            foreach (var method in GetMethods(type))
            {
                var result = Generate(method);
                if (result != null)
                {
                    yield return result;
                }
            }
        }  

        /// <inheritdoc />
        public virtual TResult? Generate(MethodInfo method, AIJsonSchemaCreateOptions? options = null)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            _currentOptions = options ?? AIJsonSchemaCreateOptions.Default;

            return GenerateCore(method);
        }

        /// <summary>
        /// 核心生成方法
        /// </summary>
        protected abstract TResult? GenerateCore(MethodInfo method);

        /// <summary>
        /// 获取需要处理的方法列表
        /// </summary>
        public virtual IEnumerable<MethodInfo> GetMethods(Type type)
        {
            if (!_hasTypeAttribute(type))
            {
                // 检查嵌套类型
                foreach (var nestedType in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (_hasTypeAttribute(nestedType))
                    {
                        foreach (var method in GetMethodsFromType(nestedType))
                        {
                            yield return method;
                        }
                    }
                }
                yield break;
            }

            foreach (var method in GetMethodsFromType(type))
            {
                yield return method;
            }

            // 嵌套类型
            foreach (var nestedType in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (_hasTypeAttribute(nestedType))
                {
                    foreach (var method in GetMethodsFromType(nestedType))
                    {
                        yield return method;
                    }
                }
            }
        }

        private IEnumerable<MethodInfo> GetMethodsFromType(Type type)
        {
            return type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => _getMethodAttribute(m) != null);
        }

        #region 通用辅助方法

        protected virtual TAttribute? GetAttribute(MethodInfo method) => _getMethodAttribute(method);

        protected virtual string? GetDescription(MethodInfo method)
            => method.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description;

        protected virtual string? GetParameterDescription(ParameterInfo parameter)
            => parameter.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description;

        protected virtual string GetName(MethodInfo method, string? customName = null)
            => customName ?? NameHelper.GetFunctionName(method);

        protected virtual bool IsSpecialParameter(ParameterInfo parameter)
        {
            var type = parameter.ParameterType;
            return type == typeof(System.Threading.CancellationToken) ||
                   type == typeof(IServiceProvider) ||
                   (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IProgress<>));
        }

        protected virtual bool IsParameterRequired(ParameterInfo parameter)
            => !parameter.HasDefaultValue && !parameter.IsOptional &&
               parameter.ParameterType.IsValueType && Nullable.GetUnderlyingType(parameter.ParameterType) == null;

        #endregion
    }
}
