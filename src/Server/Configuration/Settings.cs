using System.Text.Json;
using Core.Globals;

namespace XtremeWorlds.Server.Configuration;

public class SettingsManager
{
    private const string FileName = "Settings.json";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    public static SettingsManager Instance { get; } = Load();

    public int Port { get; init; } = 7001;
    public string GameName { get; init; } = "XtremeWorlds";
    public double TimeSpeed { get; set; }

    private static SettingsManager Load()
    {
        try
        {
            var path = Path.Combine(DataPath.Config, FileName);
            if (!File.Exists(path))
            {
                return CreateDefaults();
            }

            var settingsJson = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<SettingsManager>(settingsJson);

            return settings ?? new SettingsManager();
        }
        catch
        {
            return CreateDefaults();
        }
    }

    private static void Save(SettingsManager settings)
    {
        try
        {
            var path = DataPath.Config;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path = Path.Combine(path, FileName);

            var settingsJson = JsonSerializer.Serialize(settings, JsonSerializerOptions);

            File.WriteAllText(path, settingsJson);
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to save settings");
            Console.WriteLine(e);
        }
    }

    public static void Save() => Save(Instance);

    private static SettingsManager CreateDefaults()
    {
        var settings = new SettingsManager();

        Save(settings);

        return settings;
    }
}