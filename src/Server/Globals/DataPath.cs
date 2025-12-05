namespace Core.Globals;

public static class DataPath
{
    public static string Config => Path.Combine(Environment.CurrentDirectory, "Config");
    public static string Logs => Path.Combine(Environment.CurrentDirectory, "Logs");
    public static string Database => Path.Combine(Environment.CurrentDirectory, "Database");
}