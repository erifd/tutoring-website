require('dotenv').config();
const express = require('express');
const cors    = require('cors');
const fs      = require('fs');
const path    = require('path');

const app       = express();
const PORT      = process.env.PORT || 4000;
const VIDEO_DIR = process.env.VIDEO_DIR || path.join(__dirname, 'videos');
const TOKEN     = process.env.VIDEO_TOKEN || 'smartscope-video-2026';

// Make sure videos folder exists
if (!fs.existsSync(VIDEO_DIR)) {
  fs.mkdirSync(VIDEO_DIR, { recursive: true });
  console.log(`Created videos folder at: ${VIDEO_DIR}`);
}

app.use(cors({ origin: '*' }));

// ── Token auth middleware ─────────────────────────────────────────
function requireToken(req, res, next) {
  const t = req.query.token || req.headers['x-video-token'];
  if (t !== TOKEN) return res.status(401).json({ error: 'Unauthorized' });
  next();
}

// ── List available videos ─────────────────────────────────────────
app.get('/videos', requireToken, (req, res) => {
  try {
    const files = fs.readdirSync(VIDEO_DIR)
      .filter(f => /\.(mp4|webm|mov|mkv)$/i.test(f))
      .map(f => {
        const stat = fs.statSync(path.join(VIDEO_DIR, f));
        return {
          name: f,
          size: stat.size,
          sizeMB: (stat.size / 1024 / 1024).toFixed(1),
          url: `/stream/${encodeURIComponent(f)}?token=${TOKEN}`
        };
      });
    res.json({ videos: files, count: files.length, dir: VIDEO_DIR });
  } catch (e) {
    res.status(500).json({ error: e.message });
  }
});

// ── Stream video with range support ──────────────────────────────
app.get('/stream/:filename', requireToken, (req, res) => {
  const filename = decodeURIComponent(req.params.filename);

  // Security: no path traversal
  if (filename.includes('..') || filename.includes('/') || filename.includes('\\')) {
    return res.status(400).json({ error: 'Invalid filename' });
  }

  const filepath = path.join(VIDEO_DIR, filename);

  if (!fs.existsSync(filepath)) {
    return res.status(404).json({ error: 'Video not found' });
  }

  const stat = fs.statSync(filepath);
  const fileSize = stat.size;
  const range = req.headers.range;
  const ext = path.extname(filename).toLowerCase();
  const mimeTypes = { '.mp4':'video/mp4', '.webm':'video/webm', '.mov':'video/quicktime', '.mkv':'video/x-matroska' };
  const contentType = mimeTypes[ext] || 'video/mp4';

  if (range) {
    // Partial content (seeking support)
    const parts = range.replace(/bytes=/, '').split('-');
    const start = parseInt(parts[0], 10);
    const end   = parts[1] ? parseInt(parts[1], 10) : Math.min(start + 10 * 1024 * 1024, fileSize - 1);
    const chunkSize = end - start + 1;

    res.writeHead(206, {
      'Content-Range':  `bytes ${start}-${end}/${fileSize}`,
      'Accept-Ranges':  'bytes',
      'Content-Length': chunkSize,
      'Content-Type':   contentType,
      'Cache-Control':  'no-store',
    });
    fs.createReadStream(filepath, { start, end }).pipe(res);
  } else {
    // Full file
    res.writeHead(200, {
      'Content-Length': fileSize,
      'Content-Type':   contentType,
      'Accept-Ranges':  'bytes',
      'Cache-Control':  'no-store',
    });
    fs.createReadStream(filepath).pipe(res);
  }
});

// ── Health check ──────────────────────────────────────────────────
app.get('/health', (req, res) => {
  const files = fs.readdirSync(VIDEO_DIR).filter(f => /\.(mp4|webm|mov|mkv)$/i.test(f));
  res.json({ status: 'ok', videoCount: files.length, dir: VIDEO_DIR });
});

// ── Start ─────────────────────────────────────────────────────────
app.listen(PORT, () => {
  console.log('\n🎬 SmartScope Video Server running!');
  console.log(`   Local URL:  http://localhost:${PORT}`);
  console.log(`   Videos dir: ${VIDEO_DIR}`);
  console.log(`   Token:      ${TOKEN}`);
  console.log('\n📁 Drop your MP4 files into the "videos" folder.');
  console.log('🌐 Then run Cloudflare Tunnel to get a public URL.\n');
});
