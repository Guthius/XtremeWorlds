using Core;
using Core.Globals;
using Core.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Server.Game;
using Server.Game.Net;
using Server.Net;
using static Core.Globals.Commands;
using static Core.Net.Packets;
using Type = Core.Globals.Type;
using System.Linq;
using Core.Interfaces;
using Core.Objects;

namespace Server;

public class Projectile : ProjectileBase, IData, IAsyncData
{
    private static void OnAttack(int map, ref Type.MapProjectile mp, int tileX, int tileY, int projId)
    {
        // Build attacker entity snapshot (owner is player for now)
        Entity attackerEntity = null;
        if (mp.OwnerType == (byte)TargetType.Player)
        {
            attackerEntity = Core.Globals.Entity.FromPlayer(mp.Owner, Player.Instance[mp.Owner]);
            attackerEntity.Map = map;
        }
        else if (mp.OwnerType == (byte)TargetType.Npc)
        {
            attackerEntity = Core.Globals.Entity.FromNpc(mp.Owner, MapNpc.Instance[map, mp.Owner]);
            attackerEntity.Map = map;
        }

        // Prefer player target at tile excluding owner
        var playersSnapshot = PlayerService.Instance.Players.ToArray();
        foreach (var p in playersSnapshot)
        {
            if (!NetworkConfig.IsPlaying(p.Id)) continue;
            if (GetPlayerMap(p.Id) != map) continue;
            if (GetPlayerX(p.Id) == tileX && GetPlayerY(p.Id) == tileY)
            {
                if (!(mp.OwnerType == (byte)TargetType.Player && mp.Owner == p.Id))
                {
                    var targetEntity = Core.Globals.Entity.FromPlayer(p.Id, Player.Instance[p.Id]);
                    targetEntity.Map = map;
                    try
                    {
                        if (mp.SkillId >= 0)
                        {
                            Script.Instance?.CastSkill(map, targetEntity, mp.SkillId);
                        }
                        else
                        {
                            Script.Instance?.AttemptAttack(attackerEntity, targetEntity, null, Projectile.Instance[projId].Damage, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        General.Logger.LogError(ex, "[Script] Error in {MethodName}", "AttemptAttack");
                    }
                }
                return;
            }
        }

        // Then NPC target at tile excluding owner NPC
        for (int n = 0; n < Core.Globals.Variables.MaxMapNpcs; n++)
        {
            ref var mn = ref MapNpc.Instance[map, n];
            if (mn.Num < 0) continue;
            if (mn.X == tileX && mn.Y == tileY)
            {
                if (!(mp.OwnerType == (byte)TargetType.Npc && mp.Owner == n))
                {
                    var targetEntity = Core.Globals.Entity.FromNpc(n, mn);
                    targetEntity.Map = map;
                    try
                    {
                        if (mp.SkillId >= 0)
                        {
                            Script.Instance?.CastSkill(map, targetEntity, mp.SkillId);
                        }
                        else
                        {
                            Script.Instance?.AttemptAttack(attackerEntity, targetEntity, null, Projectile.Instance[projId].Damage, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        General.Logger.LogError(ex, "[Script] Error in {MethodName}", "ProjectileAttack");
                    }
                }
                return;
            }
        }
    }
    public static void OnSave(int index)
    {
        var json = JsonConvert.SerializeObject(Projectile.Instance[index]);

        if (Database.RowExists(index, "projectile"))
        {
            Database.UpdateRow(index, json, "projectile", "data");
        }
        else
        {
            Database.InsertRow(index, json, "projectile");
        }
    }

    public static async System.Threading.Tasks.Task OnLoadAllAsync()
    {
        await Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Variables.MaxProjectiles), OnLoadAsync);
    }

    public static async ValueTask OnLoadAsync(int index, CancellationToken cancellationToken)
    {
        var data = await Database.SelectRowAsync(index, "projectile", "data");
        if (data is null)
        {
            OnClear(index);
            return;
        }

        var projectileData = data.ToObject<Projectile>();

        Projectile.Instance[index] = projectileData;
    }

    public static void OnFireFreeAim(int playerId, short vx, short vy, int itemNum)
    {
        var map = GetPlayerMap(playerId);
        var mapProjectileNum = -1;
        for (var i = 0; i < Core.Globals.Variables.MaxProjectiles; i++)
        {
            if (Data.MapProjectile[map, i].ProjectileNum < 0)
            {
                mapProjectileNum = i; break;
            }
        }
        
        if (mapProjectileNum == -1) return;
        int projectileNum = itemNum >= 0 ? Item.Instance[itemNum].Projectile : -1;
        if (projectileNum < 0) return;
        if (Data.TempPlayer[playerId].ProjectileTimer > General.GetTimeMs()) return;

        ref var mp = ref Data.MapProjectile[map, mapProjectileNum];
        Data.TempPlayer[playerId].ProjectileTimer = General.GetTimeMs() + Item.Instance[itemNum].Speed;
        mp.ProjectileNum = projectileNum;
        mp.Owner = playerId;
        mp.OwnerType = (byte)TargetType.Player;
        
        // Derive dir for legacy visuals based on vx,vy in 8 directions
        double ang = Math.Atan2(vy, vx) * 180.0 / Math.PI;
        if (ang > -22.5 && ang <= 22.5) mp.Dir = (byte)Direction.Right;
        else if (ang > 22.5 && ang <= 67.5) mp.Dir = (byte)Direction.DownRight;
        else if (ang > 67.5 && ang <= 112.5) mp.Dir = (byte)Direction.Down;
        else if (ang > 112.5 && ang <= 157.5) mp.Dir = (byte)Direction.DownLeft;
        else if (ang > 157.5 || ang <= -157.5) mp.Dir = (byte)Direction.Left;
        else if (ang > -157.5 && ang <= -112.5) mp.Dir = (byte)Direction.UpLeft;
        else if (ang > -112.5 && ang <= -67.5) mp.Dir = (byte)Direction.Up;
        else mp.Dir = (byte)Direction.UpRight;
        mp.X = GetPlayerRawX(playerId);
        mp.Y = GetPlayerRawY(playerId);
        mp.Vx = vx; mp.Vy = vy; mp.FreeAim = 1;
        mp.AccX = 0; mp.AccY = 0; mp.Range = 0;
        mp.TravelTime = General.GetTimeMs() + Math.Max(1, Projectile.Instance[projectileNum].Speed);
        mp.Timer = General.GetTimeMs() + 60000;
        NetworkSend.SendProjectileToMap(map, mapProjectileNum);
    }

    public static void OnFreeAim(int playerId, short vx, short vy, int itemNum, int destX, int destY)
    {
        var map = GetPlayerMap(playerId);
        var mapProjectileNum = -1;
        for (var i = 0; i < Core.Globals.Variables.MaxProjectiles; i++)
        {
            if (Data.MapProjectile[map, i].ProjectileNum < 0)
            { mapProjectileNum = i; break; }
        }
        if (mapProjectileNum == -1) return;
        int projectileNum = itemNum >= 0 ? Item.Instance[itemNum].Projectile : -1;
        if (projectileNum < 0) return;
        if (Data.TempPlayer[playerId].ProjectileTimer > General.GetTimeMs()) return;

        ref var mp = ref Data.MapProjectile[map, mapProjectileNum];
        Data.TempPlayer[playerId].ProjectileTimer = General.GetTimeMs() + Item.Instance[itemNum].Speed;
        mp.ProjectileNum = projectileNum;
        mp.Owner = playerId;
        mp.OwnerType = (byte)TargetType.Player;
        // Angle purely for 8-dir visual; movement is driven by vx/vy
        double ang = Math.Atan2(vy, vx) * 180.0 / Math.PI;
        if (ang > -22.5 && ang <= 22.5) mp.Dir = (byte)Direction.Right;
        else if (ang > 22.5 && ang <= 67.5) mp.Dir = (byte)Direction.DownRight;
        else if (ang > 67.5 && ang <= 112.5) mp.Dir = (byte)Direction.Down;
        else if (ang > 112.5 && ang <= 157.5) mp.Dir = (byte)Direction.DownLeft;
        else if (ang > 157.5 || ang <= -157.5) mp.Dir = (byte)Direction.Left;
        else if (ang > -157.5 && ang <= -112.5) mp.Dir = (byte)Direction.UpLeft;
        else if (ang > -112.5 && ang <= -67.5) mp.Dir = (byte)Direction.Up;
        else mp.Dir = (byte)Direction.UpRight;
        mp.X = GetPlayerRawX(playerId);
        mp.Y = GetPlayerRawY(playerId);
        mp.Vx = vx; mp.Vy = vy; mp.FreeAim = 1; mp.SkillId = -1;
        mp.AccX = 0; mp.AccY = 0; mp.Range = 0;
        mp.DestX = destX; mp.DestY = destY;
        mp.TravelTime = General.GetTimeMs() + Math.Max(1, Projectile.Instance[projectileNum].Speed);
        mp.Timer = General.GetTimeMs() + 60000;
        NetworkSend.SendProjectileToMap(map, mapProjectileNum);
    }

    public static void OnShoot(int playerId, int itemNum, int skillNum = -1, int dir = -1, bool suppressCooldown = false)
    {
        var map = GetPlayerMap(playerId);
        var mapProjectileNum = -1;
        for (var i = 0; i < Core.Globals.Variables.MaxProjectiles; i++)
        {
            if (Data.MapProjectile[map, i].ProjectileNum < 0)
            {
                mapProjectileNum = i;
                break;
            }
        }

        if (mapProjectileNum == -1)
        {
            return;
        }

        var projectile = itemNum >= 0 ? Item.Instance[itemNum].Projectile : skillNum >= 0 ? Data.Skill[skillNum].Projectile : -1;
        if (projectile == -1)
        {
            return;
        }

        // Respect cooldown unless explicitly suppressed (multi-direction batch)
        if (!suppressCooldown && Data.TempPlayer[playerId].ProjectileTimer > General.GetTimeMs())
        {
            return;
        }

        ref var mapProjectile = ref Data.MapProjectile[map, mapProjectileNum];

        // Only set cooldown if not suppressed here; caller may set once per batch
        if (!suppressCooldown)
        {
            int cooldownMs = Projectile.Instance[projectile].Speed;
            Data.TempPlayer[playerId].ProjectileTimer = General.GetTimeMs() + cooldownMs;
        }
        mapProjectile.ProjectileNum = projectile;
        mapProjectile.Owner = playerId;
        mapProjectile.OwnerType = (byte)TargetType.Player;
        mapProjectile.Dir = dir >= 0 ? (byte) dir : GetPlayerDir(playerId);
        mapProjectile.X = GetPlayerRawX(playerId);
        mapProjectile.Y = GetPlayerRawY(playerId);
        mapProjectile.SkillId = skillNum;
        mapProjectile.Range = 0;
        mapProjectile.TravelTime = General.GetTimeMs() + Math.Max(1, Projectile.Instance[projectile].Speed);
        mapProjectile.Timer = General.GetTimeMs() + 60000;

        NetworkSend.SendProjectileToMap(map, mapProjectileNum);
    }

    public static void OnNpcProjectile(int map, int mapNpcNum, int skillNum, int dir = -1)
    {
        // Find free map projectile slot
        var mapProjectileNum = -1;
        for (var i = 0; i < Core.Globals.Variables.MaxProjectiles; i++)
        {
            if (Data.MapProjectile[map, i].ProjectileNum < 0)
            {
                mapProjectileNum = i;
                break;
            }
        }

        if (mapProjectileNum == -1)
        {
            return;
        }

        // Skill-defined projectile
        var projectile = skillNum >= 0 ? Data.Skill[skillNum].Projectile : -1;
        if (projectile == -1)
        {
            return;
        }

        // Validate npc is present on map
        if (map < 0 || map >= Core.Globals.Variables.MaxMaps) return;
        if (mapNpcNum < 0 || mapNpcNum >= Core.Globals.Variables.MaxMapNpcs) return;
        if (MapNpc.Instance[map, mapNpcNum].Num < 0) return;

        ref var mapProjectile = ref Data.MapProjectile[map, mapProjectileNum];

        mapProjectile.ProjectileNum = projectile;
        mapProjectile.Owner = mapNpcNum;
        mapProjectile.OwnerType = (byte) TargetType.Npc;
        mapProjectile.Dir = dir >= 0 ? (byte) dir : MapNpc.Instance[map, mapNpcNum].Dir;
        mapProjectile.X = MapNpc.Instance[map, mapNpcNum].X;
        mapProjectile.Y = MapNpc.Instance[map, mapNpcNum].Y;
        mapProjectile.SkillId = skillNum;
        mapProjectile.Range = 0;
        mapProjectile.TravelTime = General.GetTimeMs() + Math.Max(1, Projectile.Instance[projectile].Speed);
        mapProjectile.Timer = General.GetTimeMs() + 60000;

        NetworkSend.SendProjectileToMap(map, mapProjectileNum);
    }

    public static void OnUpdate()
    {
        int now = General.GetTimeMs();
        for (int map = 0; map < Core.Globals.Variables.MaxMaps; map++)
        {
            for (int i = 0; i < Core.Globals.Variables.MaxProjectiles; i++)
            {
                ref var mp = ref Data.MapProjectile[map, i];
                // Skip empty slots
                if (mp.ProjectileNum < 0) continue;

                // Expire long-running projectiles defensively
                if (mp.Timer > 0 && now > mp.Timer)
                {
                    MapProjectile.OnClear(map, i);
                    continue;
                }

                var projId = mp.ProjectileNum;
                if (projId < 0 || projId >= Projectile.Instance.Count)
                {
                    MapProjectile.OnClear(map, i);
                    continue;
                }

                int stepMs = Math.Max(1, Projectile.Instance[projId].Speed);
                bool moved = false;
                int prevTileX = mp.X / Constants.TileSize;
                int prevTileY = mp.Y / Constants.TileSize;
                
                while (now > mp.TravelTime)
                {
                    if (mp.FreeAim == 1)
                    {
                        // accumulate thousandths, step whole pixels
                        mp.AccX += mp.Vx;
                        mp.AccY += mp.Vy;
                        while (mp.AccX >= 1000) { mp.X += 1; mp.AccX -= 1000; }
                        while (mp.AccX <= -1000) { mp.X -= 1; mp.AccX += 1000; }
                        while (mp.AccY >= 1000) { mp.Y += 1; mp.AccY -= 1000; }
                        while (mp.AccY <= -1000) { mp.Y -= 1; mp.AccY += 1000; }
                    }
                    else
                    {
                        // Always move in true 8 directions for projectiles, independent of sprite direction count
                        switch (mp.Dir)
                        {
                            case (byte)Direction.Up: mp.Y -= 1; break;
                            case (byte)Direction.Down: mp.Y += 1; break;
                            case (byte)Direction.Left: mp.X -= 1; break;
                            case (byte)Direction.Right: mp.X += 1; break;
                            case (byte)Direction.UpRight: mp.Y -= 1; mp.X += 1; break;
                            case (byte)Direction.UpLeft: mp.Y -= 1; mp.X -= 1; break;
                            case (byte)Direction.DownRight: mp.Y += 1; mp.X += 1; break;
                            case (byte)Direction.DownLeft: mp.Y += 1; mp.X -= 1; break;
                        }
                    }

                    mp.TravelTime += stepMs;
                    mp.Range += 1; // pixels traveled
                    moved = true;

                    // If we have a destination (mouse target), stop when reached/passed
                    if (mp.FreeAim == 1 && (mp.DestX != 0 || mp.DestY != 0))
                    {
                        bool stopX = (mp.Vx >= 0 && mp.X >= mp.DestX) || (mp.Vx <= 0 && mp.X <= mp.DestX) || mp.Vx == 0;
                        bool stopY = (mp.Vy >= 0 && mp.Y >= mp.DestY) || (mp.Vy <= 0 && mp.Y <= mp.DestY) || mp.Vy == 0;
                        if (stopX && stopY)
                        {
                            // Snap to destination tile center rim if desired; for now, snap to Dest
                            mp.X = mp.DestX; mp.Y = mp.DestY;
                            int anim = Projectile.Instance[projId].Animation;
                            if (anim >= 0)
                            {
                                int tx = Math.Clamp(mp.X / Constants.TileSize, 0, Server.Map.Instance[map].MaxX - 1);
                                int ty = Math.Clamp(mp.Y / Constants.TileSize, 0, Server.Map.Instance[map].MaxY - 1);
                                NetworkSend.SendAnimation(map, anim, tx, ty);
                                // Try to apply attack on expire at destination
                                OnAttack(map, ref mp, tx, ty, projId);
                            }
                            MapProjectile.OnClear(map, i);
                            moved = false;
                            break;
                        }
                    }

                    // Range check (Range in tiles in DB, convert to pixels)
                    if (mp.Range >= (Projectile.Instance[projId].Range + 1) * 32)
                    {
                        // Play hit/expire animation at the last tile location if configured
                        int anim = Projectile.Instance[projId].Animation;
                        if (anim >= 0)
                        {
                            int tx = Math.Clamp(prevTileX, 0, Server.Map.Instance[map].MaxX - 1);
                            int ty = Math.Clamp(prevTileY, 0, Server.Map.Instance[map].MaxY - 1);
                            NetworkSend.SendAnimation(map, anim, tx, ty);
                            OnAttack(map, ref mp, tx, ty, projId);
                        }
                        MapProjectile.OnClear(map, i);
                        moved = false;
                        break;
                    }

                    // Bounds check
                    int tileX = Math.Clamp(mp.X / Constants.TileSize, 0, Core.Globals.Variables.MaxMapX - 1);
                    int tileY = Math.Clamp(mp.Y / Constants.TileSize, 0, Core.Globals.Variables.MaxMapY - 1);
                    if (tileX < 0 || tileY < 0 || tileX >= Server.Map.Instance[map].MaxX || tileY >= Server.Map.Instance[map].MaxY)
                    {
                        int anim = Projectile.Instance[projId].Animation;
                        if (anim >= 0)
                        {
                            if (Server.Map.Instance[map].MaxX > 0 && Server.Map.Instance[map].MaxY > 0)
                            {
                                int tx = Math.Clamp(prevTileX, 0, Server.Map.Instance[map].MaxX - 1);
                                int ty = Math.Clamp(prevTileY, 0, Server.Map.Instance[map].MaxY - 1);
                                NetworkSend.SendAnimation(map, anim, tx, ty);
                                OnAttack(map, ref mp, tx, ty, projId);
                            }
                        }
                        MapProjectile.OnClear(map, i);
                        moved = false;
                        break;
                    }

                    // Tile collision
                    if (Server.Map.Instance[map].Tile[tileX, tileY].Type == TileType.Blocked || Server.Map.Instance[map].Tile[tileX, tileY].Type2 == TileType.Blocked)
                    {
                        int anim = Projectile.Instance[projId].Animation;
                        if (anim >= 0)
                        {
                            NetworkSend.SendAnimation(map, anim, tileX, tileY);
                            OnAttack(map, ref mp, tileX, tileY, projId);
                        }
                        
                        MapProjectile.OnClear(map, i);
                        moved = false;
                        break;
                    }

                    // Entity collisions (simple tile match)
                    bool hit = false;
                    Entity attackerEntity = null;
                    Entity targetEntity = null;

                    // Players
                    var players = PlayerService.Instance.Players.ToArray();
                    foreach (var p in players)
                    {
                        if (p == null)
                            continue;

                        if (!NetworkConfig.IsPlaying(p.Id)) continue;
                        if (GetPlayerMap(p.Id) != map) continue;
                        if (GetPlayerX(p.Id) == tileX && GetPlayerY(p.Id) == tileY)
                        {
                            // Don't hit owner player
                            if (!(mp.OwnerType == (byte)TargetType.Player && mp.Owner == p.Id))
                            {
                                hit = true;
                                attackerEntity = Core.Globals.Entity.FromPlayer(mp.Owner, Player.Instance[mp.Owner]);
                                targetEntity = Core.Globals.Entity.FromPlayer(p.Id, Player.Instance[p.Id]);
                            }
                            break;
                        }
                    }

                    if (hit)
                    {
                        int anim = Projectile.Instance[projId].Animation;
                        if (anim >= 0)
                        {
                            NetworkSend.SendAnimation(map, anim, tileX, tileY);
                        }

                        try
                        {
                            if (mp.SkillId >= 0)
                            {
                                // skill-based projectile: resolve as targeted skill
                                Script.Instance?.CastSkill(map, attackerEntity, mp.SkillId);
                            }
                            else
                            {
                                // item/weapon projectile: basic damage
                                Script.Instance?.AttemptAttack(attackerEntity, targetEntity, null, Projectile.Instance[projId].Damage, true);
                            }
                        }
                        catch (Exception ex)
                        {
                            General.Logger.LogError(ex, "[Script] Error in {MethodName}", "ProjectileAttack");
                        }
                        MapProjectile.OnClear(map, i);
                        moved = false;
                        break;
                    }

                    // Npcs
                    for (int n = 0; n < Core.Globals.Variables.MaxMapNpcs; n++)
                    {
                        ref var mn = ref MapNpc.Instance[map, n];
                        if (mn.Num < 0) continue;
                        if (mn.X == tileX && mn.Y == tileY)
                        {
                            // Don't hit owner npc
                            if (!(mp.OwnerType == (byte)TargetType.Npc && mp.Owner == n))
                            {
                                hit = true;
                                attackerEntity = Core.Globals.Entity.FromPlayer(mp.Owner, Player.Instance[mp.Owner]);
                                targetEntity = Core.Globals.Entity.FromNpc(n, mn);
                            }
                            break;
                        }
                    }
                    
                    if (hit)
                    {
                        int anim = Projectile.Instance[projId].Animation;
                        if (anim >= 0)
                        {
                            NetworkSend.SendAnimation(map, anim, tileX, tileY);
                        }

                        try
                        {
                            if (mp.SkillId >= 0)
                            {
                                Script.Instance?.AttemptAttack(attackerEntity, targetEntity, mp.SkillId);
                            }
                            else
                            {
                                Script.Instance?.AttemptAttack(attackerEntity, targetEntity, null, Projectile.Instance[projId].Damage, true);
                            }
                        }
                        catch (Exception ex)
                        {
                            General.Logger.LogError(ex, "[Script] Error in {MethodName}", "ProjectileAttack");
                        }
                        MapProjectile.OnClear(map, i);
                        moved = false;
                        break;
                    }
                }

                if (!moved) continue;

                NetworkSend.SendProjectileToMap(map, i);
            }
        }
    }

    public static void OnStream(int index)
    {
        throw new NotImplementedException();
    }
}