#nullable enable
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MCP.Kernel.Cache
{
    /// <summary>
    /// Unity 反射缓存 - P0 优化
    /// 缓存反射结果，避免重复反射开销
    /// </summary>
    public static class UnityReflectionCache
    {
        // 缓存字典
        private static readonly Dictionary<Type, PropertyInfo[]> _propertiesCache = new();
        private static readonly Dictionary<Type, FieldInfo[]> _fieldsCache = new();
        private static readonly Dictionary<Type, bool> _isValueTypeCache = new();
        private static readonly Dictionary<Type, bool> _isEnumCache = new();
        private static readonly Dictionary<Type, bool> _isCollectionCache = new();
        private static readonly Dictionary<Type, bool> _isDictionaryCache = new();
        private static readonly Dictionary<Type, Type?> _elementTypeCache = new();

        // 同步锁（Unity 中 Dictionary 非线程安全）
        private static readonly object _lock = new();

        #region Properties

        public static PropertyInfo[] GetProperties(Type type)
        {
            lock (_lock)
            {
                if (_propertiesCache.TryGetValue(type, out var props))
                    return props;

                props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                _propertiesCache[type] = props;
                return props;
            }
        }

        public static PropertyInfo[] GetReadableProperties(Type type)
        {
            lock (_lock)
            {
                var cacheKey = new TypeKey(type, "readable");

                // 使用不同的缓存键避免冲突
                if (_propertiesCache.TryGetValue(type, out var allProps))
                {
                    // 缓存中存储的是所有属性，需要过滤
                    var result = new List<PropertyInfo>(allProps.Length);
                    foreach (var p in allProps)
                    {
                        // 检查索引属性 - 这是导致 StackOverflow 的关键
                        if (p.GetIndexParameters().Length > 0)
                            continue;

                        if (p.CanRead && p.GetMethod != null && !p.GetMethod.IsStatic &&
                            p.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                        {
                            result.Add(p);
                        }
                    }
                    return result.ToArray();
                }

                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                _propertiesCache[type] = props;

                var filtered = new List<PropertyInfo>(props.Length);
                foreach (var p in props)
                {
                    // 检查索引属性 - 这是导致 StackOverflow 的关键
                    if (p.GetIndexParameters().Length > 0)
                        continue;

                    if (p.CanRead && p.GetMethod != null && !p.GetMethod.IsStatic &&
                        p.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                    {
                        filtered.Add(p);
                    }
                }
                return filtered.ToArray();
            }
        }
        #endregion

        #region Fields

        public static FieldInfo[] GetFields(Type type)
        {
            lock (_lock)
            {
                if (_fieldsCache.TryGetValue(type, out var fields))
                    return fields;

                fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                _fieldsCache[type] = fields;
                return fields;
            }
        }

        public static FieldInfo[] GetSerializableFields(Type type)
        {
            lock (_lock)
            {
                if (_fieldsCache.TryGetValue(type, out var allFields))
                {
                    var result = new List<FieldInfo>(allFields.Length);
                    foreach (var f in allFields)
                    {
                        if (!f.IsStatic && f.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                        {
                            result.Add(f);
                        }
                    }
                    return result.ToArray();
                }

                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                _fieldsCache[type] = fields;

                var filtered = new List<FieldInfo>(fields.Length);
                foreach (var f in fields)
                {
                    if (!f.IsStatic && f.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                    {
                        filtered.Add(f);
                    }
                }
                return filtered.ToArray();
            }
        }

        #endregion

        #region Type Checks (缓存类型判断)

        public static bool IsValueTypeRequired(Type type)
        {
            lock (_lock)
            {
                if (_isValueTypeCache.TryGetValue(type, out var result))
                    return result;

                result = type.IsValueType && Nullable.GetUnderlyingType(type) == null;
                _isValueTypeCache[type] = result;
                return result;
            }
        }

        public static bool IsEnumType(Type type)
        {
            lock (_lock)
            {
                if (_isEnumCache.TryGetValue(type, out var result))
                    return result;

                result = type.IsEnum;
                _isEnumCache[type] = result;
                return result;
            }
        }

        public static bool IsCollectionType(Type type)
        {
            lock (_lock)
            {
                if (_isCollectionCache.TryGetValue(type, out var result))
                    return result;

                if (type == typeof(string) || type == typeof(byte[]))
                {
                    result = false;
                    _isCollectionCache[type] = result;
                    return result;
                }

                if (type.IsArray)
                {
                    result = true;
                    _isCollectionCache[type] = result;
                    return result;
                }

                if (type.IsGenericType)
                {
                    var genericDef = type.GetGenericTypeDefinition();
                    if (genericDef == typeof(IEnumerable<>) ||
                        genericDef == typeof(ICollection<>) ||
                        genericDef == typeof(IList<>) ||
                        genericDef == typeof(List<>) ||
                        genericDef == typeof(HashSet<>) ||
                        genericDef == typeof(ISet<>))
                    {
                        result = true;
                        _isCollectionCache[type] = result;
                        return result;
                    }
                }

                // 检查 IEnumerable<T> 接口
                foreach (var iface in type.GetInterfaces())
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        result = true;
                        _isCollectionCache[type] = result;
                        return result;
                    }
                }

                result = false;
                _isCollectionCache[type] = result;
                return result;
            }
        }

        public static bool IsDictionaryType(Type type)
        {
            lock (_lock)
            {
                if (_isDictionaryCache.TryGetValue(type, out var result))
                    return result;

                if (!type.IsGenericType)
                {
                    result = false;
                    _isDictionaryCache[type] = result;
                    return result;
                }

                var genericDef = type.GetGenericTypeDefinition();
                result = genericDef == typeof(Dictionary<,>) ||
                         genericDef == typeof(IDictionary<,>) ||
                         genericDef == typeof(IReadOnlyDictionary<,>);

                _isDictionaryCache[type] = result;
                return result;
            }
        }

        public static Type? GetElementType(Type type)
        {
            lock (_lock)
            {
                if (_elementTypeCache.TryGetValue(type, out var result))
                    return result;

                if (type.IsArray)
                {
                    result = type.GetElementType();
                    _elementTypeCache[type] = result;
                    return result;
                }

                if (type.IsGenericType)
                {
                    var genericDef = type.GetGenericTypeDefinition();
                    if (genericDef == typeof(IEnumerable<>) ||
                        genericDef == typeof(ICollection<>) ||
                        genericDef == typeof(IList<>) ||
                        genericDef == typeof(List<>) ||
                        genericDef == typeof(HashSet<>) ||
                        genericDef == typeof(ISet<>))
                    {
                        result = type.GetGenericArguments()[0];
                        _elementTypeCache[type] = result;
                        return result;
                    }
                }

                // 查找 IEnumerable<T> 接口
                foreach (var iface in type.GetInterfaces())
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        result = iface.GetGenericArguments()[0];
                        _elementTypeCache[type] = result;
                        return result;
                    }
                }

                result = typeof(object);
                _elementTypeCache[type] = result;
                return result;
            }
        }

        #endregion

        #region 缓存清理

        public static void Clear()
        {
            lock (_lock)
            {
                _propertiesCache.Clear();
                _fieldsCache.Clear();
                _isValueTypeCache.Clear();
                _isEnumCache.Clear();
                _isCollectionCache.Clear();
                _isDictionaryCache.Clear();
                _elementTypeCache.Clear();
            }
        }

        public static int GetCacheSize()
        {
            lock (_lock)
            {
                return _propertiesCache.Count + _fieldsCache.Count + _isValueTypeCache.Count +
                       _isEnumCache.Count + _isCollectionCache.Count + _isDictionaryCache.Count +
                       _elementTypeCache.Count;
            }
        }

        #endregion

        // 用于缓存键的辅助结构
        private struct TypeKey : IEquatable<TypeKey>
        {
            public Type Type;
            public string Key;

            public TypeKey(Type type, string key)
            {
                Type = type;
                Key = key;
            }

            public bool Equals(TypeKey other) => Type == other.Type && Key == other.Key;
            public override int GetHashCode() => Type.GetHashCode() ^ Key.GetHashCode();
        }
    }
}
