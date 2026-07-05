using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace SmartScopeApp
{
    /// <summary>
    /// Handles launching the kiosk, checking for updates,
    /// and auto-starting on Windows login.
    /// </summary>
    public static class KioskLauncher
    {
        private const string APP_NAME    = "SmartScope";
        private const string REGISTRY_RUN = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>
        /// Add SmartScope to Windows startup so it launches on login.
        /// </summary>
        public static void EnableAutoStart()
        {
            try
            {
                string exePath = Application.ExecutablePath;
                using var key  = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGISTRY_RUN, true);
                key?.SetValue(APP_NAME, $"\"{exePath}\"");
            }
            catch { /* silently fail if no registry access */ }
        }

        /// <summary>
        /// Remove SmartScope from Windows startup.
        /// </summary>
        public static void DisableAutoStart()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGISTRY_RUN, true);
                key?.DeleteValue(APP_NAME, false);
            }
            catch { }
        }

        /// <summary>
        /// Returns true if SmartScope is set to launch on Windows login.
        /// </summary>
        public static bool IsAutoStartEnabled()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REGISTRY_RUN, false);
                return key?.GetValue(APP_NAME) != null;
            }
            catch { return false; }
        }

        /// <summary>
        /// Kill any processes that could be distracting (optional, admin only).
        /// Add/remove from this list as needed.
        /// </summary>
        public static void SuppressDistractingApps()
        {
            string[] appsToClose = {
                "Discord", "Spotify", "Steam",
                "EpicGamesLauncher", "Slack", "Teams"
            };

            foreach (var name in appsToClose)
            {
                try
                {
                    foreach (var proc in Process.GetProcessesByName(name))
                        proc.Kill();
                }
                catch { /* app not running or no permission */ }
            }
        }

        /// <summary>
        /// Write a session log entry to AppData.
        /// </summary>
        public static void LogSession(string studentName, DateTime start, DateTime end, string subject)
        {
            try
            {
                string logDir  = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SmartScope", "Logs");
                Directory.CreateDirectory(logDir);

                string logFile = Path.Combine(logDir, $"sessions_{DateTime.Now:yyyy-MM}.txt");
                string line    = $"[{start:yyyy-MM-dd HH:mm}] {studentName} | {subject} | Duration: {(end-start).TotalMinutes:F0} min\n";
                File.AppendAllText(logFile, line);
            }
            catch { }
        }
    }
}
