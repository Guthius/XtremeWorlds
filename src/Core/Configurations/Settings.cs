using System.Text.Json;
using Core.Globals;

namespace Core.Configurations;

public class SettingsManager
{
    private const string FileName = "Settings.json";
    
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true
    };
    
    public static SettingsManager Instance { get; } = Load();
    
    public string Language { get; set; } = "English";
    public string Username { get; set; } = "";
    public bool SaveUsername { get; set; } = true;
    public string MenuMusic { get; set; } = "menu.mid";
    public bool Music { get; set; } = true;
    public bool Sound { get; set; } = true;
    public float MusicVolume { get; set; } = 100.0f;
    public float SoundVolume { get; set; } = 100.0f;
    public string MusicExt { get; set; } = ".mid";
    public string SoundExt { get; set; } = ".ogg";
    public byte Resolution { get; set; } = 1;
    public float GuiScale { get; set; } = 1.0f;
    public bool Vsync { get; set; } = true;
    public bool Fullscreen { get; set; }
    public byte CameraWidth { get; set; } = 32;
    public byte CameraHeight { get; set; } = 24;
    public bool OpenAdminPanelOnLogin { get; set; } = true;
    public byte[] ChannelState { get; set; } = new byte[] {1, 1, 1, 1, 1, 1, 1};
    public string Ip = "127.0.0.1";
    public int Port = 7001;
    public string GameName = "XtremeWorlds";
    public double TimeSpeed { get; set; }
    public bool Autotile { get; set; } = true;
    public string Skin { get; set; } = "Crystalshire";
    public string SpriteSegmentOrder { get; set; } = "idle,run,attack";
    public int IdleFrames { get; set; } = 3;
    public int RunFrames { get; set; } = 4;
    public int AttackFrames { get; set; } = 5;
    public int SpriteDirections { get; set; } = 4;
    public bool BitmapFont { get; set; } = false;
    
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