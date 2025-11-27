using System.Text;
using Core;
using Core.Globals;
using Core.Net;
using CSScriptLib;
using Microsoft.Extensions.Logging;
using Server.Game.Net;
using static Core.Net.Packets;
using static Core.Globals.Command;

namespace Server;

public static class Script
{
    public const int MaxScriptLinesPerChunk = 100;

    public static dynamic? Instance { get; private set; }

    public static async Task LoadAsync(int playerId)
    {
        var path = System.IO.Path.Combine(DataPath.Database, "Script.cs");
        if (File.Exists(path))
        {
            Data.Script.Code = await File.ReadAllLinesAsync(path, Encoding.UTF8);
        }
        else
        {
            Data.Script.Code = [];
        }

        var script = Data.Script.Code != null && Data.Script.Code.Length > 0
            ? string.Join(Environment.NewLine, Data.Script.Code)
            : string.Empty;

        if (string.IsNullOrWhiteSpace(script))
        {
            NetworkSend.PlayerMsg(playerId, "No script code found to compile.", (int) ColorName.BrightRed);

            General.Logger.LogWarning("No script code found to compile");
            return;
        }

        try
        {
            var evaluator = CSScript.RoslynEvaluator;

            CSScript.EvaluatorConfig.Engine = EvaluatorEngine.Roslyn;

            dynamic instance = evaluator
                .ReferenceDomainAssemblies()
                .LoadCode(script);

            if (instance is not null)
            {
                Instance = instance;
                Script.Instance?.UpdateMaxValues();

                if (playerId > 0)
                {
                    NetworkSend.PlayerMsg(playerId, "Script saved successfully! If you changed any max values, you may need to restart the server.", (int) ColorName.Yellow);
                }
            }
        }
        catch (Exception ex)
        {
            if (playerId > 0)
            {
                NetworkSend.PlayerMsg(playerId, ex.Message, (int) ColorName.BrightRed);
            }

            General.Logger.LogError(ex, "[Script] Failed to load script");
        }
    }
}