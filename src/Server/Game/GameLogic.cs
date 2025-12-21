using Core.Globals;
using Server.Game;
using static Core.Globals.Commands;

namespace Server;

public static class GameLogic
{
    public static int GetTotalMapPlayers(int map)
    {
        return PlayerService.Instance.PlayerIds.Count(i => GetPlayerMap(i) == map);
    }

    public static int GetNpcMaxVital(double npcNum, Vital vital)
    {
        if (npcNum < 0 || npcNum > Variables.MaxNpcs)
        {
            return 0;
        }

        return vital switch
        {
            Core.Globals.Vital.Health => Data.Npc[(int)npcNum].Hp,
            Core.Globals.Vital.Stamina => Data.Npc[(int)npcNum].Stat[(byte)Stat.Intelligence] * 2,
            _ => 0
        };
    }

    public static int FindPlayer(string name)
    {
        foreach (var i in PlayerService.Instance.PlayerIds)
        {
            if (GetPlayerName(i).ToUpperInvariant() == name.ToUpperInvariant())
            {
                return i;
            }
        }

        return -1;
    }

    public static string CheckGrammar(string word, byte caps = 0)
    {
        const string vowels = "aeiou";
        
        var firstLetter = word[..1].ToLowerInvariant();
        if (firstLetter == "$")
        {
            return word[1..];
        }
        
        if (vowels.Contains(firstLetter))
        {
            return (caps != 0 ? "An " : "an ") + word;
        }

        return (caps != 0 ? "A " : "a ") + word;
    }
}