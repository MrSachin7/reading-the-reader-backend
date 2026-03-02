namespace ReadingTheReader.core.Application.InfrastructureContracts;

public interface IClientBroadcaster
{
    ValueTask BroadcastAsync<T>(string messageType, T payload, CancellationToken ct = default);

    ValueTask SendToClientAsync<T>(string connectionId, string messageType, T payload, CancellationToken ct = default);

    int ConnectedClients { get; }
}
