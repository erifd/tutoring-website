# SmartScope — How to Start Everything

> You need **two PowerShell windows** open at the same time.
> Do these steps every time you want the site to work.

---

## Window 1 — Video Server (your MP4s)

1. Open PowerShell
2. Navigate to the server folder:
   ```
   cd 'C:\Users\family_2\Documents\GitHub\tutoring-website\smartscope-video-server'
   ```
3. Start the server:
   ```
   npm start
   ```
4. You should see:
   ```
   SmartScope Video Server running on port 4000
   ```

**Leave this window open. Don't close it.**

---

## Window 2 — Cloudflare Tunnel (public URL)

1. Open a **second** PowerShell window
2. Navigate to the same folder:
   ```
   cd 'C:\Users\family_2\Documents\GitHub\tutoring-website\smartscope-video-server'
   ```
3. Start the tunnel:
   ```
   .\cloudflared.exe tunnel --url http://localhost:4000
   ```
4. Wait ~10 seconds. You'll see something like:
   ```
   https://something-random-words.trycloudflare.com
   ```
   **Copy that URL — it's different every time you start it.**

5. Go to `admin-courses.html` → log in → paste the URL into the **Server URL** box → click **Save & test**

**Leave this window open. Don't close it.**

---

## Window 3 — Main Site Server *(optional)*

Only needed if you're running the backend locally.
**Skip this if Railway is handling it** — it probably is.

1. Open a **third** PowerShell window
2. Navigate to the backend folder:
   ```
   cd 'C:\Users\family_2\Documents\GitHub\tutoring-website\brightmind-server'
   ```
3. Start the server:
   ```
   npm start
   ```
4. You should see:
   ```
   SmartScope server running on port 3000
   ```

---

## Quick Summary

| Window | Command | What it does |
|--------|---------|-------------|
| 1 | `npm start` in `smartscope-video-server` | Serves your MP4 videos |
| 2 | `.\cloudflared.exe tunnel --url http://localhost:4000` | Makes videos publicly accessible |
| 3 | `npm start` in `brightmind-server` | Backend API (only if not using Railway) |

### Credentials
| | |
|--|--|
| **Admin login** | `admin@smartscope.com` / `Admin2026!` |
| **Video token** | `smartscope-video-2026` |

---

## Reminders

- **The Cloudflare URL changes every time you restart.**
  Always paste the new one into the admin panel.

- **Keep both PowerShell windows open** while students are watching.
  If you close them, videos stop working.

- **To add new videos:** drop the `.mp4` file into the `videos/` folder
  inside `smartscope-video-server`. No restart needed — it shows up instantly.

- **If something isn't working**, check that both PowerShell windows
  are still running and haven't errored out.