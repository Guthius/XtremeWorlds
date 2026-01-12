using System.Threading.Tasks;
using Core;
using Core.Globals;
using Microsoft.Extensions.Logging;
using Server.Game;
using static Core.Globals.Commands;

namespace Server;

public static class Loop
{
    private static readonly double[] PlayerMoveRemainder = new double[Core.Globals.Variables.MaxPlayers];

    public static async System.Threading.Tasks.Task ServerAsync()
    {
        var tmr25 = 0;
        var tmr500 = 0;
        var tmrPlayerWalk = 0;
        var tmrNpcWalk = 0;
        var tmr1000 = 0;
        var tmrProj = 0;
        var tmr60000 = 0;
        var tmr3600000 = 0;
        var lastUpdateSavePlayers = 0;
        var lastUpdateMapSpawnItems = 0;

        while (true)
        {
            // Update our current tick value.
            var tick = General.GetTime();

            await General.CheckShutDownCountDownAsync();

            if (tick > tmr25)
            {
                // Update all our available events.
                EventLogic.UpdateEventLogic();

                tmr25 = General.GetTime() + 25;
            }

            if (tick > tmrPlayerWalk)
            {
                foreach (var player in PlayerService.Instance.Players)
                {
                    var id = player.Id;
                    if (id < 0 || id >= Player.Instance.Count)
                    {
                        continue;
                    }

                    var basePlayer = Player.Instance[id];
                    if (basePlayer is null)
                    {
                        continue;
                    }

                    if (basePlayer.Moving > 0)
                    {
                        int jobId = basePlayer.Job;
                        double jobSpeed = 1.0;
                        if (jobId >= 0 && jobId < Job.Instance.Count)
                            jobSpeed = Job.Instance[jobId].MoveSpeed;

                        if (jobSpeed == 0) jobSpeed = 1.0;

                        // Skill-based movement modifier (temporary).
                        // Expiry is checked against current tick time.
                        float mult = 1.0f;
                        if (id >= 0 && id < Data.TempPlayer.Length)
                        {
                            mult = Data.TempPlayer[id].MoveSpeedMultiplier;
                            if (mult == 0) mult = 1.0f;
                            if (Data.TempPlayer[id].MoveSpeedMultiplierTimer > 0 && Data.TempPlayer[id].MoveSpeedMultiplierTimer <= tick)
                            {
                                Data.TempPlayer[id].MoveSpeedMultiplier = 1.0f;
                                Data.TempPlayer[id].MoveSpeedMultiplierTimer = 0;
                                mult = 1.0f;
                            }
                        }

                        // Walking is intentionally slower than running.
                        if (basePlayer.Moving == (byte)MovementState.Walking)
                            jobSpeed *= 0.5;

                        jobSpeed *= mult;

                        // Fractional speeds accumulate across ticks.
                        if (id >= 0 && id < PlayerMoveRemainder.Length)
                            PlayerMoveRemainder[id] += jobSpeed;

                        int steps = 0;
                        if (id >= 0 && id < PlayerMoveRemainder.Length)
                        {
                            steps = (int)Math.Floor(PlayerMoveRemainder[id]);
                            if (steps != 0) PlayerMoveRemainder[id] -= steps;
                        }

                        if (steps == 0)
                            continue;

                        // Determine movement direction based on sign of steps
                        bool reverseDirection = steps < 0;
                        int absSteps = Math.Abs(steps);
                        
                        // Safety cap to prevent pathological loops if data is bad.
                        if (absSteps > 64) absSteps = 64;

                        // If negative speed, reverse the player's direction for this movement
                        byte originalDir = basePlayer.Dir;
                        if (reverseDirection)
                        {
                            // Reverse direction based on actual Direction enum values
                            // 0=Down, 1=Right, 2=Left, 3=Up
                            basePlayer.Dir = basePlayer.Dir switch
                            {
                                0 => 3, // Down -> Up
                                1 => 2, // Right -> Left
                                2 => 1, // Left -> Right
                                3 => 0, // Up -> Down
                                _ => basePlayer.Dir
                            };
                        }

                        for (int step = 0; step < absSteps; step++)
                        {
                            Player.OnMove(id, basePlayer.Dir, basePlayer.Moving, false);
                            if (id < 0 || id >= Player.Instance.Count) break;
                            if (!Player.Instance[id].IsMoving && Player.Instance[id].Moving == 0) break;
                        }

                        // Restore original direction if it was reversed
                        if (reverseDirection && id >= 0 && id < Player.Instance.Count && Player.Instance[id] != null)
                        {
                            Player.Instance[id].Dir = originalDir;
                        }
                    }
                    else
                    {
                        if (id >= 0 && id < PlayerMoveRemainder.Length) PlayerMoveRemainder[id] = 0;
                    }
                }

                // Player walk tick interval
                tmrPlayerWalk = General.GetTime() + 5;
            }

            if (tick > tmrNpcWalk)
            {
                // NPC pixel step progression (1px per tick) independent of player loop
                MapNpc.ProcessActiveNpcMovement();

                // Event pixel step progression (1px per tick) like NPCs
                Server.Event.ProcessActiveEventMovement();

                tmrNpcWalk = General.GetTime() + 5;
            }

            if (tick > tmrProj)
            {
                // Server-side projectile pixel movement and sparse map broadcasts
                Projectile.OnUpdate();
                tmrProj = General.GetTime() + 5;
            }

            if (tick > tmr3600000)
            {
                try
                {
                    Script.Instance?.ServerHour();
                }
                catch (Exception ex)
                {
                    General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(Loop));
                }

                tmr3600000 = General.GetTime() + 3600000;
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

                tmr60000 = General.GetTime() + 60000;
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

                // Expire any temporary move speed multipliers (even if players are idle).
                foreach (var player in PlayerService.Instance.Players)
                {
                    var id = player.Id;
                    if (id < 0 || id >= Data.TempPlayer.Length) continue;

                    if (Data.TempPlayer[id].MoveSpeedMultiplierTimer > 0 && Data.TempPlayer[id].MoveSpeedMultiplierTimer <= tick)
                    {
                        Data.TempPlayer[id].MoveSpeedMultiplier = 1.0f;
                        Data.TempPlayer[id].MoveSpeedMultiplierTimer = 0;
                        NetworkSend.SendPlayerXYToMap(id);
                    }
                }

                // Move the timer up 1000ms.
                tmr1000 = General.GetTime() + 1000;
            }

            if (tick > tmr500)
            {
                UpdateMapAi();

                // Move the timer up 500ms.
                tmr500 = General.GetTime() + 500;
            }

            // Checks to spawn map items every 1 minute
            if (tick > lastUpdateMapSpawnItems)
            {
                UpdateMapSpawnItems();
                lastUpdateMapSpawnItems = General.GetTime() + 60000;
            }

            // Checks to save players every 5 minutes
            if (tick > lastUpdateSavePlayers)
            {
                await UpdateSavePlayers();
                lastUpdateSavePlayers = General.GetTime() + 300000;
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

    private static async Task UpdateSavePlayers()
    {
        var players = PlayerService.Instance.Players.ToList();
        if (players.Count == 0)
        {
            return;
        }
        
        Console.WriteLine("Saving all online players...");

        foreach (var player in players)
        {
            await Account.OnSave(player.Id);
        }
    }

    private static void UpdateMapSpawnItems()
    {
        // ///////////////////////////////////////////
        // // This is used for respawning map items //
        // ///////////////////////////////////////////

        var mapCount = Math.Min(Variables.MaxMaps, Server.Map.Instance.Count);
        for (var map = 0; map < mapCount; map++)
        {
            for (var mapItem = 0; mapItem < Variables.MaxMapItems; mapItem++)
            {
                MapItem.OnClear(mapItem, map);
            }

            MapItem.Spawn(map);
            NetworkSend.SendMapItemsToAll(map);
        }
    }

    private static void UpdateMapAi()
    {
        // Clear the entity list before repopulating to avoid accumulating instances
        Entity.Instances.Clear();

        var entities = Entity.Instances;
        var mapCount = Math.Min(Variables.MaxMaps, Server.Map.Instance.Count);
        if (mapCount <= 0)
        {
            return;
        }

        // Use entities from entity class
        for (var map = 0; map < mapCount; map++)
        {
            // Add Npcs
            for (var i = 0; i < Variables.MaxMapNpcs; i++)
            {
                var npc = Entity.FromNpc(i, MapNpc.Instance[map, i]);
                if (npc.Num < 0)
                {
                    continue;
                }

                npc.Map = map;
                entities.Add(npc);
            }

            // Add Players
            foreach (var i in PlayerService.Instance.Players)
            {
                var id = i.Id;
                if (id < 0 || id >= Player.Instance.Count)
                {
                    continue;
                }

                var player = Player.Instance[id];
                if (player is null)
                {
                    continue;
                }

                if (player.Map != map)
                {
                    continue;
                }

                var entity = Entity.FromPlayer(id, player);
                if (!IsPlaying(i.Id))
                {
                    continue;
                }

                player.Map = map;
                entities.Add(entity);
            }
        }

        long tickCount = General.GetTime();

        for (int x = 0; x < entities.Count; x++)
        {
            var entity = entities[x];
            if (entity == null) continue;
            var vitals = entity.Vital;
            var map = entity.Map;

            // Resolve completed skill buffers for both players and npcs
            long nowMsBuff = General.GetTime();
            if (entity.Type == Core.Globals.Entity.EntityType.Player)
            {
                int slot = (int)Data.TempPlayer[entity.Id].SkillBuffer;
                if (slot >= 0)
                {
                    int skillId = -1;
                    if (Player.Instance[entity.Id].Skill != null && slot < Player.Instance[entity.Id].Skill.Length)
                        skillId = Player.Instance[entity.Id].Skill[slot].Num;
                    int castMs = (skillId >= 0 && skillId < Skill.Instance.Count) ? Skill.Instance[skillId].CastTime * 1000 : 0;
                    if (nowMsBuff > Data.TempPlayer[entity.Id].SkillBufferTimer + castMs)
                    {
                        Script.Instance?.CastSkill(map, entity, skillId, slot);
                        // clear buffer
                        Data.TempPlayer[entity.Id].SkillBuffer = -1;
                        Data.TempPlayer[entity.Id].SkillBufferTimer = 0;
                        NetworkSend.SendClearSkillBuffer(entity.Id);
                    }
                }
            }
            else if (entity.Type == Core.Globals.Entity.EntityType.Npc)
            {
                int skill = entity.SkillBuffer;
                if (skill >= 0)
                {
                    int castMs = (skill < Skill.Instance.Count) ? Skill.Instance[skill].CastTime * 1000 : 0;
                    if (nowMsBuff > entity.SkillBufferTimer + castMs)
                    {
                        Script.Instance?.CastSkill(map, entity, skill, -1);
                        // clear snapshot & underlying map npc buffer
                        entity.SkillBuffer = -1;
                        entity.SkillBufferTimer = 0;
                        if (map >= 0 && map < Variables.MaxMaps)
                        {
                            if (entity.Id >= 0 && entity.Id < Variables.MaxMapNpcs)
                            {
                                ref var npc = ref MapNpc.Instance[map, entity.Id];
                                npc.SkillBuffer = -1;
                                npc.SkillBufferTimer = 0;
                            }
                        }
                    }
                }
            }
            
            // Only process NPC-specific logic below
            if (entity.Type != Core.Globals.Entity.EntityType.Npc || entity.Num < 0) continue;
            else
            {
                // ATTACKING ON SIGHT (use tile-based distance; ensure property name consistency)
                if (entity.Behavior == (byte)NpcBehavior.AttackOnSight || entity.Behavior == (byte)NpcBehavior.Guard)
                {
                    // make sure it's not stunned
                    if (!(entity.StunDuration > 0))
                    {
                        foreach (var player in PlayerService.Instance.Players)
                        {
                            if (NetworkConfig.IsPlaying(player.Id))
                            {
                                if (GetPlayerMap(player.Id) == map && entity.TargetType == 0 && GetPlayerAccess(player.Id) <= (byte)AccessLevel.Moderator)
                                {
                                    // Detection range
                                    int n = entity.Range;
                                    int ex = entity.X / Constants.TileSize;
                                    int ey = entity.Y / Constants.TileSize;
                                    int px = GetPlayerX(player.Id);
                                    int py = GetPlayerY(player.Id);
                                    int distanceX = Math.Abs(ex - px);
                                    int distanceY = Math.Abs(ey - py);

                                    if (distanceX <= n && distanceY <= n)
                                    {
                                        if (entity.Behavior == (byte)NpcBehavior.AttackOnSight || GetPlayerPk(player.Id))
                                        {
                                            if (!string.IsNullOrEmpty(entity.AttackSay))
                                            {
                                                NetworkSend.SendPlayerMessage(player.Id, GameLogic.CheckGrammar(entity.Name, 1) + " says, '" + entity.AttackSay + "' to you.", (int)ColorName.Yellow);
                                            }
                                            entity.TargetType = (byte)TargetType.Player;
                                            entity.Target = player.Id;
                                            // Persist target into base map data for movement logic
                                            if (map >= 0 && map < Variables.MaxMaps)
                                            {
                                                if (entity.Id >= 0 && entity.Id < Variables.MaxMapNpcs)
                                                {
                                                    ref var mapNpc = ref MapNpc.Instance[map, entity.Id];
                                                    mapNpc.TargetType = entity.TargetType;
                                                    mapNpc.Target = entity.Target;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // Check if target was found for Npc targeting
                        if (entity.TargetType == 0 && entity.Faction > 0)
                        {
                            for (int i = 0; i < entities.Count; i++)
                            {
                                var otherEntity = entities[i];
                                if (otherEntity != null && otherEntity.Num >= 0)
                                {
                                    if (otherEntity.Map != map) continue;
                                    if (ReferenceEquals(otherEntity, entity)) continue;
                                    if ((int)otherEntity.Faction > 0 && otherEntity.Faction != entity.Faction)
                                    {
                                        // Detection range between NPCs
                                        int n = otherEntity.Range;
                                        int ex = entity.X / Constants.TileSize;
                                        int ey = entity.Y / Constants.TileSize;
                                        int ox = otherEntity.X / Constants.TileSize;
                                        int oy = otherEntity.Y / Constants.TileSize;
                                        int distanceX = Math.Abs(ex - ox);
                                        int distanceY = Math.Abs(ey - oy);

                                        if (distanceX <= n && distanceY <= n && entity.Behavior == (byte)NpcBehavior.AttackOnSight)
                                        {
                                            entity.TargetType = (byte)TargetType.Npc;
                                            entity.Target = i;
                                            if (map >= 0 && map < Variables.MaxMaps)
                                            {
                                                if (entity.Id >= 0 && entity.Id < Variables.MaxMapNpcs)
                                                {
                                                    ref var mapNpc = ref MapNpc.Instance[map, entity.Id];
                                                    mapNpc.TargetType = entity.TargetType;
                                                    mapNpc.Target = entity.Target;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Attempt attack using new combat system when target acquired
                if (entity != null && entity.Target >= 0)
                {
                    if (entity.TargetType == (byte)TargetType.Player)
                    {
                        var pid = entity.Target;
                        if (NetworkConfig.IsPlaying(pid) && GetPlayerMap(pid) == map)
                        {
                            // Clear target if out of chase range
                            int ex = entity.X / Constants.TileSize;
                            int ey = entity.Y / Constants.TileSize;
                            int px = GetPlayerX(pid);
                            int py = GetPlayerY(pid);
                            int r = entity.Range;
                            if (Math.Abs(ex - px) > r || Math.Abs(ey - py) > r)
                            {
                                entity.Target = -1;
                                entity.TargetType = 0;
                                // reflect to base map npc if this is an NPC snapshot
                                if (entity.Type == Core.Globals.Entity.EntityType.Npc && map >= 0 && map < Variables.MaxMaps)
                                {
                                    if (entity.Id >= 0 && entity.Id < Variables.MaxMapNpcs)
                                    {
                                        ref var baseNpcClr = ref MapNpc.Instance[map, entity.Id];
                                        baseNpcClr.TargetType = 0;
                                        baseNpcClr.Target = -1;
                                    }
                                }
                            }
                            else
                            {
                                var targetEntity = Core.Globals.Entity.FromPlayer(pid, Player.Instance[pid]);
                                targetEntity.Map = map;
                                // NPC skills: select a valid skill and cast it directly; otherwise do a basic attack
                                bool didCast = false;
                                if (entity.Type == Core.Globals.Entity.EntityType.Npc && entity.Num >= 0 && entity.Num < Npc.Instance.Count)
                                {
                                    var skills = Npc.Instance[entity.Num].Skill;
                                    if (skills != null)
                                    {
                                        long nowMs = General.GetTime();
                                        int dist = Math.Max(Math.Abs(ex - px), Math.Abs(ey - py));
                                        for (int slot = 0; slot < Core.Globals.Variables.MaxNpcSkills && slot < skills.Length; slot++)
                                        {
                                            int sid = skills[slot];
                                            if (sid <= 0 || sid >= Skill.Instance.Count) continue;
                                            var sk = Skill.Instance[sid];
                                            bool inRange = sk.Range == 0 ? (sk.IsAoE || dist <= 1) : dist <= sk.Range;
                                            if (!inRange) continue;
                                            if (map < 0 || map >= Variables.MaxMaps) break;
                                            if (entity.Id < 0 || entity.Id >= Variables.MaxMapNpcs) break;
                                            ref var baseNpc = ref MapNpc.Instance[map, entity.Id];
                                            bool cdReady = baseNpc.SkillCd == null || slot >= baseNpc.SkillCd.Length || baseNpc.SkillCd[slot] <= nowMs;
                                            if (!cdReady) continue;
                                            if (entity.Vital == null || entity.Vital.Length <= (int)Core.Globals.Vital.Mana || entity.Vital[(int)Core.Globals.Vital.Mana] < sk.MpCost) continue;
                                            Script.Instance?.CastSkill(map, entity, sid, slot);
                                            didCast = true;
                                            break;
                                        }
                                    }
                                }

                                if (!didCast)
                                {
                                    Script.Instance?.AttemptAttack(entity, targetEntity);
                                }
                            }
                        }
                        else
                        {
                            entity.Target = -1;
                            entity.TargetType = 0;
                        }
                    }
                    else if (entity.TargetType == (byte)TargetType.Npc)
                    {
                        var id = entity.Target;
                        if (id >= 0 && id < entities.Count)
                        {
                            var targetEntity = entities[id];
                            if (targetEntity != null && targetEntity.Type == Core.Globals.Entity.EntityType.Npc && targetEntity.Map == map && targetEntity.Num >= 0)
                            {
                                int ex = entity.X / Constants.TileSize;
                                int ey = entity.Y / Constants.TileSize;
                                int tx = targetEntity.X / Constants.TileSize;
                                int ty = targetEntity.Y / Constants.TileSize;
                                int r = entity.Range;
                                if (Math.Abs(ex - tx) > r || Math.Abs(ey - ty) > r)
                                {
                                    entity.Target = -1;
                                    entity.TargetType = 0;
                                    if (entity.Type == Core.Globals.Entity.EntityType.Npc && map >= 0 && map < Variables.MaxMaps)
                                    {
                                        if (entity.Id >= 0 && entity.Id < Variables.MaxMapNpcs)
                                        {
                                            ref var baseNpc = ref MapNpc.Instance[map, entity.Id];
                                            baseNpc.TargetType = 0;
                                            baseNpc.Target = -1;
                                        }
                                    }
                                }
                                else
                                {
                                    bool didCast2 = false;
                                    if (entity.Type == Core.Globals.Entity.EntityType.Npc && entity.Num >= 0 && entity.Num < Npc.Instance.Count)
                                    {
                                        var skills2 = Npc.Instance[entity.Num].Skill;
                                        if (skills2 != null)
                                        {
                                            long nowMs2 = General.GetTime();
                                            int dist2 = Math.Max(Math.Abs(ex - tx), Math.Abs(ey - ty));
                                            for (int slot2 = 0; slot2 < Core.Globals.Variables.MaxNpcSkills && slot2 < skills2.Length; slot2++)
                                            {
                                                int sid2 = skills2[slot2];
                                                if (sid2 <= 0 || sid2 >= Skill.Instance.Count) continue;
                                                var sk2 = Skill.Instance[sid2];
                                                bool inRange2 = sk2.Range == 0 ? (sk2.IsAoE || dist2 <= 1) : dist2 <= sk2.Range;
                                                if (!inRange2) continue;
                                                if (map < 0 || map >= Variables.MaxMaps) break;
                                                if (entity.Id < 0 || entity.Id >= Variables.MaxMapNpcs) break;
                                                ref var baseNpc2 = ref MapNpc.Instance[map, entity.Id];
                                                bool cdReady2 = baseNpc2.SkillCd == null || slot2 >= baseNpc2.SkillCd.Length || baseNpc2.SkillCd[slot2] <= nowMs2;
                                                if (!cdReady2) continue;
                                                if (entity.Vital == null || entity.Vital.Length <= (int)Core.Globals.Vital.Mana || entity.Vital[(int)Core.Globals.Vital.Mana] < sk2.MpCost) continue;
                                                Script.Instance?.CastSkill(map, entity, sid2, slot2);
                                                didCast2 = true;
                                                break;
                                            }
                                        }
                                    }
                                    if (!didCast2)
                                    {
                                        Script.Instance?.AttemptAttack(entity, targetEntity);
                                    }
                                }
                            }
                            else
                            {
                                entity.Target = -1;
                                entity.TargetType = 0;
                            }
                        }
                        else
                        {
                            entity.Target = -1;
                            entity.TargetType = 0;
                        }
                    }
                }

                // Simplified death/spawn handling (entity is non-null here)
#pragma warning disable CS8602
                if (vitals != null && vitals[(byte)Core.Globals.Vital.Health] < 0 && entity.SpawnWait > 0)
                {
                    entity.Num = 0;
                    entity.SpawnWait = General.GetTime();
                    vitals[(byte)Core.Globals.Vital.Health] = 0;
                }
#pragma warning restore CS8602

#pragma warning disable CS8602
                // Handle npc respawn logic.
                if (entity.Type == Core.Globals.Entity.EntityType.Npc)
                {
                    // Dead NPCs are represented by a non-zero DeathTimer (absolute server ms).
                    // Once expired, respawn and clear the corpse.
                    if (map >= 0 && map < Server.Map.Instance.Count && x >= 0 && x < Variables.MaxMapNpcs)
                    {
                        ref var baseNpc = ref MapNpc.Instance[map, x];

                        // Skip all AI/updates while dead (corpse window).
                        if (baseNpc.DeathTimer > 0 && tickCount <= baseNpc.DeathTimer)
                        {
                            // Do nothing; corpse remains.
                        }
                        else
                        {
                            if (baseNpc.DeathTimer > 0 && tickCount > baseNpc.DeathTimer)
                            {
                                // Clear corpse state and respawn.
                                baseNpc.DeathTimer = 0;
                                baseNpc.SpawnWait = tickCount > int.MaxValue ? int.MaxValue : (int)tickCount;
                                Server.MapNpc.OnSpawn(x, map);
                            }
                        }
                    }
                }
#pragma warning restore CS8602
            }
        }

        // ----- NPC Movement (Chase + Wander) -----
        var nowMove = General.GetTime();
        foreach (var e in entities)
        {
            if (e == null) continue;
            if (e.Type != Core.Globals.Entity.EntityType.Npc) continue;
            if (e.Num < 0) continue;
            var npcIndex = e.Id; // Index into MapNpc.Instance[map, slot]
            var map = e.Map;
            if (map < 0 || map >= Variables.MaxMaps) continue;
            if (npcIndex < 0 || npcIndex >= Variables.MaxMapNpcs) continue;

            ref var baseNpc = ref MapNpc.Instance[map, npcIndex];

            // Corpse window: NPC stays visible but does not move/act.
            if (baseNpc.DeathTimer > 0 && baseNpc.DeathTimer > nowMove)
            {
                continue;
            }

            // Skip if stunned
            if (baseNpc.StunDuration > 0) continue;

            // Sync any target assigned on snapshot back to base data if base has none.
            if (baseNpc.TargetType == 0 && e.TargetType != 0)
            {
                baseNpc.TargetType = e.TargetType;
                baseNpc.Target = e.Target;
            }

            bool moved = false;

            // If target exists but is out of range, clear it before deciding movement
            if (baseNpc.TargetType == (byte)TargetType.Player && baseNpc.Target >= 0 && NetworkConfig.IsPlaying(baseNpc.Target) && GetPlayerMap(baseNpc.Target) == map)
            {
                int sxR = baseNpc.X / Constants.TileSize;
                int syR = baseNpc.Y / Constants.TileSize;
                int txR = GetPlayerX(baseNpc.Target);
                int tyR = GetPlayerY(baseNpc.Target);
                int rR = Math.Max(0, (int)Npc.Instance[baseNpc.Num].Range);
                if (Math.Abs(sxR - txR) > rR || Math.Abs(syR - tyR) > rR)
                {
                    baseNpc.TargetType = 0;
                    baseNpc.Target = -1;
                }
            }
            else if (baseNpc.TargetType == (byte)TargetType.Npc && baseNpc.Target >= 0)
            {
                int targetSlot = baseNpc.Target;
                if (targetSlot >= 0 && targetSlot < Variables.MaxMapNpcs && MapNpc.Instance[map, targetSlot].Num >= 0)
                {
                    int sxR = baseNpc.X / Constants.TileSize;
                    int syR = baseNpc.Y / Constants.TileSize;
                    int txR = MapNpc.Instance[map, targetSlot].X / Constants.TileSize;
                    int tyR = MapNpc.Instance[map, targetSlot].Y / Constants.TileSize;
                    int rR = Math.Max(0, (int)Npc.Instance[baseNpc.Num].Range);
                    if (Math.Abs(sxR - txR) > rR || Math.Abs(syR - tyR) > rR)
                    {
                        baseNpc.TargetType = 0;
                        baseNpc.Target = -1;
                    }
                }
                else
                {
                    baseNpc.TargetType = 0;
                    baseNpc.Target = -1;
                }
            }

            // Read target info from persistent npc record
            if (baseNpc.TargetType == (byte)TargetType.Player && baseNpc.Target >= 0 && NetworkConfig.IsPlaying(baseNpc.Target) && GetPlayerMap(baseNpc.Target) == map)
            {
                int sx = baseNpc.X / Constants.TileSize;
                int sy = baseNpc.Y / Constants.TileSize;
                int tx = GetPlayerX(baseNpc.Target);
                int ty = GetPlayerY(baseNpc.Target);
                moved = Script.Instance?.TryChase(map, npcIndex, sx, sy, tx, ty);
            }
            else if (baseNpc.TargetType == (byte)TargetType.Npc && baseNpc.Target >= 0)
            {
                int targetSlot = baseNpc.Target;
                if (targetSlot >= 0 && targetSlot < Variables.MaxMapNpcs && MapNpc.Instance[map, targetSlot].Num >= 0)
                {
                    int sx = baseNpc.X / Constants.TileSize;
                    int sy = baseNpc.Y / Constants.TileSize;
                    int tx = MapNpc.Instance[map, targetSlot].X / Constants.TileSize;
                    int ty = MapNpc.Instance[map, targetSlot].Y / Constants.TileSize;
                    moved = Script.Instance?.TryChase(map, npcIndex, sx, sy, tx, ty);
                }
                else
                {
                    baseNpc.TargetType = 0;
                    baseNpc.Target = -1;
                }
            }

            // Wander if not moved and no target. AttackOnSight/Guard now also wander albeit less frequently.
            if (!moved && baseNpc.TargetType == 0)
            {
                bool aggressive = e.Behavior == (byte)NpcBehavior.AttackOnSight || e.Behavior == (byte)NpcBehavior.Guard;
                double chance = aggressive ? 0.02 : 0.05; // aggressive wander less
                if (Random.Shared.NextDouble() < chance)
                {
                    byte dir = (byte)(Random.Shared.Next(0, 4));
                    if (Server.MapNpc.CanMove(map, npcIndex, dir))
                    {
                        Server.MapNpc.OnMove(map, npcIndex, dir, (int)MovementState.Walking);
                    }
                }
            }
        }

        var now = General.GetTime();
        var itemCount = Variables.MaxMapItems;

        for (int map = 0; map < mapCount; map++)
        {
            // Handle map items (public/despawn)
            for (int i = 0; i < itemCount; i++)
            {
                ref var item = ref MapItem.Instance[map, i];
                if (item.Num >= 0 && !string.IsNullOrEmpty(item.PlayerName))
                {
                    if (item.PlayerTimer < now)
                    {
                        item.PlayerName = "";
                        item.PlayerTimer = 0;
                        NetworkSend.SendMapItemToAll(map, i);
                    }

                    if (item.CanDespawn && item.DespawnTimer < now)
                    {
                        MapItem.OnClear(i, map);
                        NetworkSend.SendMapItemToAll(map, i);
                    }
                }
            }

            // Respawn resources
            if (map >= 0 && map < MapResource.Instance.Length)
            {
                var mapResource = MapResource.Instance[map];
                if (mapResource.ResourceCount > 0 && mapResource.ResourceData != null)
                {
                    int count = Math.Min(mapResource.ResourceCount, mapResource.ResourceData.Length);
                    for (int i = 0; i < count; i++)
                    {
                        var resData = mapResource.ResourceData[i];
                        if (resData.X < 0 || resData.Y < 0 || resData.X >= Server.Map.Instance[map].MaxX || resData.Y >= Server.Map.Instance[map].MaxY)
                        {
                            continue;
                        }

                        int resourceindex = Server.Map.Instance[map].Tile[resData.X, resData.Y].Data1;
                        if (resourceindex > 0 && resourceindex < Resource.Instance.Count)
                        {
                            if (resData.State == 1 || resData.Health < 1)
                            {
                                if (resData.Timer + Resource.Instance[resourceindex].RespawnTime * 1000 < now)
                                {
                                    resData.Timer = now;
                                    resData.State = 0;
                                    resData.Health = (byte)Resource.Instance[resourceindex].Health;
                                    NetworkSend.SendMapResourceToMap(map);
                                }
                            }
                        }
                    }
                }
            }
        }

        // Group vital regeneration executed after NPC AI loop (wrapped for script safety)
        Script.Instance?.RegenVitals();
    }
}