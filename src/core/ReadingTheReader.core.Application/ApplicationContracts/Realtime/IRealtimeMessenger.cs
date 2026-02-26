namespace ReadingTheReader.core.Application.ApplicationContracts.Realtime;

public interface IRealtimeMessenger {

    ValueTask SendAsync<T>(string messageType, T payload, CancellationToken ct = default);

    int ConnectedClients { get; }
}
