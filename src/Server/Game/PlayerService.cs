using Server.Net;

namespace Server.Game;

public sealed class PlayerService : IPlayerService
{
    public static PlayerService Instance { get; } = new();

    private readonly LinkedList<int> _playerIds = [];

    private readonly LinkedList<Player> _players = [];

    public IEnumerable<Player> Players => _players;
    public IEnumerable<int> PlayerIds => _playerIds;

    public bool IsConnected(int playerId)
    {
        return _players.Any(x => x.Id == playerId);
    }

    public void AddPlayer(int playerId, INetworkChannel channel)
    {
        _playerIds.AddLast(playerId);
        _players.AddLast(new Player(playerId, channel));
    }

    public bool RemovePlayer(int playerId)
    {
        var player = _players.FirstOrDefault(x => x.Id == playerId);
        if (player is null)
        {
            return false;
        }

        _playerIds.Remove(playerId);
        _players.Remove(player);

        return true;
    }

    public void SendDataToAll(byte[] bytes)
    {
        foreach (var player in _players)
        {
            player.Send(bytes);
        }
    }

    public void SendDataTo(int playerId, byte[] bytes)
    {
        var player = _players.FirstOrDefault(x => x.Id == playerId);

        player?.Send(bytes);
    }

    public string ClientIp(int playerId)
    {
        var player = _players.FirstOrDefault(x => x.Id == playerId);

        return player is not null ? player.IpAddress : string.Empty;
    }
}