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

public class Projectile : ProjectileBase, IAsyncData
{
    private static void EnsureSize(int size)
    {
        if (size <= 0)
        {
            return;
        }

        if (Projectile.Instance.Count >= size)
        {
            return;
        }

        lock (Projectile.Instance)
        {
            while (Projectile.Instance.Count < size)
            {
                Projectile.Instance.Add(new Projectile());
            }
        }
    }

    private static bool TryGetProjectileSlot(int index, out int speed, out byte range, out int damage, out int animation)
    {
        try
        {
            if (index < 0)
            {
                speed = 1;
                range = 0;
                damage = 0;
                animation = -1;
                return false;
            }

            var proj = Projectile.Instance[index];
            speed = proj.Speed;
            range = proj.Range;
            damage = proj.Damage;
            animation = proj.Animation;
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            speed = 1;
            range = 0;
            damage = 0;
            animation = -1;
            return false;
        }
    }

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
                            Script.Instance?.CastSkill(map, targetEntity, mp.SkillId, -1);
                        }
                        else
                        {
                            Script.Instance?.AttemptAttack(attackerEntity, targetEntity, null, Projectile.Instance[projId].Damage, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(OnAttack));
                    }
                }
                return;
            }
        }

        // Then npc target at tile excluding owner npc
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
                            Script.Instance?.CastSkill(map, targetEntity, mp.SkillId, -1);
                        }
                        else
                        {
                            Script.Instance?.AttemptAttack(attackerEntity, targetEntity, null, Projectile.Instance[projId].Damage, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(OnAttack));
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
        EnsureSize(Core.Globals.Variables.MaxProjectiles);
        var data = await Database.SelectRowAsync(index, "projectile", "data");
        if (data is null)
        {
            OnClear(index);
            return;
        }

        var projectileData = data.ToObject<Projectile>();

        if (projectileData is null)
        {
            OnClear(index);
            return;
        }

        EnsureSize(index + 1);
        Projectile.Instance[index] = projectileData ?? new Projectile();
    }

    public static void OnFireFreeAim(int playerId, short vx, short vy, int itemNum)
    {
        var map = GetPlayerMap(playerId);
        var mapProjectileNum = -1;
        for (var i = 0; i < Core.Globals.Variables.MaxProjectiles; i++)
        {
            if (Data.MapProjectile[map, i].Index < 0)
            {
                mapProjectileNum = i; break;
            }
        }
        
        if (mapProjectileNum == -1) return;
        int projectileNum = itemNum >= 0 ? Item.Instance[itemNum].Projectile : -1;
        if (projectileNum < 0) return;
        if (Data.TempPlayer[playerId].ProjectileTimer > General.GetTime()) return;

        ref var mp = ref Data.MapProjectile[map, mapProjectileNum];
        Data.TempPlayer[playerId].ProjectileTimer = General.GetTime() + Item.Instance[itemNum].Speed;
        mp.Index = projectileNum;
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
        mp.TravelTime = General.GetTime() + Math.Max(1, Projectile.Instance[projectileNum].Speed);
        mp.Timer = General.GetTime() + 60000;
        NetworkSend.SendProjectileToMap(map, mapProjectileNum);
    }

    public static void OnFreeAim(int playerId, short vx, short vy, int itemNum, int destX, int destY)
    {
        var map = GetPlayerMap(playerId);
        var mapProjectileNum = -1;
        for (var i = 0; i < Core.Globals.Variables.MaxProjectiles; i++)
        {
            if (Data.MapProjectile[map, i].Index < 0)
            { mapProjectileNum = i; break; }
        }
        if (mapProjectileNum == -1) return;
        int index = itemNum >= 0 ? Item.Instance[itemNum].Projectile : -1;
        if (index < 0) return;
        if (Data.TempPlayer[playerId].ProjectileTimer > General.GetTime()) return;

        ref var mp = ref Data.MapProjectile[map, mapProjectileNum];
        Data.TempPlayer[playerId].ProjectileTimer = General.GetTime() + Item.Instance[itemNum].Speed;
        mp.Index = index;
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
        mp.TravelTime = General.GetTime() + Math.Max(1, Projectile.Instance[index].Speed);
        mp.Timer = General.GetTime() + 60000;
        NetworkSend.SendProjectileToMap(map, mapProjectileNum);
    }

    public static void OnShoot(int playerId, int itemNum, int skillNum = -1, int dir = -1, bool suppressCooldown = false)
    {
        var map = GetPlayerMap(playerId);
        var mapProjectileNum = -1;
        for (var i = 0; i < Core.Globals.Variables.MaxProjectiles; i++)
        {
            if (Data.MapProjectile[map, i].Owner <= 0)
            {
                mapProjectileNum = i;
                break;
            }
        }

        if (mapProjectileNum == -1)
        {
            return;
        }

        var projectile = itemNum >= 0 ? Item.Instance[itemNum].Projectile : skillNum >= 0 ? Skill.Instance[skillNum].Projectile : -1;
        if (projectile == -1)
        {
            return;
        }

        // Respect cooldown unless explicitly suppressed (multi-direction batch)
        if (!suppressCooldown && Data.TempPlayer[playerId].ProjectileTimer > General.GetTime())
        {
            return;
        }

        ref var mapProjectile = ref Data.MapProjectile[map, mapProjectileNum];

        // Only set cooldown if not suppressed here; caller may set once per batch
        if (!suppressCooldown)
        {
            int cooldownMs = Projectile.Instance[projectile].Speed;
            Data.TempPlayer[playerId].ProjectileTimer = General.GetTime() + cooldownMs;
        }
        mapProjectile.Index = projectile;
        mapProjectile.Owner = playerId;
        mapProjectile.OwnerType = (byte)TargetType.Player;
        mapProjectile.Dir = dir >= 0 ? (byte) dir : GetPlayerDir(playerId);
        mapProjectile.X = GetPlayerRawX(playerId);
        mapProjectile.Y = GetPlayerRawY(playerId);
        mapProjectile.SkillId = skillNum;
        mapProjectile.Range = 0;
        mapProjectile.TravelTime = General.GetTime() + Math.Max(1, Projectile.Instance[projectile].Speed);
        mapProjectile.Timer = General.GetTime() + 60000;

        NetworkSend.SendProjectileToMap(map, mapProjectileNum);
    }

    public static void OnNpcProjectile(int map, int npc, int skill, int dir = -1)
    {
        // Find free map projectile slot
        var index = -1;
        for (var i = 0; i < Core.Globals.Variables.MaxProjectiles; i++)
        {
            if (Data.MapProjectile[map, i].Index < 0)
            {
                index = i;
                break;
            }
        }

        if (index == -1)
        {
            return;
        }

        // Skill-defined projectile
        var projectile = skill >= 0 ? Skill.Instance[skill].Projectile : -1;
        if (projectile == -1)
        {
            return;
        }

        // Validate npc is present on map
        if (map < 0 || map >= Core.Globals.Variables.MaxMaps) return;
        if (npc < 0 || npc >= Core.Globals.Variables.MaxMapNpcs) return;
        if (MapNpc.Instance[map, npc].Num < 0) return;

        ref var mapProjectile = ref Data.MapProjectile[map, index];

        mapProjectile.Index = projectile;
        mapProjectile.Owner = npc;
        mapProjectile.OwnerType = (byte) TargetType.Npc;
        mapProjectile.Dir = dir >= 0 ? (byte) dir : MapNpc.Instance[map, npc].Dir;
        mapProjectile.X = MapNpc.Instance[map, npc].X;
        mapProjectile.Y = MapNpc.Instance[map, npc].Y;
        mapProjectile.SkillId = skill;
        mapProjectile.Range = 0;
        mapProjectile.TravelTime = General.GetTime() + Math.Max(1, Projectile.Instance[projectile].Speed);
        mapProjectile.Timer = General.GetTime() + 60000;

        NetworkSend.SendProjectileToMap(map, index);
    }

    public static void OnUpdate()
    {
        int now = General.GetTime();
        for (int x = 0; x < Core.Globals.Variables.MaxMaps; x++)
        {
            if (x < 0 || x >= Server.Map.Instance.Count || Server.Map.Instance[x] == null || Server.Map.Instance[x].Tile == null)
            {
                // Map not initialized; clear any stray projectiles for this map defensively.
                for (int i = 0; i < Core.Globals.Variables.MaxProjectiles; i++)
                {
                    if (Data.MapProjectile[x, i].Index >= 0)
                    {
                        MapProjectile.OnClear(x, i);
                    }
                }
                continue;
            }

            for (int i = 0; i < Core.Globals.Variables.MaxProjectiles; i++)
            {
                ref var mp = ref Data.MapProjectile[x, i];
                // Skip empty slots
                if (mp.Owner <= 0) continue;

                // Expire long-running projectiles defensively
                if (mp.Timer > 0 && now > mp.Timer)
                {
                    MapProjectile.OnClear(x, i);
                    continue;
                }

                var index = mp.Index;
                if (!TryGetProjectileSlot(index, out var speed, out var range, out var damage, out var animation))
                {
                    MapProjectile.OnClear(x, i);
                    continue;
                }

                var interval = Math.Max(1, speed);
                bool moved = false;
                int prevTileX = mp.X / Constants.TileSize;
                int prevTileY = mp.Y / Constants.TileSize;

                // Travel time should only process once per update call.
                // If we're behind, we move a single step and reschedule relative to now.
                if (mp.TravelTime <= 0)
                {
                    mp.TravelTime = now + interval;
                }

                if (now <= mp.TravelTime)
                {
                    continue;
                }

                // We move in discrete ticks. Each tick advances `intervalMs` pixels and is scheduled every `intervalMs` ms.
                // This makes speed=5 mean 5px per 5ms.
                var overDue = now - mp.TravelTime;
                var ticks = 1 + (overDue / interval);
                if (ticks > 50)
                {
                    // Avoid pathological catch-up spirals if the server is paused/stalled.
                    ticks = 50;
                }

                var dtMs = ticks * interval;
                mp.TravelTime += dtMs;

                if (mp.FreeAim == 1)
                {
                    // accumulate thousandths, step whole pixels
                    mp.AccX += mp.Vx * dtMs;
                    mp.AccY += mp.Vy * dtMs;

                    // Fast-path integer division instead of unbounded while loops.
                    // Division truncates toward zero; remainder stays within (-1000, 1000).
                    var dx = mp.AccX / 1000;
                    if (dx != 0)
                    {
                        mp.X += dx;
                        mp.AccX -= dx * 1000;
                    }

                    var dy = mp.AccY / 1000;
                    if (dy != 0)
                    {
                        mp.Y += dy;
                        mp.AccY -= dy * 1000;
                    }

                    // Range is tracked in pixels.
                    mp.Range += (byte)Math.Clamp(Math.Max(Math.Abs(dx), Math.Abs(dy)), 0, 255);
                }
                else
                {
                    var movePixels = (int)Math.Clamp(dtMs, 1, int.MaxValue);
                    // Always move in true 8 directions for projectiles, independent of sprite direction count
                    switch (mp.Dir)
                    {
                        case (byte)Direction.Up: mp.Y -= movePixels; break;
                        case (byte)Direction.Down: mp.Y += movePixels; break;
                        case (byte)Direction.Left: mp.X -= movePixels; break;
                        case (byte)Direction.Right: mp.X += movePixels; break;
                        case (byte)Direction.UpRight: mp.Y -= movePixels; mp.X += movePixels; break;
                        case (byte)Direction.UpLeft: mp.Y -= movePixels; mp.X -= movePixels; break;
                        case (byte)Direction.DownRight: mp.Y += movePixels; mp.X += movePixels; break;
                        case (byte)Direction.DownLeft: mp.Y += movePixels; mp.X -= movePixels; break;
                    }

                    // Range is tracked in pixels.
                    mp.Range = (byte)Math.Clamp(mp.Range + movePixels, 0, 255);
                }

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
                        if (animation >= 0)
                        {
                            int tx = Math.Clamp(mp.X / Constants.TileSize, 0, Server.Map.Instance[x].MaxX - 1);
                            int ty = Math.Clamp(mp.Y / Constants.TileSize, 0, Server.Map.Instance[x].MaxY - 1);
                            NetworkSend.SendAnimation(x, animation, tx, ty);
                            // Try to apply attack on expire at destination
                            OnAttack(x, ref mp, tx, ty, index);
                        }
                        MapProjectile.OnClear(x, i);
                        continue;
                    }
                }

                // Range check (Range in tiles in DB, convert to pixels)
                if (mp.Range >= (range + 1) * 32)
                {
                    // Play hit/expire animation at the last tile location if configured
                    if (animation >= 0)
                    {
                        int tx = Math.Clamp(prevTileX, 0, Server.Map.Instance[x].MaxX - 1);
                        int ty = Math.Clamp(prevTileY, 0, Server.Map.Instance[x].MaxY - 1);
                        NetworkSend.SendAnimation(x, animation, tx, ty);
                        OnAttack(x, ref mp, tx, ty, index);
                    }
                    MapProjectile.OnClear(x, i);
                    continue;
                }

                // Bounds check
                int tileX = Math.Clamp(mp.X / Constants.TileSize, 0, Math.Max(0, Server.Map.Instance[x].MaxX - 1));
                int tileY = Math.Clamp(mp.Y / Constants.TileSize, 0, Math.Max(0, Server.Map.Instance[x].MaxY - 1));
                if (tileX < 0 || tileY < 0 || tileX >= Server.Map.Instance[x].MaxX || tileY >= Server.Map.Instance[x].MaxY)
                {
                    if (animation >= 0)
                    {
                        if (Server.Map.Instance[x].MaxX > 0 && Server.Map.Instance[x].MaxY > 0)
                        {
                            int tx = Math.Clamp(prevTileX, 0, Server.Map.Instance[x].MaxX - 1);
                            int ty = Math.Clamp(prevTileY, 0, Server.Map.Instance[x].MaxY - 1);
                            NetworkSend.SendAnimation(x, animation, tx, ty);
                            OnAttack(x, ref mp, tx, ty, index);
                        }
                    }
                    MapProjectile.OnClear(x, i);
                    continue;
                }

                // Tile collision
                if (Server.Map.Instance[x].Tile[tileX, tileY].Type == TileType.Blocked || Server.Map.Instance[x].Tile[tileX, tileY].Type2 == TileType.Blocked)
                {
                    if (animation >= 0)
                    {
                        NetworkSend.SendAnimation(x, animation, tileX, tileY);
                        OnAttack(x, ref mp, tileX, tileY, index);
                    }
                    
                    MapProjectile.OnClear(x, i);
                    continue;
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
                    if (GetPlayerMap(p.Id) != x) continue;
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
                    if (animation >= 0)
                    {
                        NetworkSend.SendAnimation(x, animation, tileX, tileY);
                    }

                    try
                    {
                        if (mp.SkillId >= 0)
                        {
                            // skill-based projectile: resolve as targeted skill
                            Script.Instance?.CastSkill(x, attackerEntity, mp.SkillId, -1);
                        }
                        else
                        {
                            // item/weapon projectile: basic damage
                            Script.Instance?.AttemptAttack(attackerEntity, targetEntity, null, damage, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(OnUpdate));
                    }
                    MapProjectile.OnClear(x, i);
                    continue;
                }

                // Npcs
                for (int n = 0; n < Core.Globals.Variables.MaxMapNpcs; n++)
                {
                    ref var mn = ref MapNpc.Instance[x, n];
                    if (mn.Num < 0) continue;
                    if (mn.X == tileX && mn.Y == tileY)
                    {
                        // Don't hit owner npc
                        if (!(mp.OwnerType == (byte)TargetType.Npc && mp.Owner == n))
                        {
                            hit = true;
                            if (mp.OwnerType == (byte)TargetType.Player)
                                attackerEntity = Core.Globals.Entity.FromPlayer(mp.Owner, Player.Instance[mp.Owner]);
                            else
                                attackerEntity = Core.Globals.Entity.FromNpc(mp.Owner, MapNpc.Instance[x, mp.Owner]);
                            targetEntity = Core.Globals.Entity.FromNpc(n, mn);
                        }
                        break;
                    }
                }
                
                if (hit)
                {
                    if (animation >= 0)
                    {
                        NetworkSend.SendAnimation(x, animation, tileX, tileY);
                    }

                    try
                    {
                        if (mp.SkillId >= 0)
                        {
                            Script.Instance?.AttemptAttack(attackerEntity, targetEntity, mp.SkillId);
                        }
                        else
                        {
                            Script.Instance?.AttemptAttack(attackerEntity, targetEntity, null, damage, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        General.Logger.LogError(ex, "[Script] Error in {MethodName}", nameof(OnUpdate));
                    }
                    MapProjectile.OnClear(x, i);
                    continue;
                }

                if (!moved) continue;

                NetworkSend.SendProjectileToMap(x, i);
            }
        }
    }

    public static void OnStream(int index)
    {
        throw new NotImplementedException();
    }
}