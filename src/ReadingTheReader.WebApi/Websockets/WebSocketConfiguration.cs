using System.Net.WebSockets;
using ReadingTheReader.core.Application.ApplicationContracts.Realtime;
using ReadingTheReader.RealtimeMessenger;

namespace ReadingTheReader.WebApi.Websockets;

public static class WebSocketConfiguration
{
    /// <summary>
    /// Registers WebSocket services in the DI container
    /// </summary>
    public static IServiceCollection AddWebSocketServices(this IServiceCollection services)
    {
        services.AddSingleton<WebSocketConnectionManager>();
        services.AddSingleton<IRealtimeMessenger, WebSocketRealtimeMessenger>();

        return services;
    }

    /// <summary>
    /// Configures WebSocket endpoints and middleware
    /// </summary>
    public static WebApplication ConfigureWebSockets(this WebApplication app)
    {
        app.UseWebSockets();

        app.Map("/ws", async (HttpContext context, WebSocketConnectionManager connections) => {
            if (!context.WebSockets.IsWebSocketRequest) {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            var connectionId = connections.Add(socket);
            var buffer = new byte[4 * 1024];

            try {
                while (socket.State == WebSocketState.Open && !context.RequestAborted.IsCancellationRequested) {
                    var result = await socket.ReceiveAsync(buffer, context.RequestAborted);
                    if (result.MessageType == WebSocketMessageType.Close) {
                        break;
                    }
                }
            } catch (OperationCanceledException) {
                // Request aborted.
            } finally {
                connections.Remove(connectionId);
                if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived) {
                    try {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    } catch {
                        // Ignore close failures.
                    }
                }
            }
        });

        return app;
    }
}
