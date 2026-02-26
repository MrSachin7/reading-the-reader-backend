using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace ReadingTheReader.RealtimeMessenger;

public sealed class WebSocketConnectionManager {

    private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();

    public string Add(WebSocket socket) {
        var id = Guid.NewGuid().ToString("N");
        _sockets[id] = socket;
        return id;
    }

    public bool Remove(string id) => _sockets.TryRemove(id, out _);

    public IEnumerable<WebSocket> All => _sockets.Values;

    public int Count => _sockets.Count;
}
