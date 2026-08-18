#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace MCP.AI
{
    /// <summary>
    /// 高性能类型缓存
    /// </summary>
    public static class TypeCache
    {
        private static readonly ConcurrentDictionary<Type, bool> _isAIContentRelated = new();
        private static readonly ConcurrentDictionary<Type, bool> _isPrimitive = new();
        private static readonly ConcurrentDictionary<Type, bool> _isValueType = new();
        private static readonly ConcurrentDictionary<Type, bool> _isTask = new();
        private static readonly ConcurrentDictionary<Type, bool> _isValueTask = new();
        private static readonly ConcurrentDictionary<Type, Type?> _nullableUnderlying = new();
        private static readonly ConcurrentDictionary<Type, Type> _enumerableElementType = new();
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new();
        private static readonly ConcurrentDictionary<Type, MethodInfo[]> _methodCache = new();

        /// <summary>
        /// 快速判断是否与AIContent相关
        /// </summary>
        public static bool IsAIContentRelated(Type type)
        {
            return _isAIContentRelated.GetOrAdd(type, t =>
            {
                if (t == null) return false;

                // 检查自身
                if (typeof(AIContent).IsAssignableFrom(t)) return true;

                // 检查IEnumerable<AIContent>
                foreach (var iface in t.GetInterfaces())
                {
                    if (iface.IsGenericType &&
                        iface.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
                        typeof(AIContent).IsAssignableFrom(iface.GetGenericArguments()[0]))
                    {
                        return true;
                    }
                }
                return false;
            });
        }

        /// <summary>
        /// 快速判断是否原始类型
        /// </summary>
        public static bool IsPrimitive(Type type)
        {
            return _isPrimitive.GetOrAdd(type, t =>
                t.IsPrimitive ||
                t == typeof(string) ||
                t == typeof(decimal) ||
                t == typeof(DateTime) ||
                t == typeof(DateTimeOffset) ||
                t == typeof(Guid) ||
                t == typeof(TimeSpan) ||
                t == typeof(Uri));
        }

        /// <summary>
        /// 快速判断是否值类型
        /// </summary>
        public static bool IsValueType(Type type)
        {
            return _isValueType.GetOrAdd(type, t => t.IsValueType);
        }

        /// <summary>
        /// 快速判断是否Task类型
        /// </summary>
        public static bool IsTask(Type type)
        {
            return _isTask.GetOrAdd(type, t =>
                t == typeof(Task) ||
                (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Task<>)));
        }

        /// <summary>
        /// 快速判断是否ValueTask类型
        /// </summary>
        public static bool IsValueTask(Type type)
        {
            return _isValueTask.GetOrAdd(type, t =>
                t == typeof(ValueTask) ||
                (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ValueTask<>)));
        }

        /// <summary>
        /// 获取可空类型的底层类型
        /// </summary>
        public static Type? GetNullableUnderlying(Type type)
        {
            return _nullableUnderlying.GetOrAdd(type, Nullable.GetUnderlyingType);
        }

        /// <summary>
        /// 获取IEnumerable的元素类型
        /// </summary>
        public static Type? GetEnumerableElementType(Type type)
        {
            if (_enumerableElementType.TryGetValue(type, out var elementType))
                return elementType;

            if (type.IsArray)
            {
                elementType = type.GetElementType();
                _enumerableElementType[type] = elementType!;
                return elementType;
            }

            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    elementType = iface.GetGenericArguments()[0];
                    _enumerableElementType[type] = elementType;
                    return elementType;
                }
            }

            _enumerableElementType[type] = null!;
            return null;
        }

        /// <summary>
        /// 缓存获取属性
        /// </summary>
        public static PropertyInfo[] GetCachedProperties(Type type, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
        {
            return _propertyCache.GetOrAdd(type, t => t.GetProperties(flags));
        }

        /// <summary>
        /// 缓存获取方法
        /// </summary>
        public static MethodInfo[] GetCachedMethods(Type type, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
        {
            return _methodCache.GetOrAdd(type, t => t.GetMethods(flags));
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public static void Clear()
        {
            _isAIContentRelated.Clear();
            _isPrimitive.Clear();
            _isValueType.Clear();
            _isTask.Clear();
            _isValueTask.Clear();
            _nullableUnderlying.Clear();
            _enumerableElementType.Clear();
            _propertyCache.Clear();
            _methodCache.Clear();
        }
    }
}