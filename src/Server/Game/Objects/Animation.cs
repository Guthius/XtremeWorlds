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
using static Core.Globals.Commands;
using Type = Core.Globals.Type;
using Core.Interfaces;
using Core.Objects;

namespace Server;

public class Animation : AnimationBase, IAsyncData
{
    private static void EnsureSize(int size)
    {
        if (size <= 0)
        {
            return;
        }

        if (Animation.Instance.Count >= size)
        {
            return;
        }

        lock (Animation.Instance)
        {
            while (Animation.Instance.Count < size)
            {
                Animation.Instance.Add(new Animation());
            }
        }
    }

    public static void OnSave(int index)
    {
        var json = JsonConvert.SerializeObject(Animation.Instance[index]);

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
        EnsureSize(Variables.MaxAnimations);
        return Parallel.ForEachAsync(Enumerable.Range(0, Variables.MaxAnimations), OnLoadAsync);
    }

    public static async ValueTask OnLoadAsync(int index, CancellationToken cancellationToken)
    {
        EnsureSize(Variables.MaxAnimations);
        var data = await Database.SelectRowAsync(index, "animation", "data");
        if (data is null)
        {
            OnClear(index);
            return;
        }

        var animationData = JObject.FromObject(data).ToObject<Animation>();

        EnsureSize(index + 1);
        Animation.Instance[index] = animationData ?? new Animation();
    }
}