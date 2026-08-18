// SchemaHelpers.cs - 精简门面
#nullable enable
using MCP.AI;
using MCP.Kernel.Cache;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// Schema 辅助类 - 统一门面入口
    /// </summary>
    public static class SchemaHelpers
    {
        #region 公共 API

        public static JObject CreateJsonSchema(Type type, AIJsonSchemaCreateOptions? options = null)
        {
            return SchemaPipeline.GetTypeSchema(type, options);
        }

        public static JObject CreateFunctionJsonSchema(
            MethodInfo method,
            string? name = null,
            string? description = null,
            JsonSerializerSettings? settings = null,
            AIJsonSchemaCreateOptions? options = null,
            Func<ParameterInfo, bool>? isSpecialParameter = null,
            Func<ParameterInfo, bool>? isParameterRequired = null)
        {
            return SchemaPipeline.GetFunctionSchema(
                method, name, description, settings, options,
                isSpecialParameter, isParameterRequired);
        }

        public static JObject? CreateReturnJsonSchema(
            MethodInfo method,
            JsonSerializerSettings? settings = null,
            AIJsonSchemaCreateOptions? options = null,
            bool excludeResultSchema = false)
        {
            return SchemaPipeline.GetReturnSchema(method, settings, options, excludeResultSchema);
        }

        #endregion

        #region 类型判断 (委托给 TypeHelper)

        public static bool IsTaskType(Type type) => TypeHelper.IsTaskType(type);
        public static bool IsValueTaskType(Type type) => TypeHelper.IsValueTaskType(type);
        public static bool IsAsyncMethod(MethodInfo method) => TypeHelper.IsAsyncMethod(method);
        public static bool IsAIContentRelatedType(Type type) => TypeHelper.IsAIContentRelated(type);
        public static Type? GetUnwrappedReturnType(Type type) => TypeHelper.GetUnwrappedReturnType(type);
        public static bool IsSpecialParameter(ParameterInfo parameter) => TypeHelper.IsSpecialParameter(parameter);
        public static bool IsParameterRequired(ParameterInfo parameter) => TypeHelper.IsParameterRequired(parameter);

        #endregion

        #region 名称处理 (委托给 NameHelper)

        public static string GetFunctionName(MethodInfo method) => NameHelper.GetFunctionName(method);
        public static string GetFunctionDescription(MethodInfo method) => NameHelper.GetFunctionDescription(method);
        public static string GetParameterSchemaName(ParameterInfo parameter) => NameHelper.GetParameterName(parameter);
        public static string SanitizeMemberName(string memberName) => NameHelper.Sanitize(memberName);

        #endregion

        #region 默认值处理

        public static bool TryGetEffectiveDefaultValue(ParameterInfo parameterInfo, out object? defaultValue)
            => DefaultValueHelper.TryGetValue(parameterInfo, out defaultValue);

        #endregion

        #region 缓存管理

        public static void ClearAllCache() => MCP.Kernel.Cache.UnifiedCache.Instance.ClearAll();
        public static CacheStatistics GetCacheStatistics() => MCP.Kernel.Cache.UnifiedCache.Instance.GetStatistics();
        public static void ResetCacheHitCounters() => MCP.Kernel.Cache.UnifiedCache.Instance.ResetHitCounters();
        public static void SetMaxCacheSize(int size) => MCP.Kernel.Cache.UnifiedCache.Instance.MaxCacheSize = size;

        #endregion

        #region Schema 转换

        public static JToken TransformSchema(JToken schema, AIJsonSchemaTransformOptions transformOptions)
            => SchemaTransformer.Transform(schema, transformOptions);

        #endregion
    }
}