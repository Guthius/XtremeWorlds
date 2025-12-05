using Core;
using Core.Globals;
using Core.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Server.Game;
using Server.Game.Net;
using Server.Net;
using static Core.Net.Packets;
using static Core.Globals.Command;
using Type = Core.Globals.Type;
using Core.Interfaces;

namespace Server;

public class Animation : IData, IAsyncData
{
    public static void OnSave(int index)
    {
        var json = JsonConvert.SerializeObject(Data.Animation[index]);

        if (Database.RowExists(index, "animation"))
        {
            Database.UpdateRow(index, json, "animation", "data");
        }
        else
        {
            Database.InsertRow(index, json, "animation");
        }
    }

    public static Task OnLoadAllAsync()
    {
        return Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Variables.MaxAnimations), OnLoadAsync);
    }

    public static async ValueTask OnLoadAsync(int index, CancellationToken cancellationToken)
    {
        var data = await Database.SelectRowAsync(index, "animation", "data");
        if (data is null)
        {
            OnClear(index);
            return;
        }

        var animationData = JObject.FromObject(data).ToObject<Type.Animation>();

        Data.Animation[index] = animationData;
    }

    public static void OnClear(int index)
    {
        Data.Animation[index].Name = "";
        Data.Animation[index].Sound = "";
        Data.Animation[index].Sprite = [0, 0];
        Data.Animation[index].Frames = [0, 0];
        Data.Animation[index].LoopCount = [0, 0];
        Data.Animation[index].LoopTime = [0, 0];
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
        for (int i = 0; i < Data.Animation.Length; i++)
            OnClear(i);
    }

    public static void OnLoad(int index)
    {
        throw new NotImplementedException();
    }

    public static void OnUpdate(int index)
    {
        throw new NotImplementedException();
    }
}