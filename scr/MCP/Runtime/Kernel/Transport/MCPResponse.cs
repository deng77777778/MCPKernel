using MCP.Kernel.Transport.Body;
using MCP.Protocol;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;

namespace MCP.Kernel.Transport
{
    public sealed class MCPResponse
    {
        public int StatusCode { get; set; } = (int)HttpStatusCode.OK;
        public string ContentType { get; set; } = "application/json";
        public Dictionary<string, string> Headers { get; set; } = new();
        public IResponseBody Body { get; set; }

        #region Static Helpers

        public static MCPResponse Ok(string body = null)
            => new()
            {
                Body = new StringBody(body ?? string.Empty)
            };

        public static MCPResponse Ok(byte[] rawBody, string contentType = "application/octet-stream")
            => new()
            {
                ContentType = contentType,
                Body = new BytesBody(rawBody)
            };

        public static MCPResponse Json(object data, HttpStatusCode statusCode = HttpStatusCode.OK)
            => new()
            {
                StatusCode = (int)statusCode,
                ContentType = "application/json",
                Body = new StringBody(JsonConvert.SerializeObject(data, McpJsonUtilities.DefaultSettings))
            };

        public static MCPResponse Streaming(
            IEnumerable<string> events,
            string contentType = "text/event-stream")
            => new()
            {
                ContentType = contentType,
                Body = new StreamingBody(new ServerSentEventsWriter(events))
            };

        public static MCPResponse Streaming(IStreamWriterAsync writer)
            => new()
            {
                Body = new StreamingBody(writer)
            };

        public static MCPResponse StatusCodeResponse(HttpStatusCode statusCode, string error = null)
            => new()
            {
                StatusCode = (int)statusCode,
                Body = error is null
                    ? EmptyBody.Instance
                    : new StringBody($"{{\"error\":\"{error}\"}}")
            };

        public static MCPResponse BadRequest(string error = "Bad Request")
            => StatusCodeResponse(HttpStatusCode.BadRequest, error);

        public static MCPResponse NotFound(string error = "Not Found")
            => StatusCodeResponse(HttpStatusCode.NotFound, error);

        public static MCPResponse MethodNotAllowed()
            => StatusCodeResponse(HttpStatusCode.MethodNotAllowed, "Method Not Allowed");

        public static MCPResponse InternalError(string error = "Internal Server Error")
            => StatusCodeResponse(HttpStatusCode.InternalServerError, error);

        #endregion
    }
}

