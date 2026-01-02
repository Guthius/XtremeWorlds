using Server.Net;

namespace Server.Game;

public sealed class Player(int id, INetworkChannel channel)
{
    private readonly INetworkChannel _channel = channel;

    public int Id { get; } = id;
    public string IpAddress { get; } = channel.IpAddress;

    public void Send(byte[] bytes)
    {
        _channel.Send(bytes);
    }

    public void Disconnect()
    {
        _channel.Close();
    }
}