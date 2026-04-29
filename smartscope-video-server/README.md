# SmartScope Video Server

Streams your MP4 videos from your own PC to students — for free.

---

## Setup (5 minutes)

### Step 1 — Install
```powershell
cd smartscope-video-server
npm install
```

### Step 2 — Add your videos
Drop your `.mp4` files into the `videos/` folder next to `server.js`.

### Step 3 — Configure (optional)
Copy `.env.example` to `.env` and edit:
```
PORT=4000
VIDEO_DIR=C:\Users\family_2\Videos\SmartScope
VIDEO_TOKEN=your-secret-token-here
```

If you don't create a `.env`, defaults are used (port 4000, `./videos` folder).

### Step 4 — Start the server
```powershell
npm start
```
You'll see:
```
🎬 SmartScope Video Server running!
   Local URL:  http://localhost:4000
   Videos dir: ./videos
   Token:      smartscope-video-2026
```

### Step 5 — Get a public URL with Cloudflare Tunnel
Open a **second** PowerShell window and run:
```powershell
winget install Cloudflare.cloudflared
cloudflared tunnel --url http://localhost:4000
```
You'll get a URL like:
```
https://rough-bird-3847.trycloudflare.com
```
Copy that URL.

### Step 6 — Connect to admin panel
1. Open `admin-courses.html`
2. Log in as admin
3. Paste your Cloudflare URL into the **Server URL** field
4. Enter your token
5. Click **Save & test** — you should see "✅ Connected"

Now when you build a course lesson and click **Browse my videos**, it shows all the MP4s in your folder!

---

## Notes
- Keep PowerShell open while students are watching
- The Cloudflare URL changes every time you run the tunnel — paste the new one in admin each time
- For a permanent URL, sign up for a free Cloudflare account and create a named tunnel
- Videos stream directly from your PC — they never get uploaded anywhere

## Security
- Only requests with the correct token can access videos
- No path traversal attacks possible (filename is sanitized)
- Token is passed as a URL query param — use HTTPS (Cloudflare tunnel is always HTTPS)
