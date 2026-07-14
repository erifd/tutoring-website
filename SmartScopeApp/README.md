# SmartScope Kiosk App

A locked-down Windows class attendance app built with C# / .NET 6 + WebView2.

## What it does

- Opens full screen, hides the taskbar
- Blocks Alt+Tab, Alt+F4, Win key, Escape
- Shows the SmartScope student dashboard inside a locked browser
- Counts down the session timer in the top bar
- Requires an admin password to exit
- Optionally closes distracting apps (Discord, Spotify etc.)
- Optionally launches automatically on Windows login

## Requirements

- Windows 10 or 11
- .NET 6 SDK — download from https://dotnet.microsoft.com/download/dotnet/6.0
- WebView2 Runtime — download from https://developer.microsoft.com/en-us/microsoft-edge/webview2/

## Build & Run

```powershell
cd SmartScopeApp
dotnet build
dotnet run
```

Or build a release executable:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

Then run `publish\SmartScopeApp.exe`

## How it works

1. App opens → **Setup screen** appears (admin fills in student name, subject, URL, duration, password)
2. Click **Start Class Session** → screen locks, kiosk launches
3. Student sees their SmartScope dashboard inside the locked window
4. Timer counts down in the top bar
5. When time is up → session ends automatically
6. To exit early → click **End Session** → enter admin password

## Files

| File | Purpose |
|---|---|
| `Program.cs` | Entry point |
| `SetupForm.cs` | Pre-session setup screen (admin only) |
| `KioskForm.cs` | Main locked kiosk window |
| `SessionConfig.cs` | Saves/loads session settings to AppData |
| `KioskLauncher.cs` | Auto-start, app suppression, session logging |

## Config is saved to

```
C:\Users\{name}\AppData\Roaming\SmartScope\config.json
```

Session logs saved to:
```
C:\Users\{name}\AppData\Roaming\SmartScope\Logs\sessions_YYYY-MM.txt
```
