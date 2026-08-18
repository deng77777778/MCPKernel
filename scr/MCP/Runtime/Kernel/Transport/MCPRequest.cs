using System.Collections.Generic;

namespace MCP.Kernel.Transport
{
    public sealed class MCPRequest
    {
        public HttpMethod Method { get; set; }
        public string Path { get; set; }
        public IReadOnlyDictionary<string, string> Headers { get; set; } 
        public IReadOnlyDictionary<string, string> QueryParameters { get; set; } 
        public IReadOnlyDictionary<string, string> RouteParameters { get; set; } 
        public string Body { get; set; }
    }
}
