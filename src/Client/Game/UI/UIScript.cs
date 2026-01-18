using Core.Configurations;
using Core.Globals;
using CSScriptLib;
using System.IO;

namespace Client.Game.UI;

public static class UIScript
{
    public static dynamic? Instance { get; private set; }

    public static void Load()
    {
        var path = Path.Combine(DataPath.Skins, SettingsManager.Instance.Skin + ".cs");
        if (!File.Exists(path))
        {
            Console.WriteLine($"[UIScript] Script not found: {path}");
            return;
        }

        try
        {
            var code = File.ReadAllText(path);

            var evaluator = CSScript.RoslynEvaluator;

            CSScript.EvaluatorConfig.Engine = EvaluatorEngine.Roslyn;

            dynamic script = evaluator
                .ReferenceDomainAssemblies()
                .LoadCode(code);

            if (script is not null)
            {
                Instance = script;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[UIScript] Failed to load UI script.");
            Console.WriteLine($"[UIScript] BaseDirectory: {AppContext.BaseDirectory}");
            Console.WriteLine($"[UIScript] DataPath.Asset: {DataPath.Asset}");
            Console.WriteLine($"[UIScript] DataPath.Skins: {DataPath.Skins}");
            Console.WriteLine($"[UIScript] Script path: {path}");
            Console.WriteLine($"[UIScript] Script exists: {File.Exists(path)}");
            Console.WriteLine($"[UIScript] DOTNET_ROOT: {Environment.GetEnvironmentVariable("DOTNET_ROOT")}");
            Console.WriteLine($"[UIScript] DOTNET_MULTILEVEL_LOOKUP: {Environment.GetEnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP")}");
            Console.WriteLine(ex);
        }
    }
}