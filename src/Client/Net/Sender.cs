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

    private const int HotbarUseThrottle = 250;
    private static readonly int[] LastHotbarUseTick = new int[Core.Globals.Variables.MaxHotbar];

    public static void AddChar(string name, int sex, int job)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CAddChar);
        packetWriter.WriteByte(GameState.Char);
        packetWriter.WriteString(name);
        packetWriter.WriteInt32(sex);
        packetWriter.WriteInt32(job);

        Network.Send(packetWriter);
    }

    public static void UseChar(byte slot)
    {
        var packetWriter = new PacketWriter(5);

        packetWriter.WriteEnum(Packets.ClientPackets.CUseChar);
        packetWriter.WriteByte(slot);

        Network.Send(packetWriter);
    }

    public static void DelChar(byte slot)
    {
        var packetWriter = new PacketWriter(5);

        packetWriter.WriteEnum(Packets.ClientPackets.CDelChar);
        packetWriter.WriteByte(slot);

        Network.Send(packetWriter);
    }

    public static void Logout()
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

    public static void Login(string username, string password)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CLogin);
        packetWriter.WriteBytes(Encrypt(Encoding.UTF8.GetBytes(username)));
        packetWriter.WriteBytes(Encrypt(Encoding.UTF8.GetBytes(password)));
        packetWriter.WriteBytes(Encrypt(Encoding.UTF8.GetBytes(GetVersion())));

        Network.Send(packetWriter);
    }

    public static void Register(string username, string password)
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

    public static void PlayerMove()
    {
        var packetWriter = new PacketWriter(14);

        packetWriter.WriteEnum(Packets.ClientPackets.CPlayerMove);
        packetWriter.WriteByte(GetPlayerDir(GameState.MyIndex));
        packetWriter.WriteByte(Player.Instance[GameState.MyIndex].Moving);
        packetWriter.WriteInt32(Player.Instance[GameState.MyIndex].X);
        packetWriter.WriteInt32(Player.Instance[GameState.MyIndex].Y);

        Network.Send(packetWriter);
    }

    public static void StopPlayerMove()
    {
        var packetWriter = new PacketWriter(5);

        packetWriter.WriteEnum(Packets.ClientPackets.CStopPlayerMove);
        packetWriter.WriteByte(GetPlayerDir(GameState.MyIndex));

        Network.Send(packetWriter);
    }

    public static void CancelCast()
    {
        var packetWriter = new PacketWriter(4);
        packetWriter.WriteEnum(Packets.ClientPackets.CCancelCast);
        Network.Send(packetWriter);
    }

    public static void SayMessage(string text)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Core.Net.Packets.ClientPackets.CSayMessage);
        packetWriter.WriteString(text);

        Network.Send(packetWriter);
    }

    public static void Kick(string name)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CKickPlayer);
        packetWriter.WriteString(name);

        Network.Send(packetWriter);
    }

    public static void Ban(string name)
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

    public static void RequestLevelUp()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestLevelUp);

        Network.Send(packetWriter);
    }

    public static void EventChatReply(int eventId, int pageId, int reply)
    {
        var packetWriter = new PacketWriter(16);

        packetWriter.WriteEnum(Packets.ClientPackets.CEventChatReply);
        packetWriter.WriteInt32(eventId);
        packetWriter.WriteInt32(pageId);
        packetWriter.WriteInt32(reply);

        Network.Send(packetWriter);
    }

    public static void SpawnItem(int index, int amount)
    {
        var packetWriter = new PacketWriter(12);

        packetWriter.WriteEnum(Packets.ClientPackets.CSpawnItem);
        packetWriter.WriteInt32(index);
        packetWriter.WriteInt32(amount);

        Network.Send(packetWriter);
    }

    public static void SetSprite(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CSetSprite);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void SetAccess(string name, byte access)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CSetAccess);
        packetWriter.WriteString(name);
        packetWriter.WriteByte(access);

        Network.Send(packetWriter);
    }

    public static void Attack()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CAttack);

        Network.Send(packetWriter);
    }

    public static void MouseAttack(int worldPixelX, int worldPixelY)
    {
        var packetWriter = new PacketWriter(12);
        packetWriter.WriteEnum(Packets.ClientPackets.CMouseAttack);
        packetWriter.WriteInt32(worldPixelX);
        packetWriter.WriteInt32(worldPixelY);
        Network.Send(packetWriter);
    }

    public static void PlayerDir()
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CPlayerDir);
        packetWriter.WriteByte(GetPlayerDir(GameState.MyIndex));

        Network.Send(packetWriter);
    }

    public static void RequestNpc(int index)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestNpc);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void RequestSkill(int skillNum)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestSkill);
        packetWriter.WriteInt32(skillNum);

        Network.Send(packetWriter);
    }

    public static void TrainStat(int stat)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CTrainStat);
        packetWriter.WriteInt32(stat);

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

    public static void AdminMessage(string text)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CAdminMessage);
        packetWriter.WriteString(text);

        Network.Send(packetWriter);
    }

    public static void WhosOnline()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CWhosOnline);

        Network.Send(packetWriter);
    }

    public static void PlayerInfo(string name)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CPlayerInfoRequest);
        packetWriter.WriteString(name);

        Network.Send(packetWriter);
    }

    public static void MotdChange(string message)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CSetMotd);
        packetWriter.WriteString(message);

        Network.Send(packetWriter);
    }

    public static void BanList()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CBanList);

        Network.Send(packetWriter);
    }

    public static void BanDestroy()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CBanDestroy);

        Network.Send(packetWriter);
    }

    public static void ChangeInvSlots(int oldSlot, int newSlot)
    {
        var buffer = new PacketWriter(4);

        buffer.WriteEnum(Packets.ClientPackets.CSwapInvSlots);
        buffer.WriteInt32(oldSlot);
        buffer.WriteInt32(newSlot);

        Network.Send(buffer);
    }

    public static void ChangeSkillSlots(int oldSlot, int newSlot)
    {
        var packetWriter = new PacketWriter(12);

        packetWriter.WriteEnum(Packets.ClientPackets.CSwapSkillSlots);
        packetWriter.WriteInt32(oldSlot);
        packetWriter.WriteInt32(newSlot);

        Network.Send(packetWriter);
    }

    public static void UseItem(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CUseItem);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void DropItem(int invSlot, int amount)
    {
        if (GameState.InBank || GameState.InShop >= 0)
        {
            return;
        }

        if (invSlot < 0 || invSlot > Core.Globals.Variables.MaxInventory)
        {
            return;
        }

        if (Player.Instance[GameState.MyIndex].Inventory[invSlot].Num < 0 ||
            Player.Instance[GameState.MyIndex].Inventory[invSlot].Num > Core.Globals.Variables.MaxItems)
        {
            return;
        }

        if (Item.Instance[GetPlayerInv(GameState.MyIndex, invSlot)].Type == (byte) ItemCategory.Currency ||
            Item.Instance[GetPlayerInv(GameState.MyIndex, invSlot)].Stackable == 1)
        {
            if (amount < 0 || amount > Player.Instance[GameState.MyIndex].Inventory[invSlot].Value)
            {
                return;
            }
        }

        var packetWriter = new PacketWriter(12);

        packetWriter.WriteEnum(Packets.ClientPackets.CMapDropItem);
        packetWriter.WriteInt32(invSlot);
        packetWriter.WriteInt32(amount);

        Network.Send(packetWriter);
    }

    public static void PlayerSearch(int curX, int curY, byte rClick)
    {
        if (!GameLogic.IsInBounds())
        {
            return;
        }

        var packetWriter = new PacketWriter(16);

        packetWriter.WriteEnum(Packets.ClientPackets.CSearch);
        packetWriter.WriteInt32(curX);
        packetWriter.WriteInt32(curY);
        packetWriter.WriteInt32((int)rClick);

        Network.Send(packetWriter);
    }

    public static void AdminWarp(int x, int y)
    {
        var packetWriter = new PacketWriter(12);

        packetWriter.WriteEnum(Packets.ClientPackets.CAdminWarp);
        packetWriter.WriteInt32(x);
        packetWriter.WriteInt32(y);

        Network.Send(packetWriter);
    }

    public static void Unequip(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CUnequip);
        packetWriter.WriteInt32(index);
        Network.Send(packetWriter);
    }

    public static void ForgetSkill(int index)
    {
        // Check for subscript out of range
        if (index < 0 || index > Core.Globals.Variables.MaxPlayerSkills)
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

    public static void RequestMapReport()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CMapReport);

        Network.Send(packetWriter);
    }

    public static void RequestAdmin()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CAdmin);

        Network.Send(packetWriter);
    }

    public static void UseEmote(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CEmote);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void RequestEditResource()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestEditResource);

        Network.Send(packetWriter);
    }

    public static void SaveResource(int index)
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

    public static void RequestEditNpc()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestEditNpc);

        Network.Send(packetWriter);
    }

    public static void SaveNpc(int index)
    {
        var packetWriter = new PacketWriter();

        if (index < 0 || index >= Npc.Instance.Count)
        {
            return;
        }

        var npc = Npc.Instance[index];

        packetWriter.WriteEnum(Packets.ClientPackets.CSaveNpc);
        packetWriter.WriteInt32(index);
        packetWriter.WriteInt32(npc.Animation);
        packetWriter.WriteString(npc.AttackSay);
        packetWriter.WriteByte(npc.Behavior);

        for (var i = 0; i < Core.Globals.Variables.MaxDropItems; i++)
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

        for (var i = 0; i < Core.Globals.Variables.MaxNpcSkills; i++)
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

    public static void RequestEditSkill()
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestEditSkill);

        Network.Send(packetWriter);
    }

    public static void SaveSkill(int index)
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
        packetWriter.WriteByte(Skill.Instance[index].Dir);
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
        
        packetWriter.WriteInt32(Skill.Instance[index].SpCost);
        Network.Send(packetWriter);
    }

    public static void SaveShop(int index)
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

        for (var i = 0; i < Core.Globals.Variables.MaxTrades; i++)
        {
            packetWriter.WriteInt32(Shop.Instance[index].TradeItem[i].CostItem);
            packetWriter.WriteInt32(Shop.Instance[index].TradeItem[i].CostValue);
            packetWriter.WriteInt32(Shop.Instance[index].TradeItem[i].Item);
            packetWriter.WriteInt32(Shop.Instance[index].TradeItem[i].ItemValue);
        }

        Network.Send(packetWriter);
    }

    public static void RequestEditShop()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestEditShop);

        Network.Send(packetWriter);
    }

    public static void SaveAnimation(int index)
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

    public static void RequestEditAnimation()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestEditAnimation);

        Network.Send(packetWriter);
    }

    public static void RequestEditJob()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestEditJob);

        Network.Send(packetWriter);
    }

    public static void SaveJob(int index)
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

        for (var i = 0; i < Core.Globals.Variables.MaxStartItems; i++)
        {
            packetWriter.WriteInt32(Job.Instance[index].StartItem[i]);
            packetWriter.WriteInt32(Job.Instance[index].StartValue[i]);
        }

        for (var i = 0; i < Core.Globals.Variables.MaxStartSkills; i++)
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

    public static void SaveItem(int index)
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
        packetWriter.WriteByte(Item.Instance[index].Mastery);
        packetWriter.WriteString(Item.Instance[index].Name);
        packetWriter.WriteInt32(Item.Instance[index].Paperdoll);
        packetWriter.WriteInt32(Item.Instance[index].Icon);
        packetWriter.WriteInt32(Item.Instance[index].Price);
        packetWriter.WriteByte(Item.Instance[index].Rarity);
        packetWriter.WriteInt32(Item.Instance[index].Speed);

        packetWriter.WriteByte(Item.Instance[index].Stackable);
        packetWriter.WriteString(Item.Instance[index].Description);
        for (var i = 0; i < StatCount; i++)
        {
            packetWriter.WriteInt32(Item.Instance[index].StatReq[i]);
        }

        packetWriter.WriteByte(Item.Instance[index].Type);
        packetWriter.WriteByte(Item.Instance[index].SubType);
        packetWriter.WriteByte(Item.Instance[index].ItemLevel);

        packetWriter.WriteByte(Item.Instance[index].KnockBack);
        packetWriter.WriteByte(Item.Instance[index].KnockBackTiles);
        packetWriter.WriteInt32(Item.Instance[index].Projectile);
        packetWriter.WriteInt32(Item.Instance[index].Ammo);

        packetWriter.WriteByte(Item.Instance[index].CommonEventType);
        packetWriter.WriteInt32(Item.Instance[index].CommonEventData1);
        packetWriter.WriteInt32(Item.Instance[index].CommonEventData2);

        Network.Send(packetWriter);
    }

    public static void RequestEditItem()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestEditItem);

        Network.Send(packetWriter);
    }

    public static void CloseEditor()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CCloseEditor);

        Network.Send(packetWriter);
    }

    public static void SetHotbarSlot(int type, int newSlot, int oldSlot, int index)
    {
        var packetWriter = new PacketWriter(20);

        packetWriter.WriteEnum(Packets.ClientPackets.CSetHotbarSlot);
        packetWriter.WriteInt32(type);
        packetWriter.WriteInt32(newSlot);
        packetWriter.WriteInt32(oldSlot);
        packetWriter.WriteInt32(index);
        Network.Send(packetWriter);
    }

    public static void DeleteHotbar(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CDeleteHotbarSlot);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void UseHotbarSlot(int index)
    {
        if (index < 0 || index >= Core.Globals.Variables.MaxHotbar)
        {
            return;
        }

        var now = Client.General.GetTickCount();
        var last = LastHotbarUseTick[index];
        if (last > 0 && now - last < HotbarUseThrottle)
        {
            return;
        }

        LastHotbarUseTick[index] = now;

        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CUseHotbarSlot);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void LearnSkill(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CSkillLearn);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void Cast(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CCast);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void RequestMoral(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestMoral);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void RequestEditMoral()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestEditMoral);

        Network.Send(packetWriter);
    }

    public static void SaveMoral(int index)
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

    public static void CloseShop()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CCloseShop);

        Network.Send(packetWriter);
    }

    public static void RequestEditScript(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestEditScript);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void SaveScript()
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CSaveScript);
        packetWriter.WriteString(string.Join(Environment.NewLine, Data.Script.Code));

        Network.Send(packetWriter);
    }

    public static void RequestItem(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestItem);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void RequestShop(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestShop);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void BuyItem(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CBuyItem);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void SellItem(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CSellItem);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void RequestAnimation(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestAnimation);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void RequestResource(int resourceNum)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteInt32((int)Packets.ClientPackets.CRequestResource);
        packetWriter.WriteInt32(resourceNum);

        Network.Send(packetWriter);
    }


    public static void AcceptTrade()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CAcceptTrade);

        Network.Send(packetWriter);
    }

    public static void DeclineTrade()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CDeclineTrade);

        Network.Send(packetWriter);
    }

    public static void TradeRequest(string name)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CTradeInvite);
        packetWriter.WriteString(name);

        Network.Send(packetWriter);

    }

    public static void HandleTradeInvite(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CHandleTradeInvite);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void TradeItem(int index, int amount)
    {
        var packetWriter = new PacketWriter(12);

        packetWriter.WriteEnum(Packets.ClientPackets.CTradeItem);
        packetWriter.WriteInt32(index);
        packetWriter.WriteInt32(amount);

        Network.Send(packetWriter);
    }

    public static void UntradeItem(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CUntradeItem);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void PlayerRequestNewMap()
    {
        if (GameState.GettingMap)
        {
            return;
        }

        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestNewMap);
        packetWriter.WriteInt32((int)GetPlayerDir(GameState.MyIndex));

        Network.Send(packetWriter);

        GameState.GettingMap = true;
        GameState.CanMoveNow = false;
    }

    public static void RequestEditMap()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestEditMap);

        Network.Send(packetWriter);
    }

    public static void Map()
    {
        int x;
        int y;
        int i;

        GameState.CanMoveNow = false;

        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CSaveMap);
        packetWriter.WriteString(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Name);
        packetWriter.WriteString(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Music);
        packetWriter.WriteInt32((int)Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Moral);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tileset);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Up);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Down);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Left);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Right);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].BootMap);
        packetWriter.WriteInt32((int)Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].BootX);
        packetWriter.WriteInt32((int)Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].BootY);
        packetWriter.WriteInt32((int)Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxX);
        packetWriter.WriteInt32((int)Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxY);
        packetWriter.WriteInt32((int)Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Weather);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Fog);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].WeatherIntensity);
        packetWriter.WriteInt32((int)Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].FogOpacity);
        packetWriter.WriteInt32((int)Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].FogSpeed);
        packetWriter.WriteBoolean(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MapTint);
        packetWriter.WriteInt32((int)Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MapTintR);
        packetWriter.WriteInt32((int)Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MapTintG);
        packetWriter.WriteInt32((int)Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MapTintB);
        packetWriter.WriteInt32((int)Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MapTintA);
        packetWriter.WriteByte(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Panorama);
        packetWriter.WriteByte(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Parallax);
        packetWriter.WriteByte(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Brightness);
        packetWriter.WriteBoolean(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].NoRespawn);
        packetWriter.WriteBoolean(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Indoors);
        packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Shop);

        // Per-map camera zoom bounds
        packetWriter.WriteSingle(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MinZoom);
        packetWriter.WriteSingle(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].MaxZoom);

        for (i = 0; i < Core.Globals.Variables.MaxMapNpcs; i++)
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
                packetWriter.WriteInt32((int)Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].DirBlock);

                int layerCount = Enum.GetValues<MapLayer>().Length;
                for (i = 0; i < layerCount; i++)
                {
                    packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Layer[i].Tileset);
                    packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Layer[i].X);
                    packetWriter.WriteInt32(Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Layer[i].Y);
                    packetWriter.WriteInt32((int)Client.Map.Instance[GetPlayerMap(GameState.MyIndex)].Tile[x, y].Layer[i].AutoTile);
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

                            packetWriter.WriteByte(instance1.IdleAnim);
                            packetWriter.WriteByte(instance1.DirFix);
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

    public static void MapRespawn()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CMapRespawn);

        Network.Send(packetWriter);
    }

    public static void RespawnNow()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRespawnNow);

        Network.Send(packetWriter);
    }


    public static void RequestSwitchesAndVariables()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestSwitchesAndVariables);

        Network.Send(packetWriter);
    }

    public static void SwitchesAndVariables()
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


    public static void PartyRequest(string name)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestParty);
        packetWriter.WriteString(name);

        Network.Send(packetWriter);
    }

    public static void AcceptParty()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CAcceptParty);

        Network.Send(packetWriter);
    }

    public static void DeclineParty()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CDeclineParty);

        Network.Send(packetWriter);
    }

    public static void LeaveParty()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CLeaveParty);

        Network.Send(packetWriter);
    }

    public static void PartyChatMsg(string text)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteInt32((int)Packets.ClientPackets.CPartyChatMsg);
        packetWriter.WriteString(text);

        Network.Send(packetWriter);
    }

    public static void DepositItem(int index, int amount)
    {
        var packetWriter = new PacketWriter(12);

        packetWriter.WriteEnum(Packets.ClientPackets.CDepositItem);
        packetWriter.WriteInt32(index);
        packetWriter.WriteInt32(amount);

        Network.Send(packetWriter);
    }

    public static void WithdrawItem(byte index, int amount)
    {
        var packetWriter = new PacketWriter(9);

        packetWriter.WriteEnum(Packets.ClientPackets.CWithdrawItem);
        packetWriter.WriteByte(index);
        packetWriter.WriteInt32(amount);

        Network.Send(packetWriter);
    }

    public static void ChangeBankSlots(int oldSlot, int newSlot)
    {
        var packetWriter = new PacketWriter(12);

        packetWriter.WriteEnum(Packets.ClientPackets.CChangeBankSlots);
        packetWriter.WriteInt32(oldSlot);
        packetWriter.WriteInt32(newSlot);

        Network.Send(packetWriter);
    }

    public static void CloseBank()
    {
        if (WindowManager.Windows[WindowManager.GetWindow("winBank")].Visible == true)
        {
            WindowManager.HideWindow(WindowManager.GetWindow("winBank"));
            WindowManager.HideWindow(WindowManager.GetWindow("winDescription"));
        }

        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CCloseBank);

        Network.Send(packetWriter);

        GameState.InBank = false;
    }

    public static void RequestEditProjectiles()
    {
        var packetWriter = new PacketWriter(4);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestEditProjectile);

        Network.Send(packetWriter);
    }

    public static void SaveProjectile(int index)
    {
        var packetWriter = new PacketWriter();

        packetWriter.WriteEnum(Packets.ClientPackets.CSaveProjectile);
        packetWriter.WriteInt32(index);
        packetWriter.WriteString(Projectile.Instance[index].Name);
        packetWriter.WriteInt32(Projectile.Instance[index].Sprite);
        packetWriter.WriteByte(Projectile.Instance[index].Range);
        packetWriter.WriteInt32(Projectile.Instance[index].Speed);
        packetWriter.WriteInt32(Projectile.Instance[index].Damage);
        packetWriter.WriteInt32(Projectile.Instance[index].Animation);

        Network.Send(packetWriter);
    }

    public static void RequestProjectile(int index)
    {
        var packetWriter = new PacketWriter(8);

        packetWriter.WriteEnum(Packets.ClientPackets.CRequestProjectile);
        packetWriter.WriteInt32(index);

        Network.Send(packetWriter);
    }

    public static void ClearProjectile(int index, int collisionindex, int collisionType, int collisionZone)
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