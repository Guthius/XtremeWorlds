using Client.Net;
using Core.Globals;
using Eto.Drawing;
using Eto.Forms;
using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;

namespace Client;

public static class Program
{
    private static UITimer? _uiTimer;
    private static bool _editorsDisposed;
    public static bool IsEtoAvailable => !RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && Application.Instance != null;

    [STAThread]
    public static void Main()
    {
        // On macOS, don't use Eto.Forms at all; run the game on the main thread
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            RunGame();
            return;
        }

        // Other platforms: start game loop on background thread so Eto UI thread stays responsive
        var gameThread = new System.Threading.Thread(RunGame) { IsBackground = false };
        gameThread.Start();

        Application app;

#if WINDOWS
        app = new Application(Eto.Platforms.Wpf);
#elif LINUX
        app = new Application(Eto.Platforms.Gtk);
#else
        try
        {
            app = new Application(Eto.Platform.Detect);    
        }
        catch (Exception ex)
        {
            app = new Application(Eto.Platforms.Wpf);
        }
#endif
        // Set up a timer to periodically update editor UIs
        _uiTimer = new UITimer { Interval = 0.05 }; // 50ms (~20fps) for editor UI refresh logic
        _uiTimer.Elapsed += UiTimerOnElapsed;
        _uiTimer.Start();

        app.Run();
    }

    private static void RunGame()
    {
        General.Client.Run();
    }

    private static void UiTimerOnElapsed(object? sender, EventArgs e)
    {
        if (IsEtoAvailable)
            SafeUpdateEditors();
    }

    private static void SafeUpdateEditors()
    {
        try
        {
            UpdateEditors();
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
        catch (Exception ex)
        {
            // Prevent UI timer from dying due to unexpected exceptions
            try { System.Console.WriteLine($"[UiTimer] Exception: {ex}"); } catch { }
        }
    }

    private static void UpdateEditors()
    {
    if (!IsEtoAvailable) return;
        if (GameState.InitAdminForm)
        {
            new Admin().Show();
            Sender.SendRequestMapReport();
            GameState.AdminPanel = true;
            GameState.InitAdminForm = false;
        }

        if (GameState.InitMapReport)
        {
            for (int i = 1, loopTo = GameState.MapNames.Length; i < loopTo; i++)
            {
                var admin = Admin.Instance;
                admin.lstMaps.Items.Add(new ListItem { Text = $"{i}: {GameState.MapNames[i]}" });
            }
                
            GameState.InitMapReport = false;
        }

        if (GameState.InitMapEditor)
        {
            GameState.MyEditorType = EditorType.Map;
            GameState.EditorIndex = 0;
            new Editor_Map().Show();
            GameState.CameraZoom = 1.0f;
            GameState.InitMapEditor = false;
        }

        if (GameState.InitEventEditor)
        {
            // Initialize editor state from the selected event id (GameState.EventNum)
            Event.EventEditorInit();
            new Editor_Event().Show();
            GameState.InitEventEditor = false;
        }

        if (GameState.InitAnimationEditor)
        {
            GameState.MyEditorType = EditorType.Animation;
            GameState.EditorIndex = 0;
            new Editor_Animation().Show();
            GameState.InitAnimationEditor = false;
        }

        if (GameState.InitItemEditor)
        {
            GameState.MyEditorType = EditorType.Item;
            GameState.EditorIndex = 0;
            new Editor_Item().Show();
            GameState.InitItemEditor = false;
        }

        if (GameState.InitJobEditor)
        {
            GameState.MyEditorType = EditorType.Job;
            GameState.EditorIndex = 0;
            new Editor_Job().Show();
            GameState.InitJobEditor = false;
        }

        if (GameState.InitMoralEditor)
        {
            GameState.MyEditorType = EditorType.Moral;
            GameState.EditorIndex = 0;
            new Editor_Moral().Show();
            GameState.InitMoralEditor = false;
        }

        if (GameState.InitResourceEditor)
        {
            GameState.MyEditorType = EditorType.Resource;
            GameState.EditorIndex = 0;
            new Editor_Resource().Show();
            GameState.InitResourceEditor = false;
        }

        if (GameState.InitNpcEditor)
        {
            GameState.MyEditorType = EditorType.Npc;
            GameState.EditorIndex = 0;
            new Editor_Npc().Show();
            GameState.InitNpcEditor = false;
        }

        if (GameState.InitSkillEditor)
        {
            GameState.MyEditorType = EditorType.Skill;
            GameState.EditorIndex = 0;
            new Editor_Skill().Show();
            GameState.InitSkillEditor = false;
        }

        if (GameState.InitShopEditor)
        {
            GameState.MyEditorType = EditorType.Shop;
            GameState.EditorIndex = 0;
            new Editor_Shop().Show();
            GameState.InitShopEditor = false;
        }

        if (GameState.InitProjectileEditor)
        {
            GameState.MyEditorType = EditorType.Projectile;
            GameState.EditorIndex = 0;
            new Editor_Projectile().Show();
            GameState.InitProjectileEditor = false;
        }

        if (GameState.InitScriptEditor)
        {
            GameState.MyEditorType = EditorType.Script;
            GameState.EditorIndex = 0;
            new Editor_Script().Show();
            GameState.InitScriptEditor = false;
        }
    }

    // Called when the game is exiting to stop the Eto loop cleanly
    public static void QuitEto()
    {
        try
        {
            // Stop the UI timer to avoid further callbacks during shutdown
            try { _uiTimer?.Stop(); } catch { }

            // Close all open Eto windows on the UI thread, then close the hidden root form
        if (IsEtoAvailable)
        Application.Instance?.AsyncInvoke(() =>
            {
                try
                {
                    // Close all windows except the hidden root, if present
                    foreach (var win in Application.Instance.Windows.ToList())
                    {
                        try
                        {
                            if (win.Visible)
                                win.Close();
                        }
                        catch { }
                    };
                }
                catch
                {
                    // As a fallback, request application quit
            try { Application.Instance?.Quit(); } catch { }
                }
            });
        }
        catch { }
    }
}