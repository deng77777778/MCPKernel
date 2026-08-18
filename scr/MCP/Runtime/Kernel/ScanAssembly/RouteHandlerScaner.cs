using MCP.DependencyInjection;
using MCP.Kernel.Attributes;
using MCP.Kernel.Extensions;
using MCP.Kernel.ScanAssembly;
using System;
using UnityEngine;

namespace MCP.Kernel.RouteHandler
{
    public sealed class RouteHandlerScaner : IScanType
    {
        private readonly HttpMethodRegistry registry;
        public RouteHandlerScaner()
        {
            registry = ServiceContainer.GetService<HttpMethodRegistry>();
        }

        public bool AllowScan(Type type)
        {
            return type.IsImplementInterface<IRouteHandler>() && type.HasAttribute<HttpRouteAttribute>();
        }

        public void ScanType(Type type)
        {
            var attribute = type.GetAttribute<HttpRouteAttribute>();
            try
            {
                var handler = (IRouteHandler)System.Activator.CreateInstance(type);
                var registryResult = registry.Resolve(attribute.Method);
                if (registryResult.Result)
                {
                    registryResult.Value.Register(attribute.Path, handler);
                }
            }
            catch(System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
