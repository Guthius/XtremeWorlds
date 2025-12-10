using Core;
using Core.Globals;
using Server.Game;
using Server.Game.Net;
using static Core.Globals.Commands;

namespace Server;

public static class NetworkConfig
{
    public static bool IsLoggedIn(int index)
    {
        if (Account.Instance == null || Account.Instance.Count <= index)
        {
            return false;
        }
        return Account.Instance[index].Login?.Length > 0;
    }

    public static bool IsPlaying(int index)
    {
        return Data.TempPlayer[index].InGame;
    }

    public static bool IsMultiLogin(int playerId, string login)
    {
        if (string.IsNullOrEmpty(login))
        {
            return false;
        }

        foreach (var otherPlayerId in PlayerService.Instance.PlayerIds)
        {
            if (otherPlayerId == playerId)
            {
                continue;
            }

            if (!Account.Instance[otherPlayerId].Login.Equals(login, StringComparison.CurrentCultureIgnoreCase) &&
                PlayerService.Instance.ClientIp(otherPlayerId) == PlayerService.Instance.ClientIp(playerId))
            {
                return true;
            }
        }

        return false;
    }

    public static void SendDataToMapBut(int excludePlayerId, int mapNum, byte[] bytes)
    {
        foreach (var playerId in PlayerService.Instance.PlayerIds)
        {
            if (!IsPlaying(playerId) || playerId == excludePlayerId)
            {
                continue;
            }

            if (GetPlayerMap(playerId) == mapNum)
            {
                PlayerService.Instance.SendDataTo(playerId, bytes);
            }
        }
    }

    public static void SendDataToMap(int mapNum, byte[] bytes)
    {
        foreach (var playerId in PlayerService.Instance.PlayerIds)
        {
            if (!IsPlaying(playerId) || GetPlayerMap(playerId) != mapNum)
            {
                continue;
            }

            PlayerService.Instance.SendDataTo(playerId, bytes);
        }
    }
}