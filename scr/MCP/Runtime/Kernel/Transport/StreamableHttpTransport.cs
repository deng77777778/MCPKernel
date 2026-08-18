using MCP.DependencyInjection.Extensions;
using MCP.Kernel.Extensions;
using MCP.Protocol;
using MCP.Result;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MCP.Kernel.Transport
{
    /// <summary>
    /// 基于Streamable HTTP的传输层，适用于Unity环境
    /// 支持Server-Sent Events和Chunked Transfer
    /// </summary>
    public class StreamableHttpTransport : IHttpTransport
    {
        private HttpListener _listener;
        private CancellationTokenSource _cts;
        private readonly DependencyInjection.IServiceProvider provider;
        public int Port { get; private set; }

        public StreamableHttpTransport(DependencyInjection.IServiceProvider provider)
        {
            // 设置Unity兼容的HTTP监听器
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            this.provider = provider;
        }

        public async Task StartAsync(int port)
        {
            Port = port;
            _cts = new CancellationTokenSource();

            // 在Unity中，我们使用线程池运行HTTP服务器
            await Task.Run(async () =>
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://+:{port}/");
                _listener.Prefixes.Add($"http://localhost:{port}/");
                _listener.Start();

                UnityEngine.Debug.Log($"[MCP Server] Started on http://localhost:{port}");

                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var context = await _listener.GetContextAsync();
                        _ = ProcessRequestAsync(context);
                    }
                    catch (HttpListenerException) when (_cts.Token.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[MCP Server] Error: {ex.Message}");
                    }
                }
            }, _cts.Token);
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            try
            {
                // 设置CORS头
                response.AddHeader("Access-Control-Allow-Origin", "*");
                response.AddHeader("Access-Control-Allow-Methods", "GET, POST, DELETE, OPTIONS");
                response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, Mcp-Session-Id");
                response.AddHeader("Access-Control-Expose-Headers", "Mcp-Session-Id");

                var r = await request.ToRequestAsync();
                //UnityEngine.Debug.Log(request.HttpMethod + " - " + r.Body);
                var message = JsonConvert.DeserializeObject<JsonRpcRequest>(r.Body);
                var handlerResult = GetRouteHandler(r, message);
                if (handlerResult.Result)
                {
                    var handler = handlerResult.Value;
                    var mcpResponse = await handler.Handle(r, message);
                    await response.ApplyResponseAsync(mcpResponse);
                }
            }
            catch (Exception ex)
            {
                var mcpResponse = MCPResponse.InternalError(ex.Message);
                await response.ApplyResponseAsync(mcpResponse);
            }
            finally
            {
                if (!response.SendChunked)
                    response?.Close();
            }
        }

        private IResult<IRouteHandler> GetRouteHandler(MCPRequest request, JsonRpcRequest message)
        {
            var registry = provider.GetService<HttpMethodRegistry>();
            var methodResult = registry.Resolve(request.Method);

            if (methodResult.Result)
            {
                var handlerRegistry = methodResult.Value;
                var handlerResult = handlerRegistry.Resolve(message is null ? string.Empty : message.Method);
                if (handlerResult.Result)
                {
                    return Results.Ok(handlerResult.Value);
                }
            }

            return Results.Bad<IRouteHandler>();
        }


        public Task StopAsync()
        {
            _cts?.Cancel();
            _listener?.Stop();
            _listener?.Close();
            return Task.CompletedTask;
        }
    }
}