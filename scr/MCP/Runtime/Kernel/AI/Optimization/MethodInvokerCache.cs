#nullable enable
using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace MCP.AI
{
    /// <summary>
    /// 使用表达式树编译的方法调用器，替代反射Invoke
    /// </summary>
    public static class MethodInvokerCache
    {
        private static readonly ConcurrentDictionary<MethodInfo, Func<object?, object?[], object?>> _invokers = new();
        private static readonly ConcurrentDictionary<MethodInfo, Func<object?, object?[], ValueTask<object?>>> _asyncInvokers = new();
        private static readonly ConcurrentDictionary<MethodInfo, Func<object?>> _staticInvokers = new();

        /// <summary>
        /// 获取或创建同步方法调用器
        /// </summary>
        public static Func<object?, object?[], object?> GetOrCreate(MethodInfo method)
        {
            return _invokers.GetOrAdd(method, CreateSyncInvoker);
        }

        /// <summary>
        /// 获取或创建异步方法调用器 (返回ValueTask)
        /// </summary>
        public static Func<object?, object?[], ValueTask<object?>> GetOrCreateAsync(MethodInfo method)
        {
            return _asyncInvokers.GetOrAdd(method, CreateAsyncInvoker);
        }

        private static Func<object?, object?[], object?> CreateSyncInvoker(MethodInfo method)
        {
            var targetParam = Expression.Parameter(typeof(object), "target");
            var argsParam = Expression.Parameter(typeof(object[]), "args");

            var parameters = method.GetParameters();
            var argExpressions = new Expression[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var index = Expression.Constant(i);
                var paramType = parameters[i].ParameterType;
                var argAccess = Expression.ArrayIndex(argsParam, index);
                argExpressions[i] = Expression.Convert(argAccess, paramType);
            }

            // 处理实例方法和静态方法
            var call = Expression.Call(
                method.IsStatic ? null : Expression.Convert(targetParam, method.DeclaringType!),
                method,
                argExpressions);

            if (method.ReturnType == typeof(void))
            {
                var block = Expression.Block(call, Expression.Constant(null));
                return Expression.Lambda<Func<object?, object?[], object?>>(block, targetParam, argsParam).Compile();
            }

            var convert = Expression.Convert(call, typeof(object));
            return Expression.Lambda<Func<object?, object?[], object?>>(convert, targetParam, argsParam).Compile();
        }

        private static Func<object?, object?[], ValueTask<object?>> CreateAsyncInvoker(MethodInfo method)
        {
            var targetParam = Expression.Parameter(typeof(object), "target");
            var argsParam = Expression.Parameter(typeof(object[]), "args");

            var parameters = method.GetParameters();
            var argExpressions = new Expression[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var index = Expression.Constant(i);
                var paramType = parameters[i].ParameterType;
                var argAccess = Expression.ArrayIndex(argsParam, index);
                argExpressions[i] = Expression.Convert(argAccess, paramType);
            }

            var call = Expression.Call(
                method.IsStatic ? null : Expression.Convert(targetParam, method.DeclaringType!),
                method,
                argExpressions);

            var returnType = method.ReturnType;

            // 如果返回类型已经是ValueTask<T>，直接转换
            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                var taskResultType = returnType.GetGenericArguments()[0];
                var convertToObject = Expression.Convert(call, typeof(object));
                var lambda = Expression.Lambda<Func<object?, object?[], ValueTask<object?>>>(
                    Expression.Convert(convertToObject, typeof(ValueTask<object?>)),
                    targetParam, argsParam);
                return lambda.Compile();
            }

            // 如果是Task<T>，转换为ValueTask<T>
            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var taskResultType = returnType.GetGenericArguments()[0];
                var getResultMethod = typeof(Task<>).MakeGenericType(taskResultType)
                    .GetProperty("Result")!.GetMethod!;

                var taskVar = Expression.Variable(returnType, "task");
                var awaitCall = Expression.Call(null,
                    typeof(TaskExtensions).GetMethod("ConfigureAwait")!.MakeGenericMethod(taskResultType),
                    call, Expression.Constant(true));

                var getResult = Expression.Call(
                    Expression.Convert(awaitCall, returnType),
                    getResultMethod);

                var convertResult = Expression.Convert(getResult, typeof(object));
                var result = Expression.Convert(convertResult, typeof(ValueTask<object?>));

                var lambda = Expression.Lambda<Func<object?, object?[], ValueTask<object?>>>(
                    result, targetParam, argsParam);
                return lambda.Compile();
            }

            // 普通返回类型
            var convertCall = Expression.Convert(call, typeof(object));
            var convertTask = Expression.Convert(convertCall, typeof(ValueTask<object?>));
            return Expression.Lambda<Func<object?, object?[], ValueTask<object?>>>(
                convertTask, targetParam, argsParam).Compile();
        }
    }
}