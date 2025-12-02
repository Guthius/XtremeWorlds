
using System.Diagnostics;
using System.Runtime.InteropServices;

public static class Cocca
    {
        private const int SIGTERM = 15;

        [DllImport("libSystem.dylib")]
        private static extern int kill(int pid, int sig);

        public static void OnExit()
        {
            if (!OperatingSystem.IsMacOS())
            {
                Environment.Exit(0);
                return;
            }

            try
            {
                int pid = Process.GetCurrentProcess().Id;
                kill(pid, SIGTERM);
            }
            catch
            {
                // Fallback if kill fails
                Environment.Exit(0);
            }
        }
    }