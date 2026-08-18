// Helpers/NameHelper.cs
#nullable enable
using MCP.AI;
using MCP.Kernel.Cache;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// 名称辅助类
    /// </summary>
    public static class NameHelper
    {
        private static readonly UnifiedCache _cache = UnifiedCache.Instance;
        private const string AsyncSuffix = "Async";

        public static string GetFunctionName(MethodInfo method)
        {
            if (method == null) return string.Empty;

            var name = Sanitize(method.Name);
            if (TypeHelper.IsAsyncMethod(method) && name.EndsWith(AsyncSuffix, StringComparison.Ordinal))
            {
                name = name.Substring(0, name.Length - AsyncSuffix.Length);
            }
            return name;
        }

        public static string GetFunctionDescription(MethodInfo method)
        {
            return method?.GetCustomAttribute<DescriptionAttribute>(true)?.Description ?? string.Empty;
        }

        public static string GetParameterName(ParameterInfo parameter)
        {
            if (parameter == null) return string.Empty;

            if (_cache.TryGetParameterName(parameter, out var name))
                return name;

            name = parameter.GetCustomAttribute<AIParameterNameAttribute>(true)?.Name ??
                   ToSnakeCase(parameter.Name ?? string.Empty);

            _cache.AddParameterName(parameter, name);
            return name;
        }

        public static string Sanitize(string memberName)
        {
            if (string.IsNullOrEmpty(memberName)) return memberName;

            var match = System.Text.RegularExpressions.Regex.Match(memberName, @"^<([^>]+)>\w__(.+)");
            if (match.Success)
            {
                memberName = $"{match.Groups[1].Value}_{match.Groups[2].Value}";
            }
            return System.Text.RegularExpressions.Regex.Replace(memberName, "[^0-9A-Za-z]+", "_");
        }

        public static string ToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var sb = new StringBuilder(input.Length + 2);

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (char.IsUpper(c))
                {
                    if (i > 0 && input[i - 1] != '_' && !char.IsUpper(input[i - 1]))
                        sb.Append('_');
                    else if (i > 0 && char.IsUpper(input[i - 1]) &&
                             i + 1 < input.Length && char.IsLower(input[i + 1]))
                        sb.Append('_');
                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        public static string ToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var parts = input.Split('_', StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();

            foreach (var part in parts)
            {
                if (part.Length > 0)
                {
                    sb.Append(char.ToUpperInvariant(part[0]));
                    if (part.Length > 1)
                        sb.Append(part.Substring(1).ToLowerInvariant());
                }
            }

            return sb.ToString();
        }
    }
}