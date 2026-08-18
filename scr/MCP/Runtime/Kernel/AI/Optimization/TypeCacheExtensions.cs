#nullable enable
using System;
using System.Reflection;

namespace MCP.AI
{
    /// <summary>
    /// 扩展方法 - 使用缓存
    /// </summary>
    public static class TypeCacheExtensions
    {
        public static bool IsAIContentRelatedFast(this Type type) => TypeCache.IsAIContentRelated(type);
        public static bool IsPrimitiveFast(this Type type) => TypeCache.IsPrimitive(type);
        public static bool IsValueTypeFast(this Type type) => TypeCache.IsValueType(type);
        public static bool IsTaskFast(this Type type) => TypeCache.IsTask(type);
        public static bool IsValueTaskFast(this Type type) => TypeCache.IsValueTask(type);
        public static Type? GetNullableUnderlyingFast(this Type type) => TypeCache.GetNullableUnderlying(type);
        public static Type? GetEnumerableElementTypeFast(this Type type) => TypeCache.GetEnumerableElementType(type);
        public static PropertyInfo[] GetCachedProperties(this Type type, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
            => TypeCache.GetCachedProperties(type, flags);
        public static MethodInfo[] GetCachedMethods(this Type type, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            => TypeCache.GetCachedMethods(type, flags);
    }
}