// Helpers/TypeHelper.cs
#nullable enable
using MCP.AI;
using MCP.Kernel.Cache;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// 类型辅助类
    /// </summary>
    public static class TypeHelper
    {
        private static readonly UnifiedCache _cache = UnifiedCache.Instance;

        #region 类型判断

        public static bool IsPrimitiveType(Type type)
        {
            if (type == null) return false;

            return type.IsPrimitive ||
                   type == typeof(string) ||
                   type == typeof(decimal) ||
                   type == typeof(DateTime) ||
                   type == typeof(DateTimeOffset) ||
                   type == typeof(Guid) ||
                   type == typeof(Uri) ||
                   type == typeof(TimeSpan) ||
                   type == typeof(Version) ||
                   type == typeof(char) ||
                   type == typeof(nint) ||
                   type == typeof(nuint);
        }

        public static bool IsValueType(Type type)
        {
            if (type == null) return false;
            return type.IsValueType;
        }

        public static bool IsTaskType(Type type)
        {
            if (type == null) return false;

            if (_cache.TryGetTaskType(type, out var isTask))
                return isTask;

            isTask = type == typeof(Task) ||
                     (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>));
            _cache.AddTaskType(type, isTask);
            return isTask;
        }

        public static bool IsValueTaskType(Type type)
        {
            if (type == null) return false;

            if (_cache.TryGetValueTaskType(type, out var isValueTask))
                return isValueTask;

            isValueTask = type == typeof(ValueTask) ||
                          (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTask<>));
            _cache.AddValueTaskType(type, isValueTask);
            return isValueTask;
        }

        public static bool IsAsyncMethod(MethodInfo method)
        {
            if (method == null) return false;

            if (_cache.TryGetAsyncMethod(method, out var isAsync))
                return isAsync;

            var ret = method.ReturnType;
            isAsync = ret == typeof(Task) || ret == typeof(ValueTask) ||
                      (ret.IsGenericType && (ret.GetGenericTypeDefinition() == typeof(Task<>) ||
                                             ret.GetGenericTypeDefinition() == typeof(ValueTask<>)));
            _cache.AddAsyncMethod(method, isAsync);
            return isAsync;
        }

        public static bool IsAIContentRelated(Type type)
        {
            if (type == null) return false;

            if (_cache.TryGetAIContentRelated(type, out var isRelated))
                return isRelated;

            isRelated = typeof(AIContent).IsAssignableFrom(type);
            if (!isRelated)
            {
                foreach (var iface in type.GetInterfaces())
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        var arg = iface.GetGenericArguments()[0];
                        if (typeof(AIContent).IsAssignableFrom(arg))
                        {
                            isRelated = true;
                            break;
                        }
                    }
                }
            }

            _cache.AddAIContentRelated(type, isRelated);
            return isRelated;
        }

        public static Type? GetUnwrappedReturnType(Type returnType)
        {
            if (returnType == null) return null;

            if (_cache.TryGetUnwrappedReturnType(returnType, out var unwrapped))
                return unwrapped;

            if (returnType == typeof(void) || returnType == typeof(Task) || returnType == typeof(ValueTask))
            {
                _cache.AddUnwrappedReturnType(returnType, null);
                return null;
            }

            if (returnType.IsGenericType)
            {
                var genType = returnType.GetGenericTypeDefinition();
                if (genType == typeof(Task<>) || genType == typeof(ValueTask<>))
                {
                    unwrapped = returnType.GetGenericArguments()[0];
                    _cache.AddUnwrappedReturnType(returnType, unwrapped);
                    return unwrapped;
                }
            }

            _cache.AddUnwrappedReturnType(returnType, returnType);
            return returnType;
        }

        public static bool IsSpecialParameter(ParameterInfo parameter)
        {
            var type = parameter.ParameterType;
            return type == typeof(CancellationToken) ||
                   type == typeof(AIFunctionArguments) ||
                   type == typeof(IServiceProvider);
        }

        public static bool IsParameterRequired(ParameterInfo parameter)
        {
            if (parameter.HasDefaultValue || parameter.IsOptional)
                return false;

            var type = parameter.ParameterType;
            if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
                return true;

            return false;
        }

        #endregion

        #region 集合类型判断

        public static bool IsCollectionType(Type type)
        {
            if (type == null) return false;
            if (type == typeof(string) || type == typeof(byte[])) return false;

            if (type.IsArray) return true;

            if (type.IsGenericType)
            {
                var genDef = type.GetGenericTypeDefinition();
                if (genDef == typeof(IEnumerable<>) || genDef == typeof(ICollection<>) ||
                    genDef == typeof(IList<>) || genDef == typeof(List<>) ||
                    genDef == typeof(HashSet<>) || genDef == typeof(ISet<>))
                    return true;
            }

            return typeof(IEnumerable).IsAssignableFrom(type);
        }

        public static bool IsDictionaryType(Type type)
        {
            if (type == null || !type.IsGenericType) return false;

            var genDef = type.GetGenericTypeDefinition();
            return genDef == typeof(Dictionary<,>) ||
                   genDef == typeof(IDictionary<,>) ||
                   genDef == typeof(IReadOnlyDictionary<,>);
        }

        public static Type? GetElementType(Type type)
        {
            if (type == null) return null;

            if (type.IsArray)
                return type.GetElementType();

            if (type.IsGenericType)
            {
                var genDef = type.GetGenericTypeDefinition();
                if (genDef == typeof(IEnumerable<>) || genDef == typeof(ICollection<>) ||
                    genDef == typeof(IList<>) || genDef == typeof(List<>) ||
                    genDef == typeof(HashSet<>) || genDef == typeof(ISet<>))
                    return type.GetGenericArguments()[0];
            }

            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return iface.GetGenericArguments()[0];
            }

            return null;
        }

        #endregion

        #region 反射缓存

        private static readonly Dictionary<Type, PropertyInfo[]> _propertyCache = new();
        private static readonly Dictionary<Type, FieldInfo[]> _fieldCache = new();
        private static readonly object _cacheLock = new();

        public static PropertyInfo[] GetSerializableProperties(Type type)
        {
            lock (_cacheLock)
            {
                if (_propertyCache.TryGetValue(type, out var props))
                    return props;

                props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetIndexParameters().Length == 0 &&
                                p.GetCustomAttribute<JsonIgnoreAttribute>() == null &&
                                p.CanRead)
                    .ToArray();

                _propertyCache[type] = props;
                return props;
            }
        }

        public static FieldInfo[] GetSerializableFields(Type type)
        {
            lock (_cacheLock)
            {
                if (_fieldCache.TryGetValue(type, out var fields))
                    return fields;

                fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(f => !f.IsStatic && f.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                    .ToArray();

                _fieldCache[type] = fields;
                return fields;
            }
        }

        public static void ClearReflectionCache()
        {
            lock (_cacheLock)
            {
                _propertyCache.Clear();
                _fieldCache.Clear();
            }
        }

        #endregion
    }
}