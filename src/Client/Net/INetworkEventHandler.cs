namespace Client.Net;

public interface INetworkEventHandler
{
    Task OnBytesReceivedAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken);

    /// <summary>
    /// Called when the network connection is lost or closed.
    /// Implementations may update UI state or perform cleanup.
    /// </summary>
    Task OnDisconnectedAsync(CancellationToken cancellationToken);
}