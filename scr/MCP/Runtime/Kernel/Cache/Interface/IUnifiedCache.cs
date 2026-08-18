using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace MCP.Kernel.Cache
{
    /// <summary>
    /// 统一缓存接口
    /// </summary>
    public interface IUnifiedCache
    {
        /// <summary>
        /// 获取 Schema 缓存
        /// </summary>
        bool TryGetSchema(Type type, out JObject schema);
        void AddSchema(Type type, JObject schema);
        void RemoveSchema(Type type);
        void ClearSchemas();

        /// <summary>
        /// 获取函数 Schema 缓存
        /// </summary>
        bool TryGetFunctionSchema(MethodInfo method, out JObject schema);
        void AddFunctionSchema(MethodInfo method, JObject schema);
        void RemoveFunctionSchema(MethodInfo method);
        void ClearFunctionSchemas();

        /// <summary>
        /// 获取参数名称缓存
        /// </summary>
        bool TryGetParameterName(ParameterInfo parameter, out string name);
        void AddParameterName(ParameterInfo parameter, string name);

        /// <summary>
        /// 获取类型判断缓存
        /// </summary>
        bool TryGetAsyncMethod(MethodInfo method, out bool isAsync);
        void AddAsyncMethod(MethodInfo method, bool isAsync);

        bool TryGetAIContentRelated(Type type, out bool isRelated);
        void AddAIContentRelated(Type type, bool isRelated);

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        void ClearAll();

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        CacheStatistics GetStatistics();

        /// <summary>
        /// 获取或设置缓存大小限制
        /// </summary>
        int MaxCacheSize { get; set; }
    }

}
