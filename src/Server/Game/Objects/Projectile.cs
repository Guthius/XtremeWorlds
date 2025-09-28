using Core;
using Core.Globals;
using Core.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Server.Game;
using Server.Game.Net;
using Server.Net;
using static Core.Globals.Command;
using static Core.Net.Packets;
using Type = Core.Globals.Type;

namespace Server;

public static class Projectile
{
    private static void SaveProjectile(int projectileNum)
    {
        var json = JsonConvert.SerializeObject(Data.Projectile[projectileNum]);

        if (Database.RowExists(projectileNum, "projectile"))
        {
            Database.UpdateRow(projectileNum, json, "projectile", "data");
        }
        else
        {
            Database.InsertRow(projectileNum, json, "projectile");
        }
    }

    public static async Task LoadProjectilesAsync()
    {
        await Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Constant.MaxProjectiles), LoadProjectileAsync);
    }

    private static async ValueTask LoadProjectileAsync(int projectileNum, CancellationToken cancellationToken)
    {
        var data = await Database.SelectRowAsync(projectileNum, "projectile", "data");
        if (data is null)
        {
            ClearProjectile(projectileNum);
            return;
        }

        var projectileData = data.ToObject<Type.Projectile>();

        Data.Projectile[projectileNum] = projectileData;
    }

    private static void ClearMapProjectile(int mapNum, int mapProjectileNum)
    {
        ref var mp = ref Data.MapProjectile[mapNum, mapProjectileNum];
        mp.ProjectileNum = -1;
        mp.Owner = 0;
        mp.OwnerType = 0;
        mp.X = 0;
        mp.Y = 0;
        mp.Dir = 0;
        mp.Range = 0;
        mp.TravelTime = 0;
        mp.Timer = 0;

        SendProjectileToMap(mapNum, mapProjectileNum);
    }

    private static void ClearProjectile(int projectileNum)
    {
        Data.Projectile[projectileNum].Name = "";
        Data.Projectile[projectileNum].Sprite = 0;
        Data.Projectile[projectileNum].Range = 0;
        Data.Projectile[projectileNum].Speed = 0;
        Data.Projectile[projectileNum].Damage = 0;
        Data.Projectile[projectileNum].Animation = -1;
    }

    public static void HandleRequestEditProjectile(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        if (GetPlayerAccess(session.Id) < (byte) AccessLevel.Developer)
        {
            return;
        }

        var user = IsEditorLocked(session.Id, EditorType.Projectile);
        if (!string.IsNullOrEmpty(user))
        {
            NetworkSend.PlayerMsg(session.Id, "The game editor is locked and being used by " + user + ".", (int) ColorName.BrightRed);
            return;
        }

        SendProjectiles(session.Id);
        Animation.SendAnimations(session.Id);

        Data.TempPlayer[session.Id].Editor = EditorType.Projectile;

        var buffer = new PacketWriter(4);

        buffer.WriteEnum(ServerPackets.SProjectileEditor);

        PlayerService.Instance.SendDataTo(session.Id, buffer.GetBytes());
    }

    public static void HandleSaveProjectile(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        if (GetPlayerAccess(session.Id) < (byte) AccessLevel.Developer)
        {
            return;
        }

        var projectileNum = packetReader.ReadInt32();
        if (projectileNum is < 0 or > Core.Globals.Constant.MaxProjectiles)
        {
            return;
        }

        Data.Projectile[projectileNum].Name = packetReader.ReadString();
        Data.Projectile[projectileNum].Sprite = packetReader.ReadInt32();
        Data.Projectile[projectileNum].Range = (byte) packetReader.ReadInt32();
        Data.Projectile[projectileNum].Speed = packetReader.ReadInt32();
        Data.Projectile[projectileNum].Damage = packetReader.ReadInt32();
        Data.Projectile[projectileNum].Animation = packetReader.ReadInt32();

        SaveProjectile(projectileNum);

        General.Logger.LogInformation("{AccountName} saved projectile #{ProjectileNum}",
            GetAccountLogin(session.Id), projectileNum);

        SendUpdateProjectileToAll(projectileNum);
    }

    public static void HandleRequestProjectile(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        var projectileNum = packetReader.ReadInt32();

        SendUpdateProjectileTo(session.Id, projectileNum);
    }

    public static void HandleClearProjectile(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        var projectileNum = packetReader.ReadInt32();
        _ = packetReader.ReadInt32(); // Target Index
        _ = (TargetType) packetReader.ReadInt32(); // Target TYpe
        _ = packetReader.ReadInt32(); // Target Zone

        var mapNum = GetPlayerMap(session.Id);

        ClearMapProjectile(mapNum, projectileNum);
    }

    private static void SendUpdateProjectileToAll(int projectileNum)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateProjectile);
        packet.WriteInt32(projectileNum);
        packet.WriteString(Data.Projectile[projectileNum].Name);
        packet.WriteInt32(Data.Projectile[projectileNum].Sprite);
        packet.WriteInt32(Data.Projectile[projectileNum].Range);
        packet.WriteInt32(Data.Projectile[projectileNum].Speed);
        packet.WriteInt32(Data.Projectile[projectileNum].Damage);
        packet.WriteInt32(Data.Projectile[projectileNum].Animation);

        PlayerService.Instance.SendDataToAll(packet.GetBytes());
    }

    private static void SendUpdateProjectileTo(int playerId, int projectileNum)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateProjectile);
        packet.WriteInt32(projectileNum);
        packet.WriteString(Data.Projectile[projectileNum].Name);
        packet.WriteInt32(Data.Projectile[projectileNum].Sprite);
        packet.WriteInt32(Data.Projectile[projectileNum].Range);
        packet.WriteInt32(Data.Projectile[projectileNum].Speed);
        packet.WriteInt32(Data.Projectile[projectileNum].Damage);
        packet.WriteInt32(Data.Projectile[projectileNum].Animation);

        PlayerService.Instance.SendDataTo(playerId, packet.GetBytes());
    }

    public static void SendProjectiles(int playerId)
    {
        for (var projectileNum = 0; projectileNum < Core.Globals.Constant.MaxProjectiles; projectileNum++)
        {
            if (Data.Projectile[projectileNum].Name.Length > 0)
            {
                SendUpdateProjectileTo(playerId, projectileNum);
            }
        }
    }

    private static void SendProjectileToMap(int mapNum, int projectileNum)
    {
        var mapProjectile = Data.MapProjectile[mapNum, projectileNum];

        var packet = new PacketWriter(4);

        packet.WriteEnum(ServerPackets.SMapProjectile);
        packet.WriteInt32(projectileNum);
        packet.WriteInt32(mapProjectile.ProjectileNum);
        packet.WriteInt32(mapProjectile.Owner);
        packet.WriteByte(mapProjectile.OwnerType);
        packet.WriteByte(mapProjectile.Dir);
        packet.WriteInt32(mapProjectile.X);
        packet.WriteInt32(mapProjectile.Y);

        NetworkConfig.SendDataToMap(mapNum, packet.GetBytes());
    }

    public static void PlayerFireProjectile(int playerId, int skillNum = -1, int itemNum = -1)
    {
        var mapNum = GetPlayerMap(playerId);
        var mapProjectileNum = -1;
        for (var i = 0; i < Core.Globals.Constant.MaxProjectiles; i++)
        {
            if (Data.MapProjectile[mapNum, i].ProjectileNum <= 0)
            {
                mapProjectileNum = i;
                break;
            }
        }

        if (mapProjectileNum == -1)
        {
            return;
        }

        var projectileNum = skillNum >= 0 ? Data.Skill[skillNum].Projectile : itemNum >= 0 ? Data.Item[itemNum].Projectile : -1;
        if (projectileNum == -1)
        {
            return;
        }

        ref var mapProjectile = ref Data.MapProjectile[mapNum, mapProjectileNum];

        mapProjectile.ProjectileNum = projectileNum;
        mapProjectile.Owner = playerId;
        mapProjectile.OwnerType = (byte) TargetType.Player;
        mapProjectile.Dir = GetPlayerDir(playerId);
        mapProjectile.X = GetPlayerRawX(playerId);
        mapProjectile.Y = GetPlayerRawY(playerId);
        mapProjectile.Range = 0;
        mapProjectile.TravelTime = General.GetTimeMs() + Math.Max(1, Data.Projectile[projectileNum].Speed);
        mapProjectile.Timer = General.GetTimeMs() + 60000;

        SendProjectileToMap(mapNum, mapProjectileNum);
    }

    public static void NpcFireProjectile(int mapNum, int mapNpcNum, int skillNum)
    {
        // Find free map projectile slot
        var mapProjectileNum = -1;
        for (var i = 0; i < Core.Globals.Constant.MaxProjectiles; i++)
        {
            if (Data.MapProjectile[mapNum, i].ProjectileNum <= 0)
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
        var projectileNum = skillNum > 0 ? Data.Skill[skillNum].Projectile : -1;
        if (projectileNum == -1)
        {
            return;
        }

        // Validate NPC is present on map
        if (mapNum < 0 || mapNum >= Data.MapNpc.Length) return;
        if (mapNpcNum < 0 || mapNpcNum >= Core.Globals.Constant.MaxMapNpcs) return;
        if (Data.MapNpc[mapNum].Npc[mapNpcNum].Num < 0) return;

        ref var mapProjectile = ref Data.MapProjectile[mapNum, mapProjectileNum];

        mapProjectile.ProjectileNum = projectileNum;
        mapProjectile.Owner = mapNpcNum;
        mapProjectile.OwnerType = (byte) TargetType.Npc;
        mapProjectile.Dir = Data.MapNpc[mapNum].Npc[mapNpcNum].Dir;
        mapProjectile.X = Data.MapNpc[mapNum].Npc[mapNpcNum].X;
        mapProjectile.Y = Data.MapNpc[mapNum].Npc[mapNpcNum].Y;
        mapProjectile.Range = 0;
        mapProjectile.TravelTime = General.GetTimeMs() + Math.Max(1, Data.Projectile[projectileNum].Speed);
        mapProjectile.Timer = General.GetTimeMs() + 60000;

        SendProjectileToMap(mapNum, mapProjectileNum);
    }

    public static void UpdateProjectiles()
    {
        int now = General.GetTimeMs();
        for (int map = 0; map < Core.Globals.Constant.MaxMaps; map++)
        {
            for (int i = 0; i < Core.Globals.Constant.MaxProjectiles; i++)
            {
                ref var mp = ref Data.MapProjectile[map, i];
                if (mp.ProjectileNum < 0) continue;

                // Expire long-running projectiles defensively
                if (mp.Timer > 0 && now > mp.Timer)
                {
                    ClearMapProjectile(map, i);
                    continue;
                }

                var projId = mp.ProjectileNum;
                if (projId < 0 || projId >= Data.Projectile.Length)
                {
                    ClearMapProjectile(map, i);
                    continue;
                }

                int stepMs = Math.Max(1, Data.Projectile[projId].Speed);
                bool moved = false;
                int prevTileX = mp.X / 32;
                int prevTileY = mp.Y / 32;
                
                while (now > mp.TravelTime)
                {
                    switch (mp.Dir)
                    {
                        case (byte)Direction.Up: mp.Y -= 1; break;
                        case (byte)Direction.Down: mp.Y += 1; break;
                        case (byte)Direction.Left: mp.X -= 1; break;
                        case (byte)Direction.Right: mp.X += 1; break;
                    }
                    mp.TravelTime += stepMs;
                    mp.Range += 1; // pixels traveled
                    moved = true;

                    // Range check (Range in tiles in DB, convert to pixels)
                    if (mp.Range >= (Data.Projectile[projId].Range + 1) * 32)
                    {
                        // Play hit/expire animation at the last tile location if configured
                        int anim = Data.Projectile[projId].Animation;
                        if (anim > 0)
                        {
                            int tx = Math.Clamp(prevTileX, 0, Data.Map[map].MaxX - 1);
                            int ty = Math.Clamp(prevTileY, 0, Data.Map[map].MaxY - 1);
                            Animation.SendAnimation(map, anim, tx, ty);
                    }
                    ClearMapProjectile(map, i);
                    moved = false;
                    break;
                }

                    // Bounds check
                    int tileX = Math.Clamp(mp.X / 32, 0, Core.Globals.Constant.MaxMapx - 1);
                    int tileY = Math.Clamp(mp.Y / 32, 0, Core.Globals.Constant.MaxMapy - 1);
                    if (tileX < 0 || tileY < 0 || tileX >= Data.Map[map].MaxX || tileY >= Data.Map[map].MaxY)
                    {
                        int anim = Data.Projectile[projId].Animation;
                        if (anim > 0)
                        {
                            int tx = Math.Clamp(prevTileX, 0, Data.Map[map].MaxX - 1);
                            int ty = Math.Clamp(prevTileY, 0, Data.Map[map].MaxY - 1);
                            Animation.SendAnimation(map, anim, tx, ty);
                        }
                        ClearMapProjectile(map, i);
                        moved = false;
                        break;
                    }

                    // Tile collision
                    if (Data.Map[map].Tile[tileX, tileY].Type == TileType.Blocked || Data.Map[map].Tile[tileX, tileY].Type2 == TileType.Blocked)
                    {
                        int anim = Data.Projectile[projId].Animation;
                        if (anim > 0)
                        {
                            Animation.SendAnimation(map, anim, tileX, tileY);
                        }
                        ClearMapProjectile(map, i);
                        moved = false;
                        break;
                    }

                    // Entity collisions (simple tile match)
                    bool hit = false;
                    Entity attackerEntity = null;
                    Entity targetEntity = null;

                    // Players
                    foreach (var p in PlayerService.Instance.Players)
                    {
                        if (!NetworkConfig.IsPlaying(p.Id)) continue;
                        if (GetPlayerMap(p.Id) != map) continue;
                        if (GetPlayerX(p.Id) == tileX && GetPlayerY(p.Id) == tileY)
                        {
                            // Don't hit owner player
                            if (!(mp.OwnerType == (byte)TargetType.Player && mp.Owner == p.Id))
                            {
                                hit = true;
                                attackerEntity = Core.Globals.Entity.FromPlayer(mp.Owner, Data.Player[mp.Owner]);
                                targetEntity = Core.Globals.Entity.FromPlayer(p.Id, Data.Player[p.Id]);
                            }
                            break;
                        }
                    }
                    if (hit)
                    {
                        int anim = Data.Projectile[projId].Animation;
                        if (anim >= 0)
                        {
                            Animation.SendAnimation(map, anim, tileX, tileY);
                        }
                        
                        try
                        {
                            Script.Instance?.AttemptAttack(attackerEntity, targetEntity);
                        }
                        catch (Exception ex)
                        {
                            General.Logger.LogError(ex, "[Script] Error in {MethodName}", "AttemptAttack");
                        }
                        ClearMapProjectile(map, i);
                        moved = false;
                        break;
                    }

                    // Npcs
                    for (int n = 0; n < Core.Globals.Constant.MaxMapNpcs; n++)
                    {
                        ref var mn = ref Data.MapNpc[map].Npc[n];
                        if (mn.Num < 0) continue;
                        if (mn.X == tileX && mn.Y == tileY)
                        {
                            // Don't hit owner npc
                            if (!(mp.OwnerType == (byte)TargetType.Npc && mp.Owner == n))
                            {
                                hit = true;
                                attackerEntity = Core.Globals.Entity.FromPlayer(mp.Owner, Data.Player[mp.Owner]);
                                targetEntity = Core.Globals.Entity.FromNpc(n, mn);
                            }
                            break;
                        }
                    }
                    if (hit)
                    {
                        int anim = Data.Projectile[projId].Animation;
                        if (anim >= 0)
                        {
                            Animation.SendAnimation(map, anim, tileX, tileY);
                        }
                        
                        try
                        {
                            Script.Instance?.AttemptAttack(attackerEntity, targetEntity);
                        }
                        catch (Exception ex)
                        {
                            General.Logger.LogError(ex, "[Script] Error in {MethodName}", "AttemptAttack");
                        }
                        ClearMapProjectile(map, i);
                        moved = false;
                        break;
                    }
                }

                if (!moved) continue;

                SendProjectileToMap(map, i);
            }
        }
    }
}