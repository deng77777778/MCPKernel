#nullable enable
using MCP.Kernel.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace MCP.AI
{
    /// <summary>
    /// JSON 工具类 - 提供 JSON Schema 生成和序列化功能
    /// </summary>
    public static partial class AIJsonUtilities
    {
        #region 默认设置

        private static JsonSerializerSettings? _defaultSettings;
        private static JObject? _defaultJsonSchema;

        /// <summary>
        /// 获取默认的 JSON 序列化设置
        /// </summary>
        public static JsonSerializerSettings DefaultSettings
        {
            get
            {
                if (_defaultSettings == null)
                {
                    _defaultSettings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        ContractResolver = new DefaultContractResolver
                        {
                            NamingStrategy = new CamelCaseNamingStrategy()
                        },
                        Converters = new List<JsonConverter> { new StringEnumConverter() },
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects
                    };
                }
                return _defaultSettings;
            }
        }

        /// <summary>
        /// 获取默认的空 JSON Schema
        /// </summary>
        public static JObject DefaultJsonSchema =>
            _defaultJsonSchema ??= new JObject { ["type"] = "object" };

        #endregion

        #region 主要 API - 创建函数 JSON Schema

        /// <summary>
        /// 创建函数的 JSON Schema
        /// </summary>
        public static JObject CreateFunctionJsonSchema(
            MethodInfo method,
            string? title = null,
            string? description = null,
            JsonSerializerSettings? settings = null,
            AIJsonSchemaCreateOptions? options = null)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            settings ??= DefaultSettings;
            options ??= AIJsonSchemaCreateOptions.Default;

            // 转换选项为 Kernel Schema 选项
            var kernelOptions = ConvertToKernelOptions(options);

            // 修复：使用 Type 而不是 WithType
            var builder = new SchemaBuilder()
                .Type("object")
                .Title(title)
                .Description(description);

            // 添加 $schema 关键字
            if (options.IncludeSchemaKeyword)
            {
                // SchemaBuilder 没有直接设置 $schema 的方法，通过 JObject 添加
            }

            var parameters = method.GetParameters();

            foreach (var param in parameters)
            {
                // 跳过特殊参数
                if (param.ParameterType == typeof(CancellationToken) ||
                    param.ParameterType == typeof(AIFunctionArguments) ||
                    param.ParameterType == typeof(IServiceProvider))
                    continue;

                // 检查是否被排除
                if (options.IncludeParameter != null && !options.IncludeParameter(param))
                    continue;

                var paramName = GetParameterSchemaName(param);
                var paramSchema = MCP.Kernel.Schema.SchemaHelpers.CreateJsonSchema(param.ParameterType, kernelOptions);

                // 添加描述
                var desc = options.ParameterDescriptionProvider?.Invoke(param) ??
                           param.GetCustomAttribute<DescriptionAttribute>(true)?.Description;
                if (!string.IsNullOrEmpty(desc))
                    paramSchema["description"] = desc;

                // 添加默认值
                if (TryGetEffectiveDefaultValue(param, out var defaultValue) && defaultValue != null)
                {
                    try
                    {
                        paramSchema["default"] = JToken.FromObject(defaultValue, JsonSerializer.Create(settings));
                    }
                    catch
                    {
                        // 忽略序列化错误
                    }
                }

                // 判断是否必需 - 修复：使用 Property 而不是 AddProperty
                var isRequired = !param.IsOptional && !TryGetEffectiveDefaultValue(param, out _);
                builder.Property(paramName, paramSchema, isRequired);
            }

            var result = builder.Build();

            // 添加 $schema 关键字（如果有）
            if (options.IncludeSchemaKeyword)
            {
                result["$schema"] = "https://json-schema.org/draft/2020-12/schema";
            }

            // 应用转换
            if (options.TransformOptions != null)
            {
                try
                {
                    result = MCP.Kernel.Schema.SchemaHelpers.TransformSchema(result, options.TransformOptions) as JObject ?? result;
                }
                catch
                {
                    // 转换失败时返回原始 schema
                }
            }

            return result;
        }

        #endregion

        #region 主要 API - 创建类型 JSON Schema

        /// <summary>
        /// 创建类型的 JSON Schema
        /// </summary>
        public static JObject CreateJsonSchema(
            Type type,
            JsonSerializerSettings? settings = null,
            AIJsonSchemaCreateOptions? options = null)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            settings ??= DefaultSettings;
            options ??= AIJsonSchemaCreateOptions.Default;

            var kernelOptions = ConvertToKernelOptions(options);
            return MCP.Kernel.Schema.SchemaHelpers.CreateJsonSchema(type, kernelOptions);
        }

        #endregion

        #region 选项转换

        /// <summary>
        /// 将 AIJsonSchemaCreateOptions 转换为 Kernel Schema 选项
        /// </summary>
        private static AIJsonSchemaCreateOptions ConvertToKernelOptions(AIJsonSchemaCreateOptions options)
        {
            if (options == null)
                return AIJsonSchemaCreateOptions.Default;

            var kernelOptions = new AIJsonSchemaCreateOptions
            {
                IncludeSchemaKeyword = options.IncludeSchemaKeyword,
                EnableCaching = true,
                DetectCircularReferences = true
            };

            if (options.IncludeParameter != null)
                kernelOptions.IncludeParameter = options.IncludeParameter;

            if (options.ParameterDescriptionProvider != null)
                kernelOptions.ParameterDescriptionProvider = options.ParameterDescriptionProvider;

            if (options.TransformSchemaNode != null)
                kernelOptions.TransformSchemaNode = options.TransformSchemaNode;

            if (options.TransformOptions != null)
                kernelOptions.TransformOptions = options.TransformOptions;

            return kernelOptions;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 检查字符串是否可能是 JSON
        /// </summary>
        public static bool IsPotentiallyJson(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            var trimmed = value.TrimStart();
            if (trimmed.Length == 0) return false;
            var first = trimmed[0];
            return first == '{' || first == '[' || first == '"' || first == '-' ||
                   char.IsDigit(first) ||
                   trimmed.StartsWith("null", StringComparison.OrdinalIgnoreCase) ||
                   trimmed.StartsWith("true", StringComparison.OrdinalIgnoreCase) ||
                   trimmed.StartsWith("false", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 清理成员名称
        /// </summary>
        public static string SanitizeMemberName(string memberName)
        {
            var match = Regex.Match(memberName, @"^<([^>]+)>\w__(.+)");
            if (match.Success)
            {
                memberName = $"{match.Groups[1].Value}_{match.Groups[2].Value}";
            }
            return Regex.Replace(memberName, "[^0-9A-Za-z]+", "_");
        }

        /// <summary>
        /// 获取参数 Schema 名称
        /// </summary>
        public static string GetParameterSchemaName(ParameterInfo parameter)
        {
            return parameter.GetCustomAttribute<AIParameterNameAttribute>(true)?.Name ??
                   MCP.Kernel.Schema.NameHelper.GetParameterName(parameter);
        }

        /// <summary>
        /// 尝试获取有效默认值
        /// </summary>
        public static bool TryGetEffectiveDefaultValue(ParameterInfo parameterInfo, out object? defaultValue)
        {
            return MCP.Kernel.Schema.DefaultValueHelper.TryGetValue(parameterInfo, out defaultValue);
        }

        /// <summary>
        /// 清空 Schema 缓存
        /// </summary>
        public static void ClearSchemaCache()
        {
            MCP.Kernel.Schema.SchemaHelpers.ClearAllCache();
        }

        #endregion

        #region Transform Schema 方法

        /// <summary>
        /// 转换 JSON Schema
        /// </summary>
        public static JToken TransformSchema(JToken schema, AIJsonSchemaTransformOptions transformOptions)
        {
            return MCP.Kernel.Schema.SchemaHelpers.TransformSchema(schema, transformOptions);
        }

        #endregion
    }
}