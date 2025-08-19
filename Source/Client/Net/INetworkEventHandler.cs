namespace Client.Net;

public interface INetworkEventHandler
{
    ValueTask OnBytesReceivedAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken);
    ValueTask OnConnectionLostAsync(CancellationToken cancellationToken);
}