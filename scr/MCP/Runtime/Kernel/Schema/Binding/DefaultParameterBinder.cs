// Binding/DefaultParameterBinder.cs
#nullable enable
using MCP.AI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;
using System.Threading;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// 默认参数绑定器（兜底）
    /// </summary>
    public class DefaultParameterBinder : IParameterBinder
    {
        private readonly JsonSerializerSettings? _settings;

        public DefaultParameterBinder(JsonSerializerSettings? settings = null)
        {
            _settings = settings ?? AIJsonUtilities.DefaultSettings;
        }

        public int Priority => 10;

        public bool CanBind(ParameterInfo parameter) => true;

        public object? Bind(ParameterInfo parameter, AIFunctionArguments arguments, CancellationToken cancellationToken)
        {
            var name = NameHelper.GetParameterName(parameter);

            if (arguments.TryGetValue(name, out var value))
            {
                return ConvertValue(value, parameter.ParameterType);
            }

            if (DefaultValueHelper.TryGetValue(parameter, out var defaultValue))
            {
                return defaultValue;
            }

            throw new ArgumentException(
                $"The arguments dictionary is missing a value for the required parameter '{name}'.");
        }

        private object? ConvertValue(object? value, Type targetType)
        {
            if (value == null || targetType.IsInstanceOfType(value))
                return value;

            try
            {
                if (value is JToken token)
                    return token.ToObject(targetType, JsonSerializer.Create(_settings));

                if (value is string str)
                    return ConvertString(str, targetType);

                return ConvertViaJson(value, targetType);
            }
            catch
            {
                return value;
            }
        }

        private object? ConvertString(string str, Type targetType)
        {
            if (targetType == typeof(string))
                return str;

            if (AIJsonUtilities.IsPotentiallyJson(str))
            {
                try
                {
                    return JsonConvert.DeserializeObject(str, targetType, _settings);
                }
                catch (JsonException) { }
            }

            try
            {
                return Convert.ChangeType(str, targetType);
            }
            catch
            {
                return JsonConvert.DeserializeObject($"\"{str}\"", targetType, _settings);
            }
        }

        private object? ConvertViaJson(object value, Type targetType)
        {
            var json = JsonConvert.SerializeObject(value, _settings);
            return JsonConvert.DeserializeObject(json, targetType, _settings);
        }
    }
}