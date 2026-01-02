using Core;
using Core.Globals;
using Core.Interfaces;
using Core.Net;
using Core.Objects;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Server.Game;
using Server.Game.Net;
using Server.Net;
using static Core.Globals.Commands;
using static Core.Net.Packets;

namespace Server;

public class Npc : NpcBase, IAsyncData
{
    private static void EnsureSize(int size)
    {
        if (size <= 0)
        {
            return;
        }

        if (Npc.Instance.Count >= size)
        {
            return;
        }

        lock (Npc.Instance)
        {
            while (Npc.Instance.Count < size)
            {
                Npc.Instance.Add(new Npc());
            }
        }
    }

    public static Task OnLoadAllAsync()
    {
        EnsureSize(Core.Globals.Variables.MaxNpcs);
        return Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Variables.MaxNpcs), OnLoadAsync);
    }

    public static void OnSave(int npcNum)
    {
        string json = JsonConvert.SerializeObject(Npc.Instance[(int)npcNum]).ToString();

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

        var npcData = JObject.FromObject(data).ToObject<Npc>();

        EnsureSize(index + 1);
        Npc.Instance[index] = npcData ?? new Npc();
    }
}