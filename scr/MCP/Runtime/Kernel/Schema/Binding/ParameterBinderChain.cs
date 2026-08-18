// Binding/ParameterBinderChain.cs
#nullable enable
using MCP.AI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// 参数绑定器责任链
    /// </summary>
    public class ParameterBinderChain
    {
        private readonly List<IParameterBinder> _binders = new();
        private readonly JsonSerializerSettings? _settings;

        public ParameterBinderChain(JsonSerializerSettings? settings = null)
        {
            _settings = settings ?? AIJsonUtilities.DefaultSettings;

            // 默认绑定器
            Register(new CancellationTokenBinder());
            Register(new ArgumentsBinder());
            Register(new ServiceProviderBinder());
            Register(new DefaultParameterBinder(_settings));
        }

        public void Register(IParameterBinder binder)
        {
            lock (_binders)
            {
                _binders.Add(binder);
                _binders.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            }
        }

        public void Register<T>() where T : IParameterBinder, new()
        {
            Register(new T());
        }

        public void Remove<T>() where T : IParameterBinder
        {
            lock (_binders)
            {
                _binders.RemoveAll(b => b is T);
            }
        }

        public IParameterBinder? GetBinder(ParameterInfo parameter)
        {
            lock (_binders)
            {
                return _binders.FirstOrDefault(b => b.CanBind(parameter));
            }
        }

        public object? Bind(ParameterInfo parameter, AIFunctionArguments arguments, CancellationToken cancellationToken)
        {
            var binder = GetBinder(parameter);
            return binder?.Bind(parameter, arguments, cancellationToken);
        }

        /// <summary>
        /// 创建绑定函数 - 返回 (AIFunctionArguments, CancellationToken) => object?
        /// </summary>
        public Func<AIFunctionArguments, CancellationToken, object?> CreateBinder(ParameterInfo parameter)
        {
            var binder = GetBinder(parameter) ?? new DefaultParameterBinder(_settings);
            return (args, ct) => binder.Bind(parameter, args, ct);
        }

        /// <summary>
        /// 创建绑定函数（带参数信息）
        /// </summary>
        public Func<ParameterInfo, AIFunctionArguments, CancellationToken, object?> CreateBinderWithInfo()
        {
            return (param, args, ct) =>
            {
                var binder = GetBinder(param) ?? new DefaultParameterBinder(_settings);
                return binder.Bind(param, args, ct);
            };
        }

        public IReadOnlyList<IParameterBinder> GetAllBinders()
        {
            lock (_binders)
            {
                return _binders.AsReadOnly();
            }
        }

        public void Clear()
        {
            lock (_binders)
            {
                _binders.Clear();
                Register(new CancellationTokenBinder());
                Register(new ArgumentsBinder());
                Register(new ServiceProviderBinder());
                Register(new DefaultParameterBinder(_settings));
            }
        }
    }
}