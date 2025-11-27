using Core;
using Core.Globals;
using Core.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Server.Game;
using Server.Game.Net;
using Server.Net;
using static Core.Globals.Command;
using static Core.Net.Packets;
using Type = Core.Globals.Type;

namespace Server;

public static class Item
{
    private static void Save(int itemNum)
    {
        var json = JsonConvert.SerializeObject(Data.Item[itemNum]);

        if (Database.RowExists(itemNum, "item"))
        {
            Database.UpdateRow(itemNum, json, "item", "data");
        }
        else
        {
            Database.InsertRow(itemNum, json, "item");
        }
    }

    public static async Task LoadAllAsync()
    {
        await Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Variables.MaxItems), LoadAsync);
    }

    private static async ValueTask LoadAsync(int itemNum, CancellationToken cancellationToken)
    {
        var data = await Database.SelectRowAsync(itemNum, "item", "data");
        if (data is null)
        {
            Clear(itemNum);
            return;
        }

        var itemData = JObject.FromObject(data).ToObject<Type.Item>();

        Data.Item[itemNum] = itemData;
    }

    private static void Clear(int itemNum)
    {
        Data.Item[itemNum].Name = "";
        Data.Item[itemNum].Description = "";
        Data.Item[itemNum].Ammo = -1;
        Data.Item[itemNum].Stackable = 1;
    }

    public static void HandleRequestItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        var itemNum = packetReader.ReadInt32();
        if (itemNum < 0 || itemNum > Core.Globals.Variables.MaxItems)
        {
            return;
        }

        SendUpdateItemTo(session.Id, itemNum);
    }

    public static void HandleRequestEditItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        if (GetPlayerAccess(session.Id) < (byte) AccessLevel.Mapper)
        {
            return;
        }

        var user = IsEditorLocked(session.Id, EditorType.Item);
        if (!string.IsNullOrEmpty(user))
        {
            NetworkSend.PlayerMsg(session.Id, "The game editor is locked and being used by " + user + ".", (int) ColorName.BrightRed);
            return;
        }

        Data.TempPlayer[session.Id].Editor = EditorType.Item;

        Animation.SendAnimations(session.Id);
        NetworkSend.SendProjectiles(session.Id);
        NetworkSend.SendJobs(session);

        SendItems(session.Id);

        var packet = new PacketWriter(4);

        packet.WriteEnum(ServerPackets.SItemEditor);

        PlayerService.Instance.SendDataTo(session.Id, packet.GetBytes());
    }

    public static void HandleSaveItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var packetReader = new PacketReader(bytes);

        if (GetPlayerAccess(session.Id) < (byte) AccessLevel.Developer)
        {
            return;
        }

        var itemNum = packetReader.ReadInt32();
        if (itemNum < 0 || itemNum > Core.Globals.Variables.MaxItems)
        {
            return;
        }

        Data.Item[itemNum].AccessReq = packetReader.ReadInt32();

        var statCount = Enum.GetNames<Stat>().Length;
        for (var i = 0; i < statCount; i++)
        {
            Data.Item[itemNum].AddStat[i] = (byte)packetReader.ReadInt32();
        }

        Data.Item[itemNum].Animation = packetReader.ReadInt32();
        Data.Item[itemNum].BindType = packetReader.ReadByte();
        Data.Item[itemNum].JobReq = packetReader.ReadInt32();
        Data.Item[itemNum].Data1 = packetReader.ReadInt32();
        Data.Item[itemNum].Data2 = packetReader.ReadInt32();
        Data.Item[itemNum].Data3 = packetReader.ReadInt32();
        Data.Item[itemNum].LevelReq = packetReader.ReadInt32();
        Data.Item[itemNum].Mastery = (byte) packetReader.ReadInt32();
        Data.Item[itemNum].Name = packetReader.ReadString();
        Data.Item[itemNum].Paperdoll = packetReader.ReadInt32();
        Data.Item[itemNum].Icon = packetReader.ReadInt32();
        Data.Item[itemNum].Price = packetReader.ReadInt32();
        Data.Item[itemNum].Rarity = (byte) packetReader.ReadInt32();
        Data.Item[itemNum].Speed = packetReader.ReadInt32();
        Data.Item[itemNum].Stackable = (byte) packetReader.ReadInt32();
        Data.Item[itemNum].Description = packetReader.ReadString();

        for (var i = 0; i < statCount; i++)
        {
            Data.Item[itemNum].StatReq[i] = (byte) packetReader.ReadInt32();
        }

        Data.Item[itemNum].Type = (byte) packetReader.ReadInt32();
        Data.Item[itemNum].SubType = (byte) packetReader.ReadInt32();
        Data.Item[itemNum].ItemLevel = (byte) packetReader.ReadInt32();
        Data.Item[itemNum].KnockBack = (byte) packetReader.ReadInt32();
        Data.Item[itemNum].KnockBackTiles = (byte) packetReader.ReadInt32();
        Data.Item[itemNum].Projectile = packetReader.ReadInt32();
        Data.Item[itemNum].Ammo = packetReader.ReadInt32();

        Item.Save(itemNum);

        General.Logger.LogInformation("{AccountName} saved item #{ItemNum}",
            GetAccountLogin(session.Id), itemNum);

        SendUpdateItemToAll(itemNum);
    }

    public static void HandleGetItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        Player.MapGetItem(session.Id);
    }

    public static void HandleDropItem(GameSession session, ReadOnlyMemory<byte> bytes)
    {
        var buffer = new PacketReader(bytes);

        var invNum = buffer.ReadInt32();
        var amount = buffer.ReadInt32();

        if (Data.TempPlayer[session.Id].InBank || Data.TempPlayer[session.Id].InShop >= 0)
        {
            return;
        }

        if (invNum < 0 || invNum > Core.Globals.Variables.MaxInv)
        {
            return;
        }

        if (GetPlayerInv(session.Id, invNum) < 0 || GetPlayerInv(session.Id, invNum) > Core.Globals.Variables.MaxItems)
        {
            return;
        }

        if (Data.Item[GetPlayerInv(session.Id, invNum)].Type == (byte) ItemCategory.Currency ||
            Data.Item[GetPlayerInv(session.Id, invNum)].Stackable == 1)
        {
            if (amount < 0 | amount > GetPlayerInvValue(session.Id, invNum))
            {
                return;
            }
        }

        Player.MapDropItem(session.Id, invNum, amount);
    }

    public static void SendItems(int playerId)
    {
        for (var itemNum = 0; itemNum < Core.Globals.Variables.MaxItems; itemNum++)
        {
            if (Data.Item[itemNum].Name.Length > 0)
            {
                SendUpdateItemTo(playerId, itemNum);
            }
        }
    }

    public static void SendUpdateItemTo(int playerId, int itemNum)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateItem);

        WriteItemDataToPacket(itemNum, packet);

        PlayerService.Instance.SendDataTo(playerId, packet.GetBytes());
    }

    public static void SendUpdateItemToAll(int itemNum)
    {
        var packet = new PacketWriter();

        packet.WriteEnum(ServerPackets.SUpdateItem);

        WriteItemDataToPacket(itemNum, packet);

        PlayerService.Instance.SendDataToAll(packet.GetBytes());
    }

    private static void WriteItemDataToPacket(int itemNum, PacketWriter packet)
    {
        var statCount = Enum.GetNames<Stat>().Length;

        packet.WriteInt32(itemNum);
        packet.WriteInt32(Data.Item[itemNum].AccessReq);

        for (var i = 0; i < statCount; i++)
        {
            packet.WriteInt32(Data.Item[itemNum].AddStat[i]);
        }

        packet.WriteInt32(Data.Item[itemNum].Animation);
        packet.WriteByte(Data.Item[itemNum].BindType);
        packet.WriteInt32(Data.Item[itemNum].JobReq);
        packet.WriteInt32(Data.Item[itemNum].Data1);
        packet.WriteInt32(Data.Item[itemNum].Data2);
        packet.WriteInt32(Data.Item[itemNum].Data3);
        packet.WriteInt32(Data.Item[itemNum].LevelReq);
        packet.WriteInt32(Data.Item[itemNum].Mastery);
        packet.WriteString(Data.Item[itemNum].Name);
        packet.WriteInt32(Data.Item[itemNum].Paperdoll);
        packet.WriteInt32(Data.Item[itemNum].Icon);
        packet.WriteInt32(Data.Item[itemNum].Price);
        packet.WriteInt32(Data.Item[itemNum].Rarity);
        packet.WriteInt32(Data.Item[itemNum].Speed);
        packet.WriteInt32(Data.Item[itemNum].Stackable);
        packet.WriteString(Data.Item[itemNum].Description);

        for (var i = 0; i < statCount; i++)
        {
            packet.WriteInt32(Data.Item[itemNum].StatReq[i]);
        }

        packet.WriteInt32(Data.Item[itemNum].Type);
        packet.WriteInt32(Data.Item[itemNum].SubType);
        packet.WriteInt32(Data.Item[itemNum].ItemLevel);
        packet.WriteInt32(Data.Item[itemNum].KnockBack);
        packet.WriteInt32(Data.Item[itemNum].KnockBackTiles);
        packet.WriteInt32(Data.Item[itemNum].Projectile);
        packet.WriteInt32(Data.Item[itemNum].Ammo);
    }
}