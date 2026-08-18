#nullable enable
using MCP.AI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// Schema 生成上下文
    /// </summary>
    public class SchemaContext
    {
        #region 属性

        public Type? CurrentType { get; private set; }
        public MethodInfo? CurrentMethod { get; private set; }
        public AIJsonSchemaCreateOptions Options { get; private set; }
        public JsonSerializerSettings? Settings { get; private set; }
        public string? CustomName { get; private set; }
        public string? CustomDescription { get; private set; }
        public Func<ParameterInfo, bool>? ParameterFilter { get; private set; }
        public Func<ParameterInfo, bool>? RequiredFilter { get; private set; }
        public bool IsReturnType { get; private set; }
        public JObject? Result { get; set; }

        // 循环引用检测
        public HashSet<Type> TypeStack { get; } = new();

        // 缓存控制
        public bool CacheEnabled { get; set; } = true;

        #endregion

        #region 构造函数

        public SchemaContext(Type type, AIJsonSchemaCreateOptions? options = null)
        {
            CurrentType = type;
            Options = options ?? AIJsonSchemaCreateOptions.Default;
        }

        public SchemaContext(MethodInfo method, AIJsonSchemaCreateOptions? options = null)
        {
            CurrentMethod = method;
            Options = options ?? AIJsonSchemaCreateOptions.Default;
        }

        #endregion

        #region Fluent API

        public SchemaContext WithSettings(JsonSerializerSettings? settings)
        {
            Settings = settings;
            return this;
        }

        public SchemaContext WithName(string? name)
        {
            CustomName = name;
            return this;
        }

        public SchemaContext WithDescription(string? description)
        {
            CustomDescription = description;
            return this;
        }

        public SchemaContext WithParameterFilter(Func<ParameterInfo, bool>? filter)
        {
            ParameterFilter = filter;
            return this;
        }

        public SchemaContext WithRequiredFilter(Func<ParameterInfo, bool>? filter)
        {
            RequiredFilter = filter;
            return this;
        }

        public SchemaContext ForReturnType()
        {
            IsReturnType = true;
            return this;
        }

        public SchemaContext WithType(Type type)
        {
            CurrentType = type;
            return this;
        }

        public SchemaContext WithMethod(MethodInfo method)
        {
            CurrentMethod = method;
            return this;
        }

        public SchemaContext WithCache(bool enabled)
        {
            CacheEnabled = enabled;
            return this;
        }

        public SchemaContext WithResult(JObject result)
        {
            Result = result;
            return this;
        }

        #endregion

        #region 辅助方法

        public T? GetResult<T>() where T : class
        {
            return Result as T;
        }

        public bool TryGetResult<T>(out T? result) where T : class
        {
            if (Result is T t)
            {
                result = t;
                return true;
            }
            result = null;
            return false;
        }

        public bool IsTypeInStack(Type type) => TypeStack.Contains(type);

        public void PushType(Type type) => TypeStack.Add(type);

        public void PopType(Type type) => TypeStack.Remove(type);

        #endregion

        #region 克隆

        public SchemaContext Clone()
        {
            return new SchemaContext(CurrentType!, Options)
            {
                CurrentMethod = CurrentMethod,
                Settings = Settings,
                CustomName = CustomName,
                CustomDescription = CustomDescription,
                ParameterFilter = ParameterFilter,
                RequiredFilter = RequiredFilter,
                IsReturnType = IsReturnType,
                Result = Result,
                CacheEnabled = CacheEnabled
            };
        }

        #endregion
    }
}
