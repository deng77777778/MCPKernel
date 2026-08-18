using System;
using System.Collections.Generic;

namespace MCP.Kernel.Transport
{
    /// <summary>
    /// HTTP 头验证器
    /// </summary>
    internal static class HttpHeaderValidator
    {
        private static readonly HashSet<string> _forbiddenHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            "content-type",
            "content-length",
            "transfer-encoding",
            "connection"
        };

        public static bool IsForbidden(string headerName)
            => _forbiddenHeaders.Contains(headerName);

        public static bool IsValid(string headerName)
            => !IsForbidden(headerName);
    }
}
