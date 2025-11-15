using System;
using Client.Game.UI;
using Client.Net;
using Core.Configurations;
using Core.Globals;
using static Core.Globals.Command;
using Eto.Drawing;
using UIWindow = Client.Game.UI.Window;

namespace Client
{
    /// <summary>
    /// Admin Panel Editor - Now integrated with Crystalshire UI skin system
    /// This class maintains the singleton pattern for backward compatibility.
    /// All UI initialization is handled in Crystalshire.cs via UpdateWindow_Admin()
    /// All functionality is in WinAdmin.cs static methods.
    /// </summary>
    internal class Admin
    {
        // Singleton instance for static access
        private static Admin? _instance;
        public static Admin Instance => _instance ??= new Admin();

        // Reference to the loaded window
        private UIWindow? _window;

        public Admin()
        {
            _instance = this;
        }

        /// <summary>
        /// Gets the admin window from WindowManager if it exists
        /// </summary>
        public UIWindow? GetWindow()
        {
            // Try to fetch from WindowManager
            return WindowManager.GetWindowByName("winAdmin");
        }

        /// <summary>
        /// Initialize the admin window - called by Crystalshire.cs
        /// </summary>
        public void Initialize()
        {
            _window = GetWindow();
            if (_window == null)
            {
                Console.WriteLine("Failed to load Admin window layout");
            }
        }
    }
}

