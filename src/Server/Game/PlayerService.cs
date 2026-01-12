using Server.Net;

namespace Server.Game;

public sealed class PlayerService : IPlayerService
{
    public static PlayerService Instance { get; } = new();

    private readonly object _sync = new();

    private readonly LinkedList<int> _playerIds = [];

    private readonly LinkedList<Player> _players = [];

    // Return snapshots to avoid concurrent-modification exceptions during enumeration.
    public IEnumerable<Player> Players
    {
        get
        {
            lock (_sync)
            {
                return _players.ToArray();
            }
        }
    }

    public IEnumerable<int> PlayerIds
    {
        get
        {
            lock (_sync)
            {
                return _playerIds.ToArray();
            }
        }
    }

    public bool IsConnected(int playerId)
    {
        lock (_sync)
        {
            return _players.Any(x => x.Id == playerId);
        }
    }

    public void OnAdd(int playerId, INetworkChannel channel)
    {
        lock (_sync)
        {
            _playerIds.AddLast(playerId);
            _players.AddLast(new Player(playerId, channel));
        }
    }

    public bool RemovePlayer(int playerId)
    {
        lock (_sync)
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
    }

    public bool Disconnect(int playerId)
    {
        lock (_sync)
        {
            var player = _players.FirstOrDefault(x => x.Id == playerId);
            if (player is null)
            {
                return false;
            }

            player.Disconnect();
            return true;
        }
    }

    public void SendDataToAll(byte[] bytes)
    {
        Player[] players;
        lock (_sync)
        {
            players = _players.ToArray();
        }

        foreach (var player in players)
        {
            player.Send(bytes);
        }
    }

    public void SendDataTo(int playerId, byte[] bytes)
    {
        Player player;
        lock (_sync)
        {
            player = _players.FirstOrDefault(x => x.Id == playerId);
        }

        player?.Send(bytes);
    }

    public string ClientIp(int playerId)
    {
        lock (_sync)
        {
            var player = _players.FirstOrDefault(x => x.Id == playerId);
            return player is not null ? player.IpAddress : string.Empty;
        }
    }
}