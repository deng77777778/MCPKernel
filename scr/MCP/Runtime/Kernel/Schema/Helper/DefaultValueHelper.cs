// Helpers/DefaultValueHelper.cs
#nullable enable
using MCP.Kernel.Cache;
using System.ComponentModel;
using System.Reflection;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// 默认值辅助类
    /// </summary>
    public static class DefaultValueHelper
    {
        private static readonly UnifiedCache _cache = UnifiedCache.Instance;

        public static bool TryGetValue(ParameterInfo parameterInfo, out object? defaultValue)
        {
            if (parameterInfo == null)
            {
                defaultValue = null;
                return false;
            }

            if (parameterInfo.GetCustomAttribute<DefaultValueAttribute>(true) is DefaultValueAttribute attr)
            {
                defaultValue = attr.Value;
                return true;
            }

            if (parameterInfo.HasDefaultValue)
            {
                defaultValue = parameterInfo.DefaultValue;
                return true;
            }

            defaultValue = null;
            return false;
        }

        public static object? GetValueOrDefault(ParameterInfo parameterInfo)
        {
            TryGetValue(parameterInfo, out var value);
            return value;
        }

        public static bool HasDefaultValue(ParameterInfo parameterInfo)
        {
            return parameterInfo?.GetCustomAttribute<DefaultValueAttribute>(true) != null ||
                   parameterInfo?.HasDefaultValue == true;
        }
    }
}