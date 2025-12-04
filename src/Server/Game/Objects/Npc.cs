using Core;
using Core.Globals;
using Core.Interfaces;
using Core.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Server.Game;
using Server.Game.Net;
using Server.Net;
using static Core.Globals.Command;
using static Core.Net.Packets;

namespace Server;

public class Npc : IData, IAsyncData
{
    public static Task OnLoadAllAsync()
    {
        return Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Variables.MaxNpcs), OnLoadAsync);
    }

    public static void OnSave(int npcNum)
    {
        string json = JsonConvert.SerializeObject(Data.Npc[(int)npcNum]).ToString();

        if (Database.RowExists(npcNum, "npc"))
        {
            Database.UpdateRow(npcNum, json, "npc", "data");
        }
        else
        {
            Database.InsertRow(npcNum, json, "npc");
        }
    }

    public static async ValueTask OnLoadAsync(int index, CancellationToken cancellationToken)
    {
        JObject data;

        data = await Database.SelectRowAsync(index, "npc", "data");
        if (data is null)
        {
            OnClear(index);
            return;
        }

        var npcData = JObject.FromObject(data).ToObject<Core.Globals.Type.Npc>();
        Data.Npc[index] = npcData;
    }

    public static void OnClear(int index)
    {
        Data.Npc[index].Name = "";
        Data.Npc[index].AttackSay = "";
        int statCount = Enum.GetValues(typeof(Stat)).Length;
        Data.Npc[index].Stat = new byte[statCount];

        for (int i = 0, loopTo = Core.Globals.Variables.MaxDropItems; i < loopTo; i++)
        {
            Data.Npc[index].DropChance = new int[Core.Globals.Variables.MaxDropItems];
            Data.Npc[index].DropItem = new int[Core.Globals.Variables.MaxDropItems];
            Data.Npc[index].DropItemValue = new int[Core.Globals.Variables.MaxDropItems];
            Data.Npc[index].Skill = new byte[Core.Globals.Variables.MaxNpcSkills];
        }
    }

    public static void OnDraw(int index)
    {
        throw new NotImplementedException();
    }

    public static void OnStream(int index)
    {
        throw new NotImplementedException();
    }

    public static void OnReset()
    {
        throw new NotImplementedException();
    }

    public static void OnLoad(int index)
    {
        throw new NotImplementedException();
    }
}