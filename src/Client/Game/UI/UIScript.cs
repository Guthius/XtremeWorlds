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

            Console.WriteLine("[UIScript] Roslyn compile errors:\n" + string.Join("\n", errors));
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
            Console.WriteLine("[UIScript] Compiled UI script assembly contained no suitable type to instantiate.");
            return null;
        }

        return Activator.CreateInstance(uiType);
    }

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

            var preferredTypeName = SettingsManager.Instance.Skin;
            var instance = CompileAndCreateInstance(code, preferredTypeName);
            if (instance is not null)
            {
                Instance = instance;
                Console.WriteLine($"[UIScript] Loaded UI script '{preferredTypeName}' from: {path}");
                return;
            }

            Console.WriteLine("[UIScript] UI script load returned null instance.");
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
            Console.WriteLine(ex.ToString());
        }
    }
}