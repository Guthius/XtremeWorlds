using Core;
using Core.Globals;
using Microsoft.Extensions.Logging;
using Server.Game;
using static Core.Globals.Command;

namespace Server;

public static class Loop
{
    public static async Task ServerAsync()
    {
        var tmr25 = 0;
        var tmr500 = 0;
        var tmrWalk = 0;
        var tmrNpcWalk = 0; // separate NPC pixel movement timer (same cadence as player walk)
        var tmr1000 = 0;
        var tmrProj = 0;
        var tmr60000 = 0;
        var lastUpdateSavePlayers = 0;
        var lastUpdateMapSpawnItems = 0;

        while (true)
        {
            // Update our current tick value.
            var tick = General.GetTimeMs();

            await General.CheckShutDownCountDownAsync();

            if (tick > tmr25)
            {
                // Update all our available events.
                EventLogic.UpdateEventLogic();

                tmr25 = General.GetTimeMs() + 25;
            }

            if (tick > tmrWalk)
            {
                foreach (var player in PlayerService.Instance.Players)
                {
                    if (Data.Player[player.Id].Moving > 0)
                    {
                        Player.PlayerMove(player.Id, Data.Player[player.Id].Dir, Data.Player[player.Id].Moving, false);
                    }
                }

                // Player walk tick interval
                tmrWalk = General.GetTimeMs() + 5;
            }

            if (tick > tmrNpcWalk)
            {
                // NPC pixel step progression (1px per tick) independent of player loop
                MapNpc.ProcessActiveNpcMovement();

                tmrNpcWalk = General.GetTimeMs() + 5;
            }

            if (tick > tmrProj)
            {
                // Server-side projectile pixel movement and sparse map broadcasts
                Projectile.UpdateProjectiles();
                tmrProj = General.GetTimeMs() + 5;
            }

            if (tick > tmr60000)
            {
                try
                {
                    Script.Instance?.ServerMinute();
                }
                catch (Exception ex)
                {
                    General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(Loop));
                }

                tmr60000 = General.GetTimeMs() + 60000;
            }

            if (tick > tmr1000)
            {
                try
                {
                    Script.Instance?.ServerSecond();
                }
                catch (Exception ex)
                {
                    General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(Loop));
                }

                Clock.Instance.Tick();

                // Move the timer up 1000ms.
                tmr1000 = General.GetTimeMs() + 1000;
            }

            if (tick > tmr500)
            {
                UpdateMapAi();

                // Move the timer up 500ms.
                tmr500 = General.GetTimeMs() + 500;
            }

            // Checks to spawn map items every 1 minute
            if (tick > lastUpdateMapSpawnItems)
            {
                UpdateMapSpawnItems();
                lastUpdateMapSpawnItems = General.GetTimeMs() + 60000;
            }

            // Checks to save players every 5 minutes
            if (tick > lastUpdateSavePlayers)
            {
                UpdateSavePlayers();
                lastUpdateSavePlayers = General.GetTimeMs() + 300000;
            }

            try
            {
                Script.Instance?.Loop();
            }
            catch (Exception ex)
            {
                General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(Loop));
            }

            await Task.Delay(1);
        }
    }

    private static void UpdateSavePlayers()
    {
        var players = PlayerService.Instance.Players.ToList();
        if (players.Count == 0)
        {
            return;
        }
        
        Console.WriteLine("Saving all online players...");

        foreach (var player in players)
        {
            Database.SaveCharacter(player.Id, Data.TempPlayer[player.Id].Slot);
            Database.SaveBank(player.Id);
        }
    }

    private static void UpdateMapSpawnItems()
    {
        // ///////////////////////////////////////////
        // // This is used for respawning map items //
        // ///////////////////////////////////////////

        for (var mapNum = 0; mapNum < Variables.MaxMaps; mapNum++)
        {
            for (var mapItemNum = 0; mapItemNum < Variables.MaxMapItems; mapItemNum++)
            {
                MapItem.OnClear(mapItemNum, mapNum);
            }

            MapItem.Spawn(mapNum);
            NetworkSend.SendMapItemsToAll(mapNum);
        }
    }

    private static void UpdateMapAi()
    {
        // Clear the entity list before repopulating to avoid accumulating instances
        Entity.Instances.Clear();

        var entities = Entity.Instances;
        var mapCount = Variables.MaxMaps;

        // Use entities from entity class
        for (var mapNum = 0; mapNum < mapCount; mapNum++)
        {
            // Add Npcs
            for (var i = 0; i < Variables.MaxMapNpcs; i++)
            {
                var npc = Entity.FromNpc(i, Data.MapNpc[mapNum].Npc[i]);
                if (npc.Num < 0)
                {
                    continue;
                }

                npc.Map = mapNum;
                entities.Add(npc);
            }

            // Add Players
            foreach (var i in PlayerService.Instance.Players)
            {
                if (Data.Player[i.Id].Map != mapNum)
                {
                    continue;
                }

                var player = Entity.FromPlayer(i.Id, Data.Player[i.Id]);
                if (!IsPlaying(i.Id))
                {
                    continue;
                }

                player.Map = mapNum;
                entities.Add(player);
            }
        }

        Script.Instance?.UpdateMapAi();
    }
}