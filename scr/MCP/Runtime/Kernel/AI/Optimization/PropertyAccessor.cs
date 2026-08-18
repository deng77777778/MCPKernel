using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace MCP.AI
{
    /// <summary>
    /// 高性能属性访问器缓存
    /// </summary>
    public static class PropertyAccessor
    {
        private static readonly ConcurrentDictionary<PropertyInfo, Func<object, object>> _getters = new();
        private static readonly ConcurrentDictionary<PropertyInfo, Action<object, object>> _setters = new();
        private static readonly ConcurrentDictionary<FieldInfo, Func<object, object>> _fieldGetters = new();
        private static readonly ConcurrentDictionary<FieldInfo, Action<object, object>> _fieldSetters = new();

        /// <summary>
        /// 获取属性Getter
        /// </summary>
        public static Func<object, object> GetGetter(PropertyInfo property)
        {
            return _getters.GetOrAdd(property, CreateGetter);
        }

        /// <summary>
        /// 获取属性Setter
        /// </summary>
        public static Action<object, object> GetSetter(PropertyInfo property)
        {
            return _setters.GetOrAdd(property, CreateSetter);
        }

        /// <summary>
        /// 获取字段Getter
        /// </summary>
        public static Func<object, object> GetFieldGetter(FieldInfo field)
        {
            return _fieldGetters.GetOrAdd(field, CreateFieldGetter);
        }

        /// <summary>
        /// 获取字段Setter
        /// </summary>
        public static Action<object, object> GetFieldSetter(FieldInfo field)
        {
            return _fieldSetters.GetOrAdd(field, CreateFieldSetter);
        }

        /// <summary>
        /// 快速读取属性值
        /// </summary>
        public static T GetValue<T>(object target, PropertyInfo property)
        {
            var getter = GetGetter(property);
            return (T)getter(target);
        }

        /// <summary>
        /// 快速设置属性值
        /// </summary>
        public static void SetValue(object target, PropertyInfo property, object value)
        {
            var setter = GetSetter(property);
            setter(target, value);
        }

        private static Func<object, object> CreateGetter(PropertyInfo property)
        {
            var method = property.GetMethod;
            if (method == null)
                throw new InvalidOperationException($"Property {property.Name} has no getter");

            var dynamicMethod = new DynamicMethod(
                $"Get_{property.Name}_{Guid.NewGuid():N}",
                typeof(object),
                new[] { typeof(object) },
                property.DeclaringType ?? typeof(object),
                true);

            var il = dynamicMethod.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);

            if (!method.IsStatic)
            {
                il.Emit(OpCodes.Castclass, property.DeclaringType!);
            }

            il.Emit(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method);

            if (property.PropertyType.IsValueType)
            {
                il.Emit(OpCodes.Box, property.PropertyType);
            }

            il.Emit(OpCodes.Ret);

            return (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
        }

        private static Action<object, object> CreateSetter(PropertyInfo property)
        {
            var method = property.SetMethod;
            if (method == null)
                throw new InvalidOperationException($"Property {property.Name} has no setter");

            var dynamicMethod = new DynamicMethod(
                $"Set_{property.Name}_{Guid.NewGuid():N}",
                null,
                new[] { typeof(object), typeof(object) },
                property.DeclaringType ?? typeof(object),
                true);

            var il = dynamicMethod.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);

            if (!method.IsStatic)
            {
                il.Emit(OpCodes.Castclass, property.DeclaringType!);
            }

            il.Emit(OpCodes.Ldarg_1);

            if (property.PropertyType.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, property.PropertyType);
            }

            il.Emit(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method);
            il.Emit(OpCodes.Ret);

            return (Action<object, object>)dynamicMethod.CreateDelegate(typeof(Action<object, object>));
        }

        private static Func<object, object> CreateFieldGetter(FieldInfo field)
        {
            var dynamicMethod = new DynamicMethod(
                $"GetField_{field.Name}_{Guid.NewGuid():N}",
                typeof(object),
                new[] { typeof(object) },
                field.DeclaringType ?? typeof(object),
                true);

            var il = dynamicMethod.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);

            if (!field.IsStatic)
            {
                il.Emit(OpCodes.Castclass, field.DeclaringType!);
            }

            il.Emit(field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, field);

            if (field.FieldType.IsValueType)
            {
                il.Emit(OpCodes.Box, field.FieldType);
            }

            il.Emit(OpCodes.Ret);

            return (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
        }

        private static Action<object, object> CreateFieldSetter(FieldInfo field)
        {
            var dynamicMethod = new DynamicMethod(
                $"SetField_{field.Name}_{Guid.NewGuid():N}",
                null,
                new[] { typeof(object), typeof(object) },
                field.DeclaringType ?? typeof(object),
                true);

            var il = dynamicMethod.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);

            if (!field.IsStatic)
            {
                il.Emit(OpCodes.Castclass, field.DeclaringType!);
            }

            il.Emit(OpCodes.Ldarg_1);

            if (field.FieldType.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, field.FieldType);
            }

            il.Emit(field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);

            return (Action<object, object>)dynamicMethod.CreateDelegate(typeof(Action<object, object>));
        }
    }
}