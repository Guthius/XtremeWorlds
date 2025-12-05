using Core.Globals;
using Server.Game;
using static Core.Globals.Command;

namespace Server;

public static class Party
{
    public static void OnClear(int partyNum)
    {
        Data.Party[partyNum].Leader = -1;
        Data.Party[partyNum].MemberCount = 0;
        Data.Party[partyNum].Member = new int[Variables.MaxPartyMembers];
    }

    public static void OnMessage(int partyNum, string msg)
    {
        for (var i = 0; i < Variables.MaxPartyMembers; i++)
        {
            if (Data.Party[partyNum].Member[i] >= 0)
            {
                NetworkSend.SendPlayerMessage(Data.Party[partyNum].Member[i], msg, (int)ColorName.BrightBlue);
            }
        }
    }

    private static void OnRemove(int index, int partyNum)
    {
        for (var i = 0; i < Variables.MaxPartyMembers; i++)
        {
            if (Data.Party[partyNum].Member[i] == index)
            {
                Data.Party[partyNum].Member[i] = -1;
                Data.TempPlayer[index].InParty = -1;
                Data.TempPlayer[index].PartyInvite = -1;
                break;
            }
        }

        OnCount(partyNum);
        NetworkSend.SendPartyUpdate(partyNum);
        NetworkSend.SendPartyUpdateTo(index);
    }

    public static void OnLeave(int index)
    {
        var partyNum = Data.TempPlayer[index].InParty;

        if (partyNum >= 0)
        {
            // find out how many members we have
            OnCount(partyNum);

            // make sure there's more than 2 people
            int i;
            if (Data.Party[partyNum].MemberCount > 2)
            {
                // check if leader
                if (Data.Party[partyNum].Leader == index)
                {
                    // set next person down as leader
                    for (i = 0; i < Variables.MaxPartyMembers; i++)
                    {
                        if (Data.Party[partyNum].Member[i] >= 0 & Data.Party[partyNum].Member[i] != index)
                        {
                            Data.Party[partyNum].Leader = Data.Party[partyNum].Member[i];
                            OnMessage(partyNum, string.Format("{0} is now the party leader.", GetPlayerName(i)));
                            break;
                        }
                    }

                    // leave party
                    OnMessage(partyNum, string.Format("{0} has left the party.", GetPlayerName(index)));
                    OnRemove(index, partyNum);
                }
                else
                {
                    // not the leader, just leave
                    OnMessage(partyNum, string.Format("{0} has left the party.", GetPlayerName(index)));
                    OnRemove(index, partyNum);
                }
            }
            else
            {
                // only 2 people, disband
                OnMessage(partyNum, "The party has been disbanded.");

                // remove leader
                OnRemove(Data.Party[partyNum].Leader, partyNum);

                // clear out everyone's party
                for (i = 0; i < Variables.MaxPartyMembers; i++)
                {
                    index = Data.Party[partyNum].Member[i];
                    // player exist?
                    if (index > 0)
                    {
                        OnRemove(index, partyNum);
                    }
                }

                // clear out the party itself
                OnClear(partyNum);
            }
        }
    }

    public static void OnInvite(int index, int target)
    {
        // make sure they're not busy
        if (Data.TempPlayer[target].PartyInvite >= 0 | Data.TempPlayer[target].TradeRequest >= 0)
        {
            // they've already got a request for trade/party
            NetworkSend.SendPlayerMessage(index, "This player is busy.", (int)ColorName.BrightRed);
            return;
        }

        // make syure they're not in a party
        if (Data.TempPlayer[target].InParty >= 0)
        {
            // they're already in a party
            NetworkSend.SendPlayerMessage(index, "This player is already in a party.", (int)ColorName.BrightRed);
            return;
        }

        // check if we're in a party
        if (Data.TempPlayer[index].InParty >= 0)
        {
            var partyNum = Data.TempPlayer[index].InParty;
            // make sure we're the leader
            if (Data.Party[partyNum].Leader == index)
            {
                // got a blank slot?
                var loopTo = Variables.MaxPartyMembers;
                for (var i = 0; i < loopTo; i++)
                {
                    if (Data.Party[partyNum].Member[i] == -1)
                    {
                        // send the invitation
                        NetworkSend.SendPartyInvite(target, index);

                        // set the invite target
                        Data.TempPlayer[target].PartyInvite = index;

                        // let them know
                        NetworkSend.SendPlayerMessage(index, "Party invitation sent.", (int)ColorName.Pink);
                        return;
                    }
                }

                // no room
                NetworkSend.SendPlayerMessage(index, "Party is full.", (int)ColorName.BrightRed);
                return;
            }

            // not the leader
            NetworkSend.SendPlayerMessage(index, "You are not the party leader.", (int)ColorName.BrightRed);
            return;
        }

        // not in a party - doesn't matter!
        NetworkSend.SendPartyInvite(target, index);

        // set the invite target
        Data.TempPlayer[target].PartyInvite = index;

        // let them know
        NetworkSend.SendPlayerMessage(index, "Party invitation sent.", (int)ColorName.Pink);
    }

    public static void OnAccept(int index, int target)
    {
        var partyNum = 0;
        int i;

        // check if already in a party
        if (Data.TempPlayer[index].InParty >= 0)
        {
            // get the partynumber
            partyNum = Data.TempPlayer[index].InParty;
            // got a blank slot?
            for (i = 0; i < Variables.MaxPartyMembers; i++)
            {
                if (Data.Party[partyNum].Member[i] == -1)
                {
                    // add to the party
                    Data.Party[partyNum].Member[i] = target;

                    // recount party
                    OnCount(partyNum);

                    // send update to all - including new player
                    NetworkSend.SendPartyUpdate(partyNum);
                    NetworkSend.SendPartyVitals(partyNum, target);

                    // let everyone know they've joined
                    OnMessage(partyNum, string.Format("{0} has joined the party.", GetPlayerName(target)));

                    // add them in
                    Data.TempPlayer[target].InParty = (byte)partyNum;
                    return;
                }
            }

            // no empty slots - let them know
            NetworkSend.SendPlayerMessage(index, "Party is full.", (int)ColorName.BrightRed);
            NetworkSend.SendPlayerMessage(target, "Party is full.", (int)ColorName.BrightRed);
            return;
        }

        // not in a party. Create one with the new person.
        for (i = 0; i < Variables.MaxParty; i++)
        {
            // find blank party
            if (!(Data.Party[i].Leader > -1))
            {
                partyNum = i;
                break;
            }
        }

        // create the party
        Data.Party[partyNum].MemberCount = 2;
        Data.Party[partyNum].Leader = index;
        Data.Party[partyNum].Member[0] = index;
        Data.Party[partyNum].Member[1] = target;

        NetworkSend.SendPartyUpdate(partyNum);
        NetworkSend.SendPartyVitals(partyNum, index);
        NetworkSend.SendPartyVitals(partyNum, target);

        // let them know it's created
        OnMessage(partyNum, "Party created.");
        OnMessage(partyNum, string.Format("{0} has joined the party.", GetPlayerName(index)));

        // clear the invitation
        Data.TempPlayer[target].PartyInvite = -1;

        // add them to the party
        Data.TempPlayer[index].InParty = (byte)partyNum;
        Data.TempPlayer[target].InParty = (byte)partyNum;
    }

    public static void OnDecline(int index, int target)
    {
        NetworkSend.SendPlayerMessage(index, string.Format("{0} has declined to join your party.", GetPlayerName(target)),
            (int)ColorName.BrightRed);
        NetworkSend.SendPlayerMessage(target, "You declined to join the party.", (int)ColorName.Yellow);

        // clear the invitation
        Data.TempPlayer[target].PartyInvite = -1;
    }

    public static void OnCount(int partyNum)
    {
        int i;
        var highindex = 0;

        // find the high index
        for (i = Variables.MaxPartyMembers - 1; i >= 0; i -= 1)
        {
            if (Data.Party[partyNum].Member[i] >= 0)
            {
                highindex = i;
                break;
            }
        }

        // count the members
        for (i = 0; i < Variables.MaxPartyMembers; i++)
        {
            // we've got a blank member
            if (Data.Party[partyNum].Member[i] == -1)
            {
                // is it lower than the high index?
                if (i < highindex)
                {
                    // move everyone down a slot
                    var loopTo1 = Variables.MaxPartyMembers - 1;
                    for (var x = i; x < (int)loopTo1; x++)
                    {
                        Data.Party[partyNum].Member[x] = Data.Party[partyNum].Member[x + 1];
                        Data.Party[partyNum].Member[x + 1] = 0;
                    }
                }
                else
                {
                    // not lower - highindex is count
                    Data.Party[partyNum].MemberCount = highindex + 1;
                    return;
                }
            }

            // check if we've reached the max party members
            if (i == Variables.MaxPartyMembers - 1)
            {
                if (highindex == i)
                {
                    Data.Party[partyNum].MemberCount = Variables.MaxPartyMembers;
                    return;
                }
            }
        }

        // if we're here it means that we need to re-count again
        OnCount(partyNum);
    }

    public static void ShareExp(int partyNum, int exp, int index, int mapNum)
    {
        int expShare;
        int leftOver;
        int i;
        int tmpindex;
        var loseMemberCount = default(byte);

        // check if it's worth sharing
        if (!(exp >= Data.Party[partyNum].MemberCount))
        {
            // no party - keep exp for self
            SetPlayerExp(index, GetPlayerExp(index) + exp);
            NetworkSend.SendExp(index);
            return;
        }

        // check members in others maps
        var loopTo = Variables.MaxPartyMembers;
        for (i = 0; i < loopTo; i++)
        {
            tmpindex = Data.Party[partyNum].Member[i];
            if (tmpindex > -1)
            {
                if (PlayerService.Instance.IsConnected(tmpindex) & NetworkConfig.IsPlaying(tmpindex))
                {
                    if (GetPlayerMap(tmpindex) != mapNum)
                    {
                        loseMemberCount = +1;
                    }
                }
            }
        }

        // find out the equal share
        if (Data.Party[partyNum].MemberCount > 0)
        {
            expShare = exp / (Data.Party[partyNum].MemberCount - loseMemberCount);
            leftOver = exp % (Data.Party[partyNum].MemberCount - loseMemberCount);
        }
        else
        {
            expShare = exp;
            leftOver = 0;
        }

        // loop through and give everyone exp
        var loopTo1 = Variables.MaxPartyMembers;
        for (i = 0; i < loopTo1; i++)
        {
            tmpindex = Data.Party[partyNum].Member[i];
            // existing member?
            if (tmpindex > -1)
            {
                // playing?
                if (PlayerService.Instance.IsConnected(tmpindex) & NetworkConfig.IsPlaying(tmpindex))
                {
                    if (GetPlayerMap(tmpindex) == mapNum)
                    {
                        // give them their share
                        SetPlayerExp(tmpindex, GetPlayerExp(tmpindex) + expShare);
                    }
                }
            }
        }

        // give the remainder to a random member
        if (!(leftOver == 0))
        {
            tmpindex = Data.Party[partyNum]
                .Member[(int)Math.Round(General.GetRandom.NextDouble(1d, Data.Party[partyNum].MemberCount))];
            // give the exp
            SetPlayerExp(tmpindex, GetPlayerExp(tmpindex) + leftOver);
        }
    }

    public static void PartyWarp(int index, int mapNum, int x, int y)
    {
        if (Data.TempPlayer[index].InParty >= 0)
        {
            if (Data.Party[Data.TempPlayer[index].InParty].Leader >= 0)
            {
                var loopTo = Data.Party[Data.TempPlayer[index].InParty].MemberCount;
                for (var i = 0; i < loopTo; i++)
                    Player.OnWarp(Data.Party[Data.TempPlayer[index].InParty].Member[i], mapNum, x, y,
                        (byte)Direction.Down);
            }
        }
    }

    public static bool IsPlayerInParty(int index)
    {
        bool isPlayerInParty = false;
        if (index < 0 | index >= Variables.MaxPlayers | !Data.TempPlayer[index].InGame)
            return isPlayerInParty;

        if (Data.TempPlayer[index].InParty >= 0)
            isPlayerInParty = true;
        return isPlayerInParty;
    }

    public static int GetPlayerParty(int index)
    {
        int getPlayerParty = 0;
        if (index < 0 | index >= Variables.MaxPlayers | !Data.TempPlayer[index].InGame)
            return getPlayerParty;
        getPlayerParty = Data.TempPlayer[index].InParty;
        return getPlayerParty;
    }
}