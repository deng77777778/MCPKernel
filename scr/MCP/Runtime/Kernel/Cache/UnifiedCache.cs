#nullable enable
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace MCP.Kernel.Cache
{
    /// <summary>
    /// 统一缓存实现
    /// </summary>
    public sealed class UnifiedCache : IUnifiedCache
    {
        #region 单例

        private static readonly Lazy<UnifiedCache> _instance = new(() => new UnifiedCache());
        public static UnifiedCache Instance => _instance.Value;

        #endregion

        #region 缓存字典

        private readonly ConcurrentDictionary<Type, JObject> _schemaCache = new();
        private readonly ConcurrentDictionary<MethodInfo, JObject> _functionSchemaCache = new();
        private readonly ConcurrentDictionary<ParameterInfo, string> _parameterNameCache = new();
        private readonly ConcurrentDictionary<MethodInfo, bool> _asyncMethodCache = new();
        private readonly ConcurrentDictionary<Type, bool> _aicontentRelatedCache = new();
        private readonly ConcurrentDictionary<Type, Type?> _unwrappedReturnTypeCache = new();
        private readonly ConcurrentDictionary<Type, bool> _taskTypeCache = new();
        private readonly ConcurrentDictionary<Type, bool> _valueTaskTypeCache = new();

        private int _maxCacheSize = 1024;
        private long _cacheHits;
        private long _cacheMisses;

        #endregion

        #region 属性

        public int MaxCacheSize
        {
            get => _maxCacheSize;
            set => _maxCacheSize = Math.Max(1, value);
        }

        #endregion

        #region IUnifiedCache 实现

        public bool TryGetSchema(Type type, out JObject schema)
        {
            if (_schemaCache.TryGetValue(type, out schema))
            {
                Interlocked.Increment(ref _cacheHits);
                return true;
            }
            Interlocked.Increment(ref _cacheMisses);
            return false;
        }

        public void AddSchema(Type type, JObject schema)
        {
            if (_schemaCache.Count >= _maxCacheSize)
            {
                TrimCache(_schemaCache);
            }
            _schemaCache[type] = schema;
        }

        public void RemoveSchema(Type type)
        {
            _schemaCache.TryRemove(type, out _);
        }

        public void ClearSchemas()
        {
            _schemaCache.Clear();
        }

        public bool TryGetFunctionSchema(MethodInfo method, out JObject schema)
        {
            if (_functionSchemaCache.TryGetValue(method, out schema))
            {
                Interlocked.Increment(ref _cacheHits);
                return true;
            }
            Interlocked.Increment(ref _cacheMisses);
            return false;
        }

        public void AddFunctionSchema(MethodInfo method, JObject schema)
        {
            if (_functionSchemaCache.Count >= _maxCacheSize)
            {
                TrimCache(_functionSchemaCache);
            }
            _functionSchemaCache[method] = schema;
        }

        public void RemoveFunctionSchema(MethodInfo method)
        {
            _functionSchemaCache.TryRemove(method, out _);
        }

        public void ClearFunctionSchemas()
        {
            _functionSchemaCache.Clear();
        }

        public bool TryGetParameterName(ParameterInfo parameter, out string name)
        {
            if (_parameterNameCache.TryGetValue(parameter, out name))
            {
                Interlocked.Increment(ref _cacheHits);
                return true;
            }
            Interlocked.Increment(ref _cacheMisses);
            return false;
        }

        public void AddParameterName(ParameterInfo parameter, string name)
        {
            if (_parameterNameCache.Count >= _maxCacheSize)
            {
                TrimCache(_parameterNameCache);
            }
            _parameterNameCache[parameter] = name;
        }

        public bool TryGetAsyncMethod(MethodInfo method, out bool isAsync)
        {
            if (_asyncMethodCache.TryGetValue(method, out isAsync))
            {
                Interlocked.Increment(ref _cacheHits);
                return true;
            }
            Interlocked.Increment(ref _cacheMisses);
            return false;
        }

        public void AddAsyncMethod(MethodInfo method, bool isAsync)
        {
            if (_asyncMethodCache.Count >= _maxCacheSize)
            {
                TrimCache(_asyncMethodCache);
            }
            _asyncMethodCache[method] = isAsync;
        }

        public bool TryGetAIContentRelated(Type type, out bool isRelated)
        {
            if (_aicontentRelatedCache.TryGetValue(type, out isRelated))
            {
                Interlocked.Increment(ref _cacheHits);
                return true;
            }
            Interlocked.Increment(ref _cacheMisses);
            return false;
        }

        public void AddAIContentRelated(Type type, bool isRelated)
        {
            if (_aicontentRelatedCache.Count >= _maxCacheSize)
            {
                TrimCache(_aicontentRelatedCache);
            }
            _aicontentRelatedCache[type] = isRelated;
        }

        public void ClearAll()
        {
            _schemaCache.Clear();
            _functionSchemaCache.Clear();
            _parameterNameCache.Clear();
            _asyncMethodCache.Clear();
            _aicontentRelatedCache.Clear();
            _unwrappedReturnTypeCache.Clear();
            _taskTypeCache.Clear();
            _valueTaskTypeCache.Clear();
            _cacheHits = 0;
            _cacheMisses = 0;
        }

        public CacheStatistics GetStatistics()
        {
            var total = _schemaCache.Count + _functionSchemaCache.Count + _parameterNameCache.Count +
                        _asyncMethodCache.Count + _aicontentRelatedCache.Count +
                        _unwrappedReturnTypeCache.Count + _taskTypeCache.Count + _valueTaskTypeCache.Count;

            // 估算内存使用
            var memory = _schemaCache.Count * 1024L + // 每个 Schema 约 1KB
                         _functionSchemaCache.Count * 512L +
                         _parameterNameCache.Count * 64L +
                         _asyncMethodCache.Count * 32L +
                         _aicontentRelatedCache.Count * 32L;

            return new CacheStatistics
            {
                SchemaCacheCount = _schemaCache.Count,
                FunctionSchemaCacheCount = _functionSchemaCache.Count,
                ParameterNameCacheCount = _parameterNameCache.Count,
                AsyncMethodCacheCount = _asyncMethodCache.Count,
                AIContentRelatedCacheCount = _aicontentRelatedCache.Count,
                TotalCacheCount = total,
                CacheMemoryUsage = memory,
                CacheHits = Interlocked.Read(ref _cacheHits),
                CacheMisses = Interlocked.Read(ref _cacheMisses)
            };
        }

        #endregion

        #region 额外的类型缓存方法

        public bool TryGetUnwrappedReturnType(Type type, out Type? unwrappedType)
        {
            return _unwrappedReturnTypeCache.TryGetValue(type, out unwrappedType);
        }

        public void AddUnwrappedReturnType(Type type, Type? unwrappedType)
        {
            if (_unwrappedReturnTypeCache.Count >= _maxCacheSize)
            {
                TrimCache(_unwrappedReturnTypeCache);
            }
            _unwrappedReturnTypeCache[type] = unwrappedType;
        }

        public bool TryGetTaskType(Type type, out bool isTask)
        {
            return _taskTypeCache.TryGetValue(type, out isTask);
        }

        public void AddTaskType(Type type, bool isTask)
        {
            if (_taskTypeCache.Count >= _maxCacheSize)
            {
                TrimCache(_taskTypeCache);
            }
            _taskTypeCache[type] = isTask;
        }

        public bool TryGetValueTaskType(Type type, out bool isValueTask)
        {
            return _valueTaskTypeCache.TryGetValue(type, out isValueTask);
        }

        public void AddValueTaskType(Type type, bool isValueTask)
        {
            if (_valueTaskTypeCache.Count >= _maxCacheSize)
            {
                TrimCache(_valueTaskTypeCache);
            }
            _valueTaskTypeCache[type] = isValueTask;
        }

        #endregion

        #region 缓存管理

        private void TrimCache<TKey, TValue>(ConcurrentDictionary<TKey, TValue> cache) where TKey : notnull
        {
            if (cache.Count < _maxCacheSize) return;

            // 移除一半的条目
            var toRemove = cache.Count / 2;
            var keys = cache.Keys.ToList();
            for (int i = 0; i < Math.Min(toRemove, keys.Count); i++)
            {
                cache.TryRemove(keys[i], out _);
            }
        }

        #endregion

        #region 缓存性能统计

        public void ResetHitCounters()
        {
            Interlocked.Exchange(ref _cacheHits, 0);
            Interlocked.Exchange(ref _cacheMisses, 0);
        }

        #endregion
    }
}
