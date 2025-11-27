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

namespace Server;

public static class Animation
{
    public static void Save(int animationNum)
    {
        var json = JsonConvert.SerializeObject(Data.Animation[animationNum]);

        if (Database.RowExists(animationNum, "animation"))
        {
            Database.UpdateRow(animationNum, json, "animation", "data");
        }
        else
        {
            Database.InsertRow(animationNum, json, "animation");
        }
    }

    public static Task LoadAllAsync()
    {
        return Parallel.ForEachAsync(Enumerable.Range(0, Core.Globals.Variables.MaxAnimations), LoadAsync);
    }

    private static async ValueTask LoadAsync(int animationNum, CancellationToken cancellationToken)
    {
        var data = await Database.SelectRowAsync(animationNum, "animation", "data");
        if (data is null)
        {
            Clear(animationNum);
            return;
        }

        var animationData = JObject.FromObject(data).ToObject<Type.Animation>();

        Data.Animation[animationNum] = animationData;
    }

    private static void Clear(int animationNum)
    {
        Data.Animation[animationNum].Name = "";
        Data.Animation[animationNum].Sound = "";
        Data.Animation[animationNum].Sprite = [0, 0];
        Data.Animation[animationNum].Frames = [0, 0];
        Data.Animation[animationNum].LoopCount = [0, 0];
        Data.Animation[animationNum].LoopTime = [0, 0];
    }
}