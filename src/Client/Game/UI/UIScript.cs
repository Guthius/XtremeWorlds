using Core.Configurations;
using Core.Globals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Runtime.InteropServices;

namespace Client.Game.UI;

public static class UIScript
{
    public static dynamic? Instance { get; private set; }

    private static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(AppContext.BaseDirectory + "Logs");
            File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "Logs", "uiscript.log"), $"{DateTime.Now:O} {message}{Environment.NewLine}");
        }
        catch
        {
            try
            {
                Directory.CreateDirectory(AppContext.BaseDirectory + "Logs");
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "Logs", "uiscript.log"), $"{DateTime.Now:O} {message}{Environment.NewLine}");
            }
            catch
            {
            }
        }

        Console.WriteLine(message);
    }

    private static List<MetadataReference> GetReferences()
    {
        // Roslyn needs references to the platform assemblies (System.*, Microsoft.*) AND
        // to app/game assemblies. In some hosts, TRUSTED_PLATFORM_ASSEMBLIES can be empty,
        // so prefer enumerating the app bundle's Resources directory (for macOS .app),
        // then fall back to runtime directory and loaded assemblies.

        var refs = new HashSet<string>(StringComparer.Ordinal);

        string? baseDirForLog = null;
        string? resourcesDirForLog = null;

        try
        {
            if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string tpa && !string.IsNullOrWhiteSpace(tpa))
            {
                foreach (var p in tpa.Split(Path.PathSeparator))
                {
                    if (!string.IsNullOrWhiteSpace(p) && File.Exists(p))
                    {
                        refs.Add(p);
                    }
                }
            }
        }
        catch
        {
        }

        try
        {
            var runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
            if (!string.IsNullOrWhiteSpace(runtimeDir) && Directory.Exists(runtimeDir))
            {
                foreach (var dll in Directory.GetFiles(runtimeDir, "*.dll"))
                {
                    refs.Add(dll);
                }
            }
        }
        catch
        {
        }

        try
        {
            var baseDir = AppContext.BaseDirectory;
            baseDirForLog = baseDir;
            if (!string.IsNullOrWhiteSpace(baseDir) && Directory.Exists(baseDir))
            {
                foreach (var dll in Directory.GetFiles(baseDir, "*.dll"))
                {
                    refs.Add(dll);
                }
            }

            // When packaged as a macOS .app, managed assemblies typically live in:
            //   <App>.app/Contents/Resources/*.dll
            // while AppContext.BaseDirectory points at:
            //   <App>.app/Contents/MacOS/
            var resourcesDir = Path.GetFullPath(Path.Combine(baseDir, "..", "Resources"));
            resourcesDirForLog = resourcesDir;
            if (Directory.Exists(resourcesDir))
            {
                foreach (var dll in Directory.GetFiles(resourcesDir, "*.dll"))
                {
                    refs.Add(dll);
                }
            }
        }
        catch
        {
        }

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

            refs.Add(loc);
        }

        Log($"[UIScript] Roslyn reference path count: {refs.Count} (BaseDirectory='{baseDirForLog}', Resources='{resourcesDirForLog}')");

        var references = new List<MetadataReference>(refs.Count);
        foreach (var path in refs)
        {
            try
            {
                references.Add(MetadataReference.CreateFromFile(path));
            }
            catch (Exception ex)
            {
                Log($"[UIScript] Skipping reference '{path}': {ex.GetType().Name}: {ex.Message}");
            }
        }

        Log($"[UIScript] Roslyn metadata reference count: {references.Count}");
        return references;
    }

    private static object? CompileAndCreateInstance(string code, string? preferredTypeName)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        var references = GetReferences();

        var compilation = CSharpCompilation.Create(
            assemblyName: $"XtremeWorlds.UIScript.{Guid.NewGuid():N}",
            syntaxTrees: new[] { syntaxTree },
            references: references,
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