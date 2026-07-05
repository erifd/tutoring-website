using System;
using System.Windows.Forms;

namespace SmartScopeApp
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();

            // If launched with --kiosk flag, skip setup and go straight to kiosk
            if (args.Length > 0 && args[0] == "--kiosk")
            {
                var config = SessionConfig.Load();
                var kiosk  = new KioskForm(config);
                kiosk.StartSession(config.DurationMinutes);
                Application.Run(kiosk);
            }
            else
            {
                // Show setup form first
                Application.Run(new SetupForm());
            }
        }
    }
}
