using Server.Net;

namespace Server.Game;

public interface IPlayerService
{
    IEnumerable<Player> Players { get; }
    bool IsConnected(int playerId);
    void OnAdd(int playerId, INetworkChannel channel);
    bool RemovePlayer(int playerId);
}