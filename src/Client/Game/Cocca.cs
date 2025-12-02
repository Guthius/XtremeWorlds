
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Client
{
    public static class Cocca
    {
        private const int SIGTERM = 15;

        [DllImport("libSystem.dylib")]
        private static extern int kill(int pid, int sig);

        public static void OnExit()
        {
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
}