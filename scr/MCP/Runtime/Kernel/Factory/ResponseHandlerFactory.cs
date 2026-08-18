using MCP.Kernel.Transport;
using MCP.Kernel.Transport.Handler;
using System;
using System.Collections.Generic;

namespace MCP.Kernel.Factory
{
    /// <summary>
    /// 响应处理器工厂
    /// </summary>
    public static class ResponseHandlerFactory
    {
        private static readonly List<IResponseHandler> _handlers = new()
        {
            new EmptyResponseHandler(),      // 空响应优先处理
            new StreamingResponseHandler(),
            new StaticResponseHandler()
        };

        /// <summary>
        /// 注册自定义处理器（支持扩展）
        /// </summary>
        public static void RegisterHandler(IResponseHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            // 插入到开头，让自定义处理器有更高优先级
            _handlers.Insert(0, handler);
        }

        /// <summary>
        /// 获取匹配的处理器
        /// </summary>
        public static IResponseHandler GetHandler(MCPResponse response)
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response));

            foreach (var handler in _handlers)
            {
                if (handler.CanHandle(response))
                    return handler;
            }

            // 理论上不会到达这里，因为 StaticResponseHandler 会处理所有非流式响应
            throw new InvalidOperationException($"No suitable response handler found for response type: {response.Body.GetType()}");
        }
    }
}
