using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Client.Game.UI;
using Core;
using Core.Globals;
using Core.Net;
using System.IO;
using static Core.Globals.Commands;

namespace Client.Net;

public static class Sender
{
    private static readonly int StatCount = Enum.GetValues<Stat>().Length;

    public static void SendAddChar(string name, int sex, int job)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CAddChar);
        packetWriter.WriteByte(GameState.CharNum);
        packetWriter.WriteString(name);
        packetWriter.WriteInt32(sex);
        packetWriter.WriteInt32(job);

        Network.Send(packetWriter);
    }

    public static void SendUseChar(byte slot)
    {
        var packetWriter = new PacketWriter(5);

        packetWriter.WriteEnum(Packets.ClientPackets.CUseChar);
        packetWriter.WriteByte(slot);

        Network.Send(packetWriter);
    }

    public static void SendDelChar(byte slot)
    {
        var packetWriter = new PacketWriter(5);

        packetWriter.WriteEnum(Packets.ClientPackets.CDelChar);
        packetWriter.WriteByte(slot);

        Network.Send(packetWriter);
    }

    public static void SendLogout()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CLogout);

        Network.Send(packetWriter);
    }

    private static byte[] Encrypt(byte[] data)
    {
        using var aes = Aes.Create();

        aes.Key = General.AesKey;
        aes.IV = General.AesIV;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var memoryStream = new MemoryStream();
        using var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);

        cryptoStream.Write(data, 0, data.Length);
        cryptoStream.FlushFinalBlock();

        return memoryStream.ToArray();
    }

    private static string GetVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
    }

    public static void SendLogin(string username, string password)
    {
        var usernameBytes = Encrypt(Encoding.UTF8.GetBytes(username));
        var passwordBytes = Encrypt(Encoding.UTF8.GetBytes(password));
        var versionBytes = Encrypt(Encoding.UTF8.GetBytes(GetVersion()));

        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CLogin);
        packetWriter.WriteBytes(usernameBytes);
        packetWriter.WriteBytes(passwordBytes);
        packetWriter.WriteBytes(versionBytes);

        Network.Send(packetWriter);
    }

    public static void SendRegister(string username, string password)
    {
        var usernameBytes = Encrypt(Encoding.UTF8.GetBytes(username));
        var passwordBytes = Encrypt(Encoding.UTF8.GetBytes(password));
        var versionBytes = Encrypt(Encoding.UTF8.GetBytes(GetVersion()));

        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CRegister);
        packetWriter.WriteBytes(usernameBytes);
        packetWriter.WriteBytes(passwordBytes);
        packetWriter.WriteBytes(versionBytes);

        Network.Send(packetWriter);
    }
    
    public static void GetPing()
    {
        var packetWriter = new PacketWriter(4);

        GameState.PingStart = General.GetTickCount();

        packetWriter.WriteEnum(Packets.ClientPackets.CCheckPing);

        Network.Send(packetWriter);
    }

    public static void SendPlayerMove()
    {
        var packetWriter = new PacketWriter(14);

        packetWriter.WriteEnum(Packets.ClientPackets.CPlayerMove);
        packetWriter.WriteByte(GetPlayerDir(GameState.MyIndex));
        packetWriter.WriteByte(Player.Instance[GameState.MyIndex].Moving);
        packetWriter.WriteInt32(Player.Instance[GameState.MyIndex].X);
        packetWriter.WriteInt32(Player.Instance[GameState.MyIndex].Y);

        Network.Send(packetWriter);
    }

    public static void SendStopPlayerMove()
    {
        var packetWriter = new PacketWriter(5);

        packetWriter.WriteEnum(Packets.ClientPackets.CStopPlayerMove);
        packetWriter.WriteByte(GetPlayerDir(GameState.MyIndex));

        Network.Send(packetWriter);
    }

    public static void SendCancelCast()
    {
        var packetWriter = new PacketWriter(4);
        packetWriter.WriteEnum(Packets.ClientPackets.CCancelCast);
        Network.Send(packetWriter);
    }

    public static void SayMsg(string text)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CSayMsg);
        packetWriter.WriteString(text);

        Network.Send(packetWriter);
    }

    public static void SendKick(string name)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CKickPlayer);
        packetWriter.WriteString(name);

        Network.Send(packetWriter);
    }

    public static void SendBan(string name)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CBanPlayer);
        packetWriter.WriteString(name);

        Network.Send(packetWriter);
    }

    public static void WarpMeTo(string name)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CWarpMeTo);
        packetWriter.WriteString(name);

        Network.Send(packetWriter);
    }

    public static void WarpToMe(string name)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CWarpToMe);
        packetWriter.WriteString(name);

        Network.Send(packetWriter);
    }

    public static void WarpTo(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CWarpTo);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void SendRequestLevelUp()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestLevelUp);

        Network.Send(packetWriter);
    }

    public static void SendSpawnItem(int index, int amount)
    {
        var packetWriter = new PacketWriter(12);

        packetWriter.WriteEnum(Packets.ClientPackets.CSpawnItem);
        packetWriter.WriteInt32(index);
        packetWriter.WriteInt32(amount);

        Network.Send(packetWriter);
    }

    public static void SendSetSprite(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CSetSprite);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void SendSetAccess(string name, byte access)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CSetAccess);
        packetWriter.WriteString(name);
        packetWriter.WriteInt32(access);

        Network.Send(packetWriter);
    }

    public static void SendAttack()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CAttack);

        Network.Send(packetWriter);
    }

    public static void SendMouseAttack(int worldPixelX, int worldPixelY)
    {
        var packetWriter = new PacketWriter(12);
        packetWriter.WriteEnum(Packets.ClientPackets.CMouseAttack);
        packetWriter.WriteInt32(worldPixelX);
        packetWriter.WriteInt32(worldPixelY);
        Network.Send(packetWriter);
    }

    public static void SendPlayerDir()
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CPlayerDir);
        packetWriter.WriteInt32(GetPlayerDir(GameState.MyIndex));

        Network.Send(packetWriter);
    }

    public static void SendRequestNpc(int index)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestNpc);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void SendRequestSkill(int skillNum)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestSkill);
        packetWriter.WriteInt32(skillNum);

        Network.Send(packetWriter);
    }

    public static void SendTrainStat(byte statNum)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CTrainStat);
        packetWriter.WriteInt32(statNum);

        Network.Send(packetWriter);
    }

    public static void BroadcastMsg(string text)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CBroadcastMsg);
        packetWriter.WriteString(text);

        Network.Send(packetWriter);
    }

    public static void PlayerMsg(string text, string msgTo)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CPlayerMsg);
        packetWriter.WriteString(msgTo);
        packetWriter.WriteString(text);

        Network.Send(packetWriter);
    }

    public static void SendAdminMessage(string text)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CAdminMessage);
        packetWriter.WriteString(text);

        Network.Send(packetWriter);
    }

    public static void SendWhosOnline()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CWhosOnline);

        Network.Send(packetWriter);
    }

    public static void SendPlayerInfo(string name)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CPlayerInfoRequest);
        packetWriter.WriteString(name);

        Network.Send(packetWriter);
    }

    public static void SendMotdChange(string message)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CSetMotd);
        packetWriter.WriteString(message);

        Network.Send(packetWriter);
    }

    public static void SendBanList()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CBanList);

        Network.Send(packetWriter);
    }

    public static void SendBanDestroy()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CBanDestroy);

        Network.Send(packetWriter);
    }

    public static void SendChangeInvSlots(int oldSlot, int newSlot)
    {
        var buffer = new PacketWriter(4);

        buffer.WriteInt32((int) Packets.ClientPackets.CSwapInvSlots);
        buffer.WriteInt32(oldSlot);
        buffer.WriteInt32(newSlot);

        Network.Send(buffer);
    }

    public static void SendChangeSkillSlots(int oldSlot, int newSlot)
    {
        var packetWriter = new PacketWriter(12);

        packetWriter.WriteEnum(Packets.ClientPackets.CSwapSkillSlots);
        packetWriter.WriteInt32(oldSlot);
        packetWriter.WriteInt32(newSlot);

        Network.Send(packetWriter);
    }

    public static void SendUseItem(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CUseItem);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void SendDropItem(int invNum, int amount)
    {
        if (GameState.InBank || GameState.InShop >= 0)
        {
            return;
        }

        if (invNum < 0 || invNum > Variables.MaxInventory)
        {
            return;
        }

        if (Player.Instance[GameState.MyIndex].Inventory[invNum].Num < 0 ||
            Player.Instance[GameState.MyIndex].Inventory[invNum].Num > Core.Globals.Variables.MaxItems)
        {
            return;
        }

        if (Item.Instance[GetPlayerInventory(GameState.MyIndex, invNum)].Type == (byte) ItemCategory.Currency ||
            Item.Instance[GetPlayerInventory(GameState.MyIndex, invNum)].Stackable == 1)
        {
            if (amount < 0 || amount > Player.Instance[GameState.MyIndex].Inventory[invNum].Value)
            {
                return;
            }
        }

        var packetWriter = new PacketWriter(12);

        packetWriter.WriteEnum(Packets.ClientPackets.CMapDropItem);
        packetWriter.WriteInt32(invNum);
        packetWriter.WriteInt32(amount);

        Network.Send(packetWriter);
    }

    public static void SendPlayerSearch(int curX, int curY, byte rClick)
    {
        if (!GameLogic.IsInBounds())
        {
            return;
        }

        var packetWriter = new PacketWriter(16);

        packetWriter.WriteEnum(Packets.ClientPackets.CSearch);
        packetWriter.WriteInt32(curX);
        packetWriter.WriteInt32(curY);
        packetWriter.WriteInt32(rClick);

        Network.Send(packetWriter);
    }

    public static void SendAdminWarp(int x, int y)
    {
        var packetWriter = new PacketWriter(12);

        packetWriter.WriteEnum(Packets.ClientPackets.CAdminWarp);
        packetWriter.WriteInt32(x);
        packetWriter.WriteInt32(y);

        Network.Send(packetWriter);
    }

    public static void SendUnequip(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CUnequip);
        packetWriter.WriteInt32(index);
        Network.Send(packetWriter);
    }

    public static void SendForgetSkill(int index)
    {
        // Check for subscript out of range
        if (index < 0 || index > Variables.MaxPlayerSkills)
        {
            return;
        }

        // Dont let them forget a skill which is in CD
        if (Player.Instance[GameState.MyIndex].Skill[index].Cd > 0)
        {
            TextRenderer.AddText("Cannot forget a skill which is cooling down!", (int) ColorName.Red);
            return;
        }

        // Dont let them forget a skill which is buffered
        if (GameState.SkillBuffer == index)
        {
            TextRenderer.AddText("Cannot forget a skill which you are casting!", (int) ColorName.Red);
            return;
        }

        if (Player.Instance[GameState.MyIndex].Skill[index].Num < 0)
        {
            TextRenderer.AddText("No skill found.", (int) ColorName.Red);
            return;
        }

        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CForgetSkill);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void SendRequestMapReport()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CMapReport);

        Network.Send(packetWriter);
    }

    public static void SendRequestAdmin()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CAdmin);

        Network.Send(packetWriter);
    }

    public static void SendUseEmote(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CEmote);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void SendRequestEditResource()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestEditResource);

        Network.Send(packetWriter);
    }

    public static void SendSaveResource(int index)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CSaveResource);
        packetWriter.WriteInt32(index);
        packetWriter.WriteInt32(Resource.Instance[index].Animation);
        packetWriter.WriteString(Resource.Instance[index].EmptyMessage);
        packetWriter.WriteInt32(Resource.Instance[index].ExhaustedImage);
        packetWriter.WriteInt32(Resource.Instance[index].Health);
        packetWriter.WriteInt32(Resource.Instance[index].ExperienceReward);
        packetWriter.WriteInt32(Resource.Instance[index].ItemReward);
        packetWriter.WriteString(Resource.Instance[index].Name);
        packetWriter.WriteInt32(Resource.Instance[index].ResourceImage);
        packetWriter.WriteInt32(Resource.Instance[index].ResourceType);
        packetWriter.WriteInt32(Resource.Instance[index].RespawnTime);
        packetWriter.WriteString(Resource.Instance[index].SuccessMessage);
        packetWriter.WriteInt32(Resource.Instance[index].LvlRequired);
        packetWriter.WriteInt32(Resource.Instance[index].ToolRequired);
        packetWriter.WriteBoolean(Resource.Instance[index].Walkthrough);

        packetWriter.WriteByte(Resource.Instance[index].CommonEventType);
        packetWriter.WriteInt32(Resource.Instance[index].CommonEventData1);
        packetWriter.WriteInt32(Resource.Instance[index].CommonEventData2);
        Network.Send(packetWriter);
    }

    public static void SendRequestEditNpc()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestEditNpc);

        Network.Send(packetWriter);
    }

    public static void SendSaveNpc(int index)
    {
        var packetWriter = new PacketWriter();

        if (index < 0 || index >= Core.Objects.NpcBase.Instance.Count)
        {
            return;
        }

        var npc = Core.Objects.NpcBase.Instance[index];

        packetWriter.WriteEnum(Packets.ClientPackets.CSaveNpc);
        packetWriter.WriteInt32(index);
        packetWriter.WriteInt32(npc.Animation);
        packetWriter.WriteString(npc.AttackSay);
        packetWriter.WriteByte(npc.Behavior);

        for (var i = 0; i < Variables.MaxDropItems; i++)
        {
            packetWriter.WriteInt32(npc.DropChance[i]);
            packetWriter.WriteInt32(npc.DropItem[i]);
            packetWriter.WriteInt32(npc.DropItemValue[i]);
        }

        packetWriter.WriteInt32(npc.Experience);
        packetWriter.WriteByte(npc.Faction);
        packetWriter.WriteInt32(npc.Hp);
        packetWriter.WriteString(npc.Name);
        packetWriter.WriteByte(npc.Range);
        packetWriter.WriteByte(npc.SpawnTime);
        packetWriter.WriteInt32(npc.SpawnSecs);
        packetWriter.WriteInt32(npc.Sprite);

        for (var i = 0; i < StatCount; i++)
        {
            packetWriter.WriteByte(npc.Stat[i]);
        }

        for (var i = 0; i < Variables.MaxNpcSkills; i++)
        {
            packetWriter.WriteByte(npc.Skill[i]);
        }

        packetWriter.WriteByte(npc.Level);
        packetWriter.WriteInt32(npc.Damage);

        packetWriter.WriteInt32(npc.DeathSwitch);
        packetWriter.WriteInt32(npc.DeathVariable);
        packetWriter.WriteInt32(npc.DeathSwitchValue);
        packetWriter.WriteInt32(npc.DeathVariableValue);

        packetWriter.WriteByte(npc.CommonEventType);
        packetWriter.WriteInt32(npc.CommonEventData1);
        packetWriter.WriteInt32(npc.CommonEventData2);
        Network.Send(packetWriter);
    }

    public static void SendRequestEditSkill()
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestEditSkill);

        Network.Send(packetWriter);
    }

    public static void SendSaveSkill(int index)
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CSaveSkill);
        packetWriter.WriteInt32(index);

        packetWriter.WriteInt32(Skill.Instance[index].AccessReq);
        packetWriter.WriteInt32(Skill.Instance[index].AoE);
        packetWriter.WriteInt32(Skill.Instance[index].CastAnim);
        packetWriter.WriteInt32(Skill.Instance[index].CastTime);
        packetWriter.WriteInt32(Skill.Instance[index].CdTime);
        packetWriter.WriteInt32(Skill.Instance[index].JobReq);
        packetWriter.WriteInt32(Skill.Instance[index].Dir);
        packetWriter.WriteInt32(Skill.Instance[index].Duration);
        packetWriter.WriteInt32(Skill.Instance[index].Icon);
        packetWriter.WriteInt32(Skill.Instance[index].Interval);
        packetWriter.WriteBoolean(Skill.Instance[index].IsAoE);
        packetWriter.WriteInt32(Skill.Instance[index].LevelReq);
        packetWriter.WriteInt32(Skill.Instance[index].Map);
        packetWriter.WriteInt32(Skill.Instance[index].MpCost);
        packetWriter.WriteString(Skill.Instance[index].Name);
        packetWriter.WriteInt32(Skill.Instance[index].Range);
        packetWriter.WriteInt32(Skill.Instance[index].SkillAnim);
        packetWriter.WriteInt32(Skill.Instance[index].StunDuration);
        packetWriter.WriteByte(Skill.Instance[index].Type);
        packetWriter.WriteInt32(Skill.Instance[index].Vital);
        packetWriter.WriteInt32(Skill.Instance[index].X);
        packetWriter.WriteInt32(Skill.Instance[index].Y);

        packetWriter.WriteInt32(Skill.Instance[index].IsProjectile);
        packetWriter.WriteInt32(Skill.Instance[index].Projectile);

        packetWriter.WriteByte(Skill.Instance[index].KnockBack);
        packetWriter.WriteByte(Skill.Instance[index].KnockBackTiles);
        packetWriter.WriteInt32(Skill.Instance[index].MultiDirMask);
        packetWriter.WriteInt32(Skill.Instance[index].ChainOnHitSkillId);
        packetWriter.WriteByte(Skill.Instance[index].CommonEventType);
        packetWriter.WriteInt32(Skill.Instance[index].CommonEventData1);
        packetWriter.WriteInt32(Skill.Instance[index].CommonEventData2);

        packetWriter.WriteSingle(Skill.Instance[index].MoveSpeedMultiplier);
        Network.Send(packetWriter);
    }

    public static void SendSaveShop(int index)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CSaveShop);
        packetWriter.WriteInt32(index);
        for (var i = 0; i <= index; i++)
        {
            if (Shop.Instance.Count <= i)
            {
                Shop.Instance.Add(new Shop());
            }
        }

        packetWriter.WriteInt32(Shop.Instance[index].BuyRate);
        packetWriter.WriteString(Shop.Instance[index].Name);

        for (var i = 0; i < Variables.MaxTrades; i++)
        {
            packetWriter.WriteInt32(Shop.Instance[index].TradeItem[i].CostItem);
            packetWriter.WriteInt32(Shop.Instance[index].TradeItem[i].CostValue);
            packetWriter.WriteInt32(Shop.Instance[index].TradeItem[i].Item);
            packetWriter.WriteInt32(Shop.Instance[index].TradeItem[i].ItemValue);
        }

        Network.Send(packetWriter);
    }

    public static void SendRequestEditShop()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestEditShop);

        Network.Send(packetWriter);
    }

    public static void SendSaveAnimation(int index)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CSaveAnimation);
        packetWriter.WriteInt32(index);

        foreach (var frame in Animation.Instance[index].Frames)
        {
            packetWriter.WriteInt32(frame);
        }

        foreach (var loopCount in Animation.Instance[index].LoopCount)
        {
            packetWriter.WriteInt32(loopCount);
        }

        foreach (var loopTime in Animation.Instance[index].LoopTime)
        {
            packetWriter.WriteInt32(loopTime);
        }

        packetWriter.WriteString(Animation.Instance[index].Name);
        packetWriter.WriteString(Animation.Instance[index].Sound);

        foreach (var sprite in Animation.Instance[index].Sprite)
        {
            packetWriter.WriteInt32(sprite);
        }

        Network.Send(packetWriter);
    }

    public static void SendRequestEditAnimation()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestEditAnimation);

        Network.Send(packetWriter);
    }

    public static void SendRequestEditJob()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestEditJob);

        Network.Send(packetWriter);
    }

    public static void SendSaveJob(int index)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CSaveJob);
        packetWriter.WriteInt32(index);
        packetWriter.WriteString(Job.Instance[index].Name);
        packetWriter.WriteString(Job.Instance[index].Desc);
        packetWriter.WriteInt32(Job.Instance[index].MaleSprite);
        packetWriter.WriteInt32(Job.Instance[index].FemaleSprite);
        for (var i = 0; i < StatCount; i++)
        {
            packetWriter.WriteInt32(Job.Instance[index].Stat[i]);
        }

        for (var i = 0; i < Variables.MaxStartItems; i++)
        {
            packetWriter.WriteInt32(Job.Instance[index].StartItem[i]);
            packetWriter.WriteInt32(Job.Instance[index].StartValue[i]);
        }

        for (var i = 0; i < Variables.MaxStartSkills; i++)
        {
            packetWriter.WriteInt32(Job.Instance[index].StartSkill[i]);
        }

        packetWriter.WriteInt32(Job.Instance[index].StartMap);
        packetWriter.WriteByte(Job.Instance[index].StartX);
        packetWriter.WriteByte(Job.Instance[index].StartY);
        packetWriter.WriteInt32(Job.Instance[index].BaseExp);
        packetWriter.WriteSingle(Job.Instance[index].MoveSpeed);

        Network.Send(packetWriter);
    }

    public static void SendSaveItem(int index)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CSaveItem);
        packetWriter.WriteInt32(index);
        packetWriter.WriteInt32(Item.Instance[index].AccessReq);

        for (var i = 0; i < StatCount; i++)
        {
            packetWriter.WriteInt32(Item.Instance[index].AddStat[i]);
        }

        packetWriter.WriteInt32(Item.Instance[index].Animation);
        packetWriter.WriteByte(Item.Instance[index].BindType);
        packetWriter.WriteInt32(Item.Instance[index ].JobReq);
        packetWriter.WriteInt32(Item.Instance[index].Data1);
        packetWriter.WriteInt32(Item.Instance[index].Data2);
        packetWriter.WriteInt32(Item.Instance[index].Data3);
        packetWriter.WriteInt32(Item.Instance[index].LevelReq);
        packetWriter.WriteInt32(Item.Instance[index].Mastery);
        packetWriter.WriteString(Item.Instance[index].Name);
        packetWriter.WriteInt32(Item.Instance[index].Paperdoll);
        packetWriter.WriteInt32(Item.Instance[index].Icon);
        packetWriter.WriteInt32(Item.Instance[index].Price);
        packetWriter.WriteInt32(Item.Instance[index].Rarity);
        packetWriter.WriteInt32(Item.Instance[index].Speed);

        packetWriter.WriteInt32(Item.Instance[index].Stackable);
        packetWriter.WriteString(Item.Instance[index].Description);
        for (var i = 0; i < StatCount; i++)
        {
            packetWriter.WriteInt32(Item.Instance[index].StatReq[i]);
        }

        packetWriter.WriteInt32(Item.Instance[index].Type);
        packetWriter.WriteInt32(Item.Instance[index].SubType);
        packetWriter.WriteInt32(Item.Instance[index].ItemLevel);

        packetWriter.WriteInt32(Item.Instance[index].KnockBack);
        packetWriter.WriteInt32(Item.Instance[index].KnockBackTiles);
        packetWriter.WriteInt32(Item.Instance[index].Projectile);
        packetWriter.WriteInt32(Item.Instance[index].Ammo);

        Network.Send(packetWriter);
    }

    public static void SendRequestEditItem()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestEditItem);

        Network.Send(packetWriter);
    }

    public static void SendCloseEditor()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CCloseEditor);

        Network.Send(packetWriter);
    }

    public static void SendSetHotbarSlot(int type, int newSlot, int oldSlot, int index)
    {
        var packetWriter = new PacketWriter(20);

        packetWriter.WriteEnum(Packets.ClientPackets.CSetHotbarSlot);
        packetWriter.WriteInt32(type);
        packetWriter.WriteInt32(newSlot);
        packetWriter.WriteInt32(oldSlot);
        packetWriter.WriteInt32(index);
        Network.Send(packetWriter);
    }

    public static void SendDeleteHotbar(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CDeleteHotbarSlot);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void SendUseHotbarSlot(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CUseHotbarSlot);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void SendLearnSkill(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CSkillLearn);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void SendCast(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CCast);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void SendRequestMoral(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestMoral);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void SendRequestEditMoral()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestEditMoral);

        Network.Send(packetWriter);
    }

    public static void SendSaveMoral(int index)
    {
        var moral = Moral.Instance[index];

        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CSaveMoral);
        packetWriter.WriteInt32(index);
        packetWriter.WriteString(moral.Name);
        packetWriter.WriteByte(moral.Color);
        packetWriter.WriteBoolean(moral.CanCast);
        packetWriter.WriteBoolean(moral.CanPk);
        packetWriter.WriteBoolean(moral.CanDropItem);
        packetWriter.WriteBoolean(moral.CanPickupItem);
        packetWriter.WriteBoolean(moral.CanUseItem);
        packetWriter.WriteBoolean(moral.DropItems);
        packetWriter.WriteBoolean(moral.LoseExp);
        packetWriter.WriteBoolean(moral.PlayerBlock);
        packetWriter.WriteBoolean(moral.NpcBlock);

        Network.Send(packetWriter);
    }

    public static void SendCloseShop()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CCloseShop);

        Network.Send(packetWriter);
    }

    public static void SendRequestEditScript(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestEditScript);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void SendSaveScript()
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CSaveScript);
        packetWriter.WriteString(string.Join(Environment.NewLine, Data.Script.Code));

        Network.Send(packetWriter);
    }

    public static void SendRequestItem(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestItem);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void SendRequestShop(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestShop);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void SendBuyItem(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CBuyItem);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void SendSellItem(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CSellItem);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void SendRequestAnimation(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestAnimation);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void SendRequestResource(int resourceNum)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteInt32((int)Packets.ClientPackets.CRequestResource);
        packetWriter.WriteInt32(resourceNum);

        Network.Send(packetWriter);
    }


    public static void SendAcceptTrade()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CAcceptTrade);

        Network.Send(packetWriter);
    }

    public static void SendDeclineTrade()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CDeclineTrade);

        Network.Send(packetWriter);
    }

    public static void SendTradeRequest(string name)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CTradeInvite);
        packetWriter.WriteString(name);

        Network.Send(packetWriter);

    }

    public static void SendHandleTradeInvite(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CHandleTradeInvite);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void SendTradeItem(int index, int amount)
    {
        var packetWriter = new PacketWriter(12);

        packetWriter.WriteEnum(Packets.ClientPackets.CTradeItem);
        packetWriter.WriteInt32(index);
        packetWriter.WriteInt32(amount);

        Network.Send(packetWriter);
    }

    public static void SendUntradeItem(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CUntradeItem);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void SendPlayerRequestNewMap()
    {
        if (GameState.GettingMap)
        {
            return;
        }

        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestNewMap);
        packetWriter.WriteInt32(GetPlayerDir(GameState.MyIndex));

        Network.Send(packetWriter);

        GameState.GettingMap = true;
        GameState.CanMoveNow = false;
    }

    public static void SendRequestEditMap()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestEditMap);

        Network.Send(packetWriter);
    }

    public static void SendMap()
    {
        int x;
        int y;
        int i;

        GameState.CanMoveNow = false;

        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CSaveMap);
        packetWriter.WriteString(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Name);
        packetWriter.WriteString(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Music);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Moral);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tileset);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Up);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Down);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Left);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Right);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].BootMap);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].BootX);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].BootY);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxX);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxY);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Weather);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Fog);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].WeatherIntensity);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].FogOpacity);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].FogSpeed);
        packetWriter.WriteBoolean(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MapTint);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MapTintR);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MapTintG);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MapTintB);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MapTintA);
        packetWriter.WriteByte(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Panorama);
        packetWriter.WriteByte(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Parallax);
        packetWriter.WriteByte(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Brightness);
        packetWriter.WriteBoolean(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].NoRespawn);
        packetWriter.WriteBoolean(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Indoors);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Shop);

        // Per-map camera zoom bounds
        packetWriter.WriteSingle(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MinZoom);
        packetWriter.WriteSingle(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxZoom);

        for (i = 0; i < Variables.MaxMapNpcs; i++)
        {
            packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Npc[i]);
        }

        for (x = 0; x < Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxX; x++)
        {
            for (y = 0; y < Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxY; y++)
            {
                packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Data1);
                packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Data2);
                packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Data3);
                packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Data1_2);
                packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Data2_2);
                packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Data3_2);
                packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].DirBlock);

                int layerCount = Enum.GetValues<MapLayer>().Length;
                for (i = 0; i < layerCount; i++)
                {
                    packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Layer[i].Tileset);
                    packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Layer[i].X);
                    packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Layer[i].Y);
                    packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Layer[i].AutoTile);
                }

                packetWriter.WriteInt32((int)Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Type);
                packetWriter.WriteInt32((int)Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Type2);
            }
        }

        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].EventCount);

        if (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].EventCount > 0)
        {
            for (i = 0; i < Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].EventCount; i++)
            {
                {
                    ref var instance = ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i];
                    packetWriter.WriteString(instance.Name);
                    packetWriter.WriteByte(instance.Globals);
                    packetWriter.WriteInt32(instance.X);
                    packetWriter.WriteInt32(instance.Y);
                    packetWriter.WriteInt32(instance.PageCount);
                }

                if (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].PageCount > 0)
                {
                    var count = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].PageCount;
                    for (x = 0; x < count; x++)
                    {
                        {
                            ref var instance1 = ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x];
                            packetWriter.WriteInt32(instance1.ChkVariable);
                            packetWriter.WriteInt32(instance1.VariableIndex);
                            packetWriter.WriteInt32(instance1.VariableCondition);
                            packetWriter.WriteInt32(instance1.VariableCompare);
                            packetWriter.WriteInt32(instance1.ChkSwitch);
                            packetWriter.WriteInt32(instance1.SwitchIndex);
                            packetWriter.WriteInt32(instance1.SwitchCompare);
                            packetWriter.WriteInt32(instance1.ChkHasItem);
                            packetWriter.WriteInt32(instance1.HasItemIndex);
                            packetWriter.WriteInt32(instance1.HasItemAmount);
                            packetWriter.WriteInt32(instance1.ChkSelfSwitch);
                            packetWriter.WriteInt32(instance1.SelfSwitchIndex);
                            packetWriter.WriteInt32(instance1.SelfSwitchCompare);
                            packetWriter.WriteByte(instance1.GraphicType);
                            packetWriter.WriteInt32(instance1.Graphic);
                            packetWriter.WriteInt32(instance1.GraphicX);
                            packetWriter.WriteInt32(instance1.GraphicY);
                            packetWriter.WriteInt32(instance1.GraphicX2);
                            packetWriter.WriteInt32(instance1.GraphicY2);
                            packetWriter.WriteByte(instance1.MoveType);
                            packetWriter.WriteByte(instance1.MoveSpeed);
                            packetWriter.WriteByte(instance1.MoveFreq);
                            packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].MoveRouteCount);
                            packetWriter.WriteInt32(instance1.IgnoreMoveRoute);
                            packetWriter.WriteInt32(instance1.RepeatMoveRoute);

                            if (instance1.MoveRouteCount > 0)
                            {
                                var count2 = instance1.MoveRouteCount;
                                for (y = 0; y < count2; y++)
                                {
                                    packetWriter.WriteInt32(instance1.MoveRoute[y].Index);
                                    packetWriter.WriteInt32(instance1.MoveRoute[y].Data1);
                                    packetWriter.WriteInt32(instance1.MoveRoute[y].Data2);
                                    packetWriter.WriteInt32(instance1.MoveRoute[y].Data3);
                                    packetWriter.WriteInt32(instance1.MoveRoute[y].Data4);
                                    packetWriter.WriteInt32(instance1.MoveRoute[y].Data5);
                                    packetWriter.WriteInt32(instance1.MoveRoute[y].Data6);
                                }
                            }

                            packetWriter.WriteInt32(instance1.IdleAnim);
                            packetWriter.WriteInt32(instance1.DirFix);
                            packetWriter.WriteInt32(instance1.WalkThrough);
                            packetWriter.WriteInt32(instance1.ShowName);
                            packetWriter.WriteByte(instance1.Trigger);
                            packetWriter.WriteInt32(instance1.CommandListCount);
                            packetWriter.WriteByte(instance1.Position);
                        }

                        if (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandListCount > 0)
                        {
                            var count3 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandListCount;
                            for (y = 0; y < count3; y++)
                            {
                                packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].CommandCount);
                                packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].ParentList);
                                if (Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].CommandCount > 0)
                                {
                                    for (int z = 0, count4 = Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].CommandCount; z < count4; z++)
                                    {
                                        {
                                            ref var instance2 = ref Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Event[i].Pages[x].CommandList[y].Commands[z];
                                            packetWriter.WriteInt32(instance2.Index);
                                            packetWriter.WriteString(instance2.Text1);
                                            packetWriter.WriteString(instance2.Text2);
                                            packetWriter.WriteString(instance2.Text3);
                                            packetWriter.WriteString(instance2.Text4);
                                            packetWriter.WriteString(instance2.Text5);
                                            packetWriter.WriteInt32(instance2.Data1);
                                            packetWriter.WriteInt32(instance2.Data2);
                                            packetWriter.WriteInt32(instance2.Data3);
                                            packetWriter.WriteInt32(instance2.Data4);
                                            packetWriter.WriteInt32(instance2.Data5);
                                            packetWriter.WriteInt32(instance2.Data6);
                                            packetWriter.WriteInt32(instance2.ConditionalBranch.CommandList);
                                            packetWriter.WriteInt32(instance2.ConditionalBranch.Condition);
                                            packetWriter.WriteInt32(instance2.ConditionalBranch.Data1);
                                            packetWriter.WriteInt32(instance2.ConditionalBranch.Data2);
                                            packetWriter.WriteInt32(instance2.ConditionalBranch.Data3);
                                            packetWriter.WriteInt32(instance2.ConditionalBranch.ElseCommandList);
                                            packetWriter.WriteInt32(instance2.MoveRouteCount);
                                            if (instance2.MoveRouteCount > 0)
                                            {
                                                for (int w = 0, count5 = instance2.MoveRouteCount; w < count5; w++)
                                                {
                                                    packetWriter.WriteInt32(instance2.MoveRoute[w].Index);
                                                    packetWriter.WriteInt32(instance2.MoveRoute[w].Data1);
                                                    packetWriter.WriteInt32(instance2.MoveRoute[w].Data2);
                                                    packetWriter.WriteInt32(instance2.MoveRoute[w].Data3);
                                                    packetWriter.WriteInt32(instance2.MoveRoute[w].Data4);
                                                    packetWriter.WriteInt32(instance2.MoveRoute[w].Data5);
                                                    packetWriter.WriteInt32(instance2.MoveRoute[w].Data6);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        Network.Send(packetWriter);
    }

    public static void SendMapRespawn()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CMapRespawn);

        Network.Send(packetWriter);
    }


    public static void SendRequestSwitchesAndVariables()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestSwitchesAndVariables);

        Network.Send(packetWriter);
    }

    public static void SendSwitchesAndVariables()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CSwitchesAndVariables);

        for (var i = 0; i < Core.Globals.Variables.MaxSwitches; i++)
        {
            packetWriter.WriteString(Event.Switches[i]);
        }

        for (var i = 0; i < Core.Globals.Variables.MaxVariables; i++)
        {
            packetWriter.WriteString(Event.Variables[i]);
        }

        Network.Send(packetWriter);
    }


    public static void SendPartyRequest(string name)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestParty);
        packetWriter.WriteString(name);

        Network.Send(packetWriter);
    }

    public static void SendAcceptParty()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CAcceptParty);

        Network.Send(packetWriter);
    }

    public static void SendDeclineParty()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CDeclineParty);

        Network.Send(packetWriter);
    }

    public static void SendLeaveParty()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CLeaveParty);

        Network.Send(packetWriter);
    }

    public static void SendPartyChatMsg(string text)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteInt32((int)Packets.ClientPackets.CPartyChatMsg);
        packetWriter.WriteString(text);

        Network.Send(packetWriter);
    }

    public static void SendDepositItem(int index, int amount)
    {
        var packetWriter = new PacketWriter(12);

        packetWriter.WriteEnum(Packets.ClientPackets.CDepositItem);
        packetWriter.WriteInt32(index);
        packetWriter.WriteInt32(amount);

        Network.Send(packetWriter);
    }

    public static void SendWithdrawItem(byte index, int amount)
    {
        var packetWriter = new PacketWriter(9);

        packetWriter.WriteEnum(Packets.ClientPackets.CWithdrawItem);
        packetWriter.WriteByte(index);
        packetWriter.WriteInt32(amount);

        Network.Send(packetWriter);
    }

    public static void SendChangeBankSlots(int oldSlot, int newSlot)
    {
        var packetWriter = new PacketWriter(12);

        packetWriter.WriteEnum(Packets.ClientPackets.CChangeBankSlots);
        packetWriter.WriteInt32(oldSlot);
        packetWriter.WriteInt32(newSlot);

        Network.Send(packetWriter);
    }

    public static void SendCloseBank()
    {
        if (WindowManager.Windows[WindowManager.GetWindowIndex("winBank")].Visible == true)
        {
            WindowManager.HideWindow(WindowManager.GetWindowIndex("winBank"));
            WindowManager.HideWindow(WindowManager.GetWindowIndex("winDescription"));
        }

        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CCloseBank);

        Network.Send(packetWriter);

        GameState.InBank = false;
    }

    public static void SendRequestEditProjectiles()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestEditProjectile);

        Network.Send(packetWriter);
    }

    public static void SendSaveProjectile(int index)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CSaveProjectile);
        packetWriter.WriteInt32(index);
        packetWriter.WriteString(Projectile.Instance[index].Name);
        packetWriter.WriteInt32(Projectile.Instance[index].Sprite);
        packetWriter.WriteInt32(Projectile.Instance[index].Range);
        packetWriter.WriteInt32(Projectile.Instance[index].Speed);
        packetWriter.WriteInt32(Projectile.Instance[index].Damage);
        packetWriter.WriteInt32(Projectile.Instance[index].Animation);

        Network.Send(packetWriter);
    }

    public static void SendRequestProjectile(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestProjectile);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void SendClearProjectile(int index, int collisionindex, byte collisionType, int collisionZone)
    {
        var packetWriter = new PacketWriter(20);

        packetWriter.WriteEnum(Packets.ClientPackets.CClearProjectile);
        packetWriter.WriteInt32(index);
        packetWriter.WriteInt32(collisionindex);
        packetWriter.WriteInt32(collisionType);
        packetWriter.WriteInt32(collisionZone);

        Network.Send(packetWriter);
    }
}