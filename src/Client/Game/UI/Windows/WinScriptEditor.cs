using Client.Net;
using Core.Globals;
using System;
using System.Collections.Generic;
using System.Text;
using Client;
using System.IO;

namespace Client.Game.UI.Windows;
public static class WinScriptEditor
{   
    public static void OpenScript()
    {
        try
        {
            var dir = Path.GetDirectoryName(Script.TempFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllLines(Script.TempFile, Data.Script.Code ?? Array.Empty<string>());

            if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = Script.TempFile,
                    UseShellExecute = true
                });
            }
            else if (OperatingSystem.IsLinux())
            {
                // Use xdg-open to launch the associated application on Linux
                System.Diagnostics.Process.Start("xdg-open", Script.TempFile);
            }
            else
            {
                Interaction.MsgBox("Unsupported platform for automatic script opening.");
            }
        }
        catch (Exception ex)
        {
            Interaction.MsgBox($"Failed to open script: {ex.Message}");
        }
    }

    public static void SaveScript()
    {
        if (!File.Exists(Script.TempFile))
        {
            Interaction.MsgBox("Open a script before saving.");
            return;
        }
        try
        {
            Data.Script.Code = File.ReadAllLines(Script.TempFile);
            Sender.SendSaveScript();
        }
        catch (Exception ex)
        {
            Interaction.MsgBox($"Failed to save script: {ex.Message}");
        }
    }
}
