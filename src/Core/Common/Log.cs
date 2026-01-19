using Core.Globals;

namespace Core.Common;

public static class Log
{
    public static void Add(string message, string name)
    {
        if (!Directory.Exists(DataPath.Logs))
        {
            Directory.CreateDirectory(DataPath.Logs);
        }

        var path = Path.Combine(DataPath.Logs, name);

        try
        {
            using var stream = File.Open(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var streamWriter = new StreamWriter(stream);
            
            streamWriter.WriteLine($"{DateTime.Now:O} {message}");
        }
        catch
        {
            // ignored
        }
    }
}