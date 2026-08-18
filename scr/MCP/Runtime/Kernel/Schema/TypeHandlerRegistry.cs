// Handlers/TypeHandlerRegistry.cs
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace MCP.Kernel.Schema
{
    /// <summary>
    /// 类型处理器注册表
    /// </summary>
    public static class TypeHandlerRegistry
    {
        private static readonly List<ITypeSchemaHandler> _handlers = new();

        static TypeHandlerRegistry()
        {
            // 按优先级注册
            Register(new PrimitiveTypeHandler());
            Register(new NullableTypeHandler());
            Register(new EnumTypeHandler());
            Register(new CollectionTypeHandler());
            Register(new DictionaryTypeHandler());
            Register(new ObjectTypeHandler());
        }

        public static void Register(ITypeSchemaHandler handler)
        {
            lock (_handlers)
            {
                _handlers.Add(handler);
                _handlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            }
        }

        public static void Register<T>() where T : ITypeSchemaHandler, new()
        {
            Register(new T());
        }

        public static void Remove<T>() where T : ITypeSchemaHandler
        {
            lock (_handlers)
            {
                _handlers.RemoveAll(h => h is T);
            }
        }

        public static ITypeSchemaHandler GetHandler(Type type)
        {
            lock (_handlers)
            {
                return _handlers.FirstOrDefault(h => h.CanHandle(type))
                       ?? new ObjectTypeHandler();
            }
        }

        public static IReadOnlyList<ITypeSchemaHandler> GetAllHandlers()
        {
            lock (_handlers)
            {
                return _handlers.AsReadOnly();
            }
        }

        public static void Clear()
        {
            lock (_handlers)
            {
                _handlers.Clear();
                // 重新注册默认处理器
                Register(new PrimitiveTypeHandler());
                Register(new NullableTypeHandler());
                Register(new EnumTypeHandler());
                Register(new CollectionTypeHandler());
                Register(new DictionaryTypeHandler());
                Register(new ObjectTypeHandler());
            }
        }
    }
}