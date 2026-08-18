using MCP.Kernel.Server;
using MCP.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;

namespace MCP.Kernel.Extensions
{
    public static class TypeExtensions
    {
        public static bool IsImplementInterface<TInterface>(this Type type)
        where TInterface : class
        {
            Type interfaceType = typeof(TInterface);
            return type.IsImplementInterface(interfaceType);
        }

        public static bool IsImplementInterface(this Type type, Type interfaceType)
        {
            if (interfaceType == null || !interfaceType.IsInterface)
                return false;

            return interfaceType.IsAssignableFrom(type);
        }

        public static bool IsImplementGenericInterface(this Type type, Type genericInterfaceDefinition)
        {
            if (!genericInterfaceDefinition.IsGenericTypeDefinition)
                return false;

            return type.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == genericInterfaceDefinition);
        }

        public static bool HasAttribute<T>(this Type type, bool inherit = true)
            where T : Attribute => type.IsDefined(typeof(T), inherit);

        public static bool HasAttribute(this Type type, Type attributeType, bool inherit = true)
            => type.IsDefined(attributeType, inherit);

        public static T GetAttribute<T>(this Type type, bool inherit = false)
            where T : Attribute => (T)type.GetCustomAttribute(typeof(T), inherit);
        public static IEnumerable<T> GetAttributes<T>(this Type type)
            where T : Attribute => type.GetCustomAttributes<T>(false);
        public static Attribute GetAttribute(this Type type, Type attributeType, bool inherit = false)
            => type.GetCustomAttribute(attributeType, inherit);


        public static bool IsAugmentedWith<TRequestParams>(this Type serviceType) =>
            serviceType == typeof(RequestContext<TRequestParams>) ||
            serviceType == typeof(ClaimsPrincipal);

    }
}
