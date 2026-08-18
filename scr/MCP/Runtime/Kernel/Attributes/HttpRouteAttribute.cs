using System;

namespace MCP.Kernel.Attributes
{
    /// <summary>
    /// Http路由特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class HttpRouteAttribute : Attribute
    {
        public HttpMethod Method { get; }
        public string Path { get; }

        public HttpRouteAttribute(HttpMethod method, string path)
        {
            Method = method;
            Path = path;
        }
    }
}

