using Core.Configurations;
using Core.Globals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;

namespace Client.Game.UI;

public static class UIScript
{
    public static dynamic? Instance { get; private set; }

    private static string UserLogsDir
    {
        get
        {
            // Never write logs into the .app bundle; users often move it to /Applications (read-only).
            // Use a per-user writable location instead.
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(baseDir))
            {
                baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            }

            if (string.IsNullOrWhiteSpace(baseDir))
            {
                baseDir = Path.GetTempPath();
            }

            return Path.Combine(baseDir, "XtremeWorlds", "Logs");
        }
    }

    private static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(UserLogsDir);
            File.AppendAllText(Path.Combine(UserLogsDir, "uiscript.log"), $"{DateTime.Now:O} {message}{Environment.NewLine}");
        }
        catch
        {
            // Best-effort logging only.
        }

        Console.WriteLine(message);
    }

    private static IEnumerable<MetadataReference> GetDomainReferences()
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm.IsDynamic)
            {
                continue;
            }

            string? loc;
            try
            {
                loc = asm.Location;
            }
            catch
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(loc))
            {
                continue;
            }

            yield return MetadataReference.CreateFromFile(loc);
        }
    }

    private static object? CompileAndCreateInstance(string code, string? preferredTypeName)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        var compilation = CSharpCompilation.Create(
            assemblyName: $"XtremeWorlds.UIScript.{Guid.NewGuid():N}",
            syntaxTrees: new[] { syntaxTree },
            references: GetDomainReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release));

        using var peStream = new MemoryStream();
        using var pdbStream = new MemoryStream();
        var emitResult = compilation.Emit(peStream, pdbStream);
        if (!emitResult.Success)
        {
            var errors = emitResult.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString())
                .ToArray();

            Log("[UIScript] Roslyn compile errors:\n" + string.Join("\n", errors));
            return null;
        }

        peStream.Position = 0;
        var asm = Assembly.Load(peStream.ToArray());

        System.Type? uiType = null;
        if (!string.IsNullOrWhiteSpace(preferredTypeName))
        {
            uiType = asm.GetType(preferredTypeName!, throwOnError: false, ignoreCase: false);
        }

        // Fallback: pick the first public class with UI entrypoints.
        uiType ??= asm.GetTypes().FirstOrDefault(t => t.IsClass && t.IsPublic &&
                                                     t.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                                         .Any(m => m.Name.StartsWith("UpdateWindow_", StringComparison.Ordinal)));

        if (uiType is null)
        {
            Log("[UIScript] Compiled UI script assembly contained no suitable type to instantiate.");
            return null;
        }

        return Activator.CreateInstance(uiType);
    }

    public static void Load()
    {
        var path = Path.Combine(DataPath.Skins, SettingsManager.Instance.Skin + ".cs");
        if (!File.Exists(path))
        {
            Log($"[UIScript] Script not found: {path}");
            return;
        }

        try
        {
            var code = File.ReadAllText(path);

            var preferredTypeName = SettingsManager.Instance.Skin;
            var instance = CompileAndCreateInstance(code, preferredTypeName);
            if (instance is not null)
            {
                Instance = instance;
                Log($"[UIScript] Loaded UI script '{preferredTypeName}' from: {path}");
                return;
            }

            Log("[UIScript] UI script load returned null instance.");
        }
        catch (Exception ex)
        {
            Log("[UIScript] Failed to load UI script.");
            Log($"[UIScript] BaseDirectory: {AppContext.BaseDirectory}");
            Log($"[UIScript] DataPath.Asset: {DataPath.Asset}");
            Log($"[UIScript] DataPath.Skins: {DataPath.Skins}");
            Log($"[UIScript] Script path: {path}");
            Log($"[UIScript] Script exists: {File.Exists(path)}");
            Log($"[UIScript] DOTNET_ROOT: {Environment.GetEnvironmentVariable("DOTNET_ROOT")}");
            Log($"[UIScript] DOTNET_MULTILEVEL_LOOKUP: {Environment.GetEnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP")}");
            Log(ex.ToString());
        }
    }
}