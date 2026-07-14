using System;
using System.Text.Json;
using System.IO;

namespace SmartScopeApp
{
    /// <summary>
    /// Stores and loads session configuration (student name, class URL, duration etc.)
    /// Saved to AppData so it persists between launches.
    /// </summary>
    public class SessionConfig
    {
        public string StudentName       { get; set; } = "";
        public string StudentEmail      { get; set; } = "";
        public string ClassUrl          { get; set; } = "https://smartscope-tutoring.onrender.com/student.html";
        public string Subject           { get; set; } = "";
        public int    DurationMinutes   { get; set; } = 60;
        public bool   AutoStartEnabled  { get; set; } = false;
        public bool   SuppressApps      { get; set; } = true;
        public string AdminPassword     { get; set; } = "Admin2026!";
        public string Theme             { get; set; } = "dark"; // dark or light

        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SmartScope", "config.json");

        public static SessionConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<SessionConfig>(json) ?? new SessionConfig();
                }
            }
            catch { }
            return new SessionConfig();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch { }
        }
    }
}
