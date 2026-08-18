#nullable enable
using MCP.Protocol;
using System;

namespace MCP.Kernel.Server
{
    /// <summary>
    /// Provides a context container that provides access to the client request parameters and resources for the request.
    /// </summary>
    /// <typeparam name="TParams">Type of the request parameters specific to each MCP operation.</typeparam>
    /// <remarks>
    /// The <see cref="RequestContext{TParams}"/> encapsulates all contextual information for handling an MCP request.
    /// This type is typically received as a parameter in handler delegates registered with IMcpServerBuilder,
    /// and can be injected as parameters into <see cref="McpServerTool"/>s.
    /// </remarks>
    public sealed class RequestContext<TParams> : MessageContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestContext{TParams}"/> class with the specified server, JSON-RPC request, and request parameters.
        /// </summary>
        /// <param name="server">The server with which this instance is associated.</param>
        /// <param name="jsonRpcRequest">The JSON-RPC request associated with this context.</param>
        /// <param name="parameters">The parameters associated with this request.</param>
        /// <exception cref="ArgumentNullException"><paramref name="server"/> or <paramref name="jsonRpcRequest"/> is <see langword="null"/>.</exception>
        public RequestContext(JsonRpcRequest jsonRpcRequest, TParams parameters)
            : base(jsonRpcRequest)
        {
            Params = parameters;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestContext{TParams}"/> class with the specified server and JSON-RPC request.
        /// </summary>
        /// <param name="server">The server with which this instance is associated.</param>
        /// <param name="jsonRpcRequest">The JSON-RPC request associated with this context.</param>
        /// <exception cref="ArgumentNullException"><paramref name="server"/> or <paramref name="jsonRpcRequest"/> is <see langword="null"/>.</exception>
        [Obsolete]
        public RequestContext(JsonRpcRequest jsonRpcRequest)
            : base(jsonRpcRequest)
        {
            Params = default!;
        }

        /// <summary>Gets or sets the parameters associated with this request.</summary>
        public TParams Params { get; set; }

        /// <summary>
        /// Gets or sets the primitive that matched the request.
        /// </summary>
        public IMcpServerPrimitive? MatchedPrimitive { get; set; }

        /// <summary>
        /// Gets the JSON-RPC request associated with this context.
        /// </summary>
        /// <remarks>
        /// This property provides access to the complete JSON-RPC request that initiated this handler invocation,
        /// including the method name, parameters, request ID, and associated transport and user information.
        /// </remarks>
        public JsonRpcRequest JsonRpcRequest
        {
            get => (JsonRpcRequest)JsonRpcMessage;
            set => JsonRpcMessage = value;
        }
    }

}
