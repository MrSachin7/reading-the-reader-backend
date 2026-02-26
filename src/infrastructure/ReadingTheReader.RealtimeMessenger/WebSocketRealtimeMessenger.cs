using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ReadingTheReader.core.Application.ApplicationContracts.Realtime;

namespace ReadingTheReader.RealtimeMessenger;

public sealed class WebSocketRealtimeMessenger : IRealtimeMessenger {

    private readonly WebSocketConnectionManager _connections;
    private readonly JsonSerializerOptions _jsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public WebSocketRealtimeMessenger(WebSocketConnectionManager connections) {
        _connections = connections;
    }

    public int ConnectedClients => _connections.Count;

    public async ValueTask SendAsync<T>(string messageType, T payload, CancellationToken ct = default) {
        var envelope = new RealtimeMessageEnvelope<T>(
            messageType,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            payload
        );

        var json = JsonSerializer.Serialize(envelope, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var buffer = new ArraySegment<byte>(bytes);

        foreach (var socket in _connections.All) {
            if (socket.State != WebSocketState.Open) {
                continue;
            }

            try {
                await socket.SendAsync(buffer, WebSocketMessageType.Text, true, ct);
            } catch (OperationCanceledException) {
                throw;
            } catch {
                // Ignore send failures for disconnected sockets.
            }
        }
    }
}
