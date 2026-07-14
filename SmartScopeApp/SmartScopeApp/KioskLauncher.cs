using System; using System.Diagnostics; using System.IO; using System.Windows.Forms;
namespace SmartScopeApp
{
    public static class KioskLauncher
    {
        private const string APP_NAME    = "SmartScope";
        private const string REGISTRY_RUN = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        public static void EnableAutoStart()  { try { using var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGISTRY_RUN, true); k?.SetValue(APP_NAME, $"\"{Application.ExecutablePath}\""); } catch {} }
        public static void DisableAutoStart() { try { using var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGISTRY_RUN, true); k?.DeleteValue(APP_NAME, false); } catch {} }
        public static void SuppressDistractingApps()
        {
            foreach (var name in new[]{"Discord","Spotify","Steam","EpicGamesLauncher","Slack"})
                try { foreach (var p in Process.GetProcessesByName(name)) p.Kill(); } catch {}
        }
        public static void LogSession(string student, DateTime start, DateTime end, string subject)
        {
            try {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"SmartScope","Logs");
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir,$"sessions_{DateTime.Now:yyyy-MM}.txt"),
                    $"[{start:yyyy-MM-dd HH:mm}] {student} | {subject} | {(end-start).TotalMinutes:F0} min\n");
            } catch {}
        }
    }
}
