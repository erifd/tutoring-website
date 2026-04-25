// routes/auth.js
const express = require('express');
const bcrypt = require('bcryptjs');
const jwt = require('jsonwebtoken');
const crypto = require('crypto');
const { getDb } = require('../db');

const router = express.Router();

const JWT_SECRET         = process.env.JWT_SECRET;
const JWT_EXPIRES_IN     = '15m';          // access token — short lived
const REFRESH_EXPIRES_IN = 7 * 24 * 3600; // 7 days in seconds
const SALT_ROUNDS        = 12;

if (!JWT_SECRET) {
  throw new Error('JWT_SECRET is not set. Add it to your .env file.');
}

// ── Helpers ─────────────────────────────────────────────────
function makeAccessToken(user) {
  return jwt.sign(
    { sub: user.id, email: user.email, role: user.role },
    JWT_SECRET,
    { expiresIn: JWT_EXPIRES_IN }
  );
}

function makeRefreshToken() {
  return crypto.randomBytes(40).toString('hex');
}

function hashToken(token) {
  return crypto.createHash('sha256').update(token).digest('hex');
}

function safeUser(user) {
  const { password, ...rest } = user;
  return rest;
}

// ── POST /api/auth/signup ────────────────────────────────────
router.post('/signup', async (req, res) => {
  const { first_name, last_name, email, password, grade } = req.body;

  // Basic validation
  if (!first_name || !last_name || !email || !password) {
    return res.status(400).json({ error: 'first_name, last_name, email, and password are required.' });
  }
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
    return res.status(400).json({ error: 'Invalid email address.' });
  }
  if (password.length < 8) {
    return res.status(400).json({ error: 'Password must be at least 8 characters.' });
  }
  if (grade !== undefined && (grade < 1 || grade > 10)) {
    return res.status(400).json({ error: 'Grade must be between 1 and 10.' });
  }

  const db = getDb();

  try {
    // Check duplicate
    const existing = db.prepare('SELECT id FROM users WHERE email = ?').get(email.toLowerCase());
    if (existing) {
      return res.status(409).json({ error: 'An account with that email already exists.' });
    }

    const hash = await bcrypt.hash(password, SALT_ROUNDS);

    const result = db.prepare(`
      INSERT INTO users (first_name, last_name, email, password, grade)
      VALUES (?, ?, ?, ?, ?)
    `).run(first_name.trim(), last_name.trim(), email.toLowerCase(), hash, grade || null);

    const user = db.prepare('SELECT * FROM users WHERE id = ?').get(result.lastInsertRowid);

    // Issue tokens
    const accessToken  = makeAccessToken(user);
    const refreshToken = makeRefreshToken();
    const expiresAt    = new Date(Date.now() + REFRESH_EXPIRES_IN * 1000).toISOString();

    db.prepare(`
      INSERT INTO sessions (user_id, token_hash, expires_at)
      VALUES (?, ?, ?)
    `).run(user.id, hashToken(refreshToken), expiresAt);

    return res.status(201).json({
      message: 'Account created successfully.',
      accessToken,
      refreshToken,
      user: safeUser(user)
    });

  } catch (err) {
    console.error('Signup error:', err);
    return res.status(500).json({ error: 'Could not create account.' });
  }
});

// ── POST /api/auth/login ─────────────────────────────────────
router.post('/login', async (req, res) => {
  const { email, password } = req.body;

  if (!email || !password) {
    return res.status(400).json({ error: 'Email and password are required.' });
  }

  const db = getDb();

  try {
    const user = db.prepare('SELECT * FROM users WHERE email = ?').get(email.toLowerCase());

    // Deliberate constant-time path — always hash even if user not found
    const hashToCheck = user ? user.password : '$2b$12$invalidhashforinvaliduser000000000';
    const match = await bcrypt.compare(password, hashToCheck);

    if (!user || !match) {
      return res.status(401).json({ error: 'Incorrect email or password.' });
    }

    const accessToken  = makeAccessToken(user);
    const refreshToken = makeRefreshToken();
    const expiresAt    = new Date(Date.now() + REFRESH_EXPIRES_IN * 1000).toISOString();

    db.prepare(`
      INSERT INTO sessions (user_id, token_hash, expires_at)
      VALUES (?, ?, ?)
    `).run(user.id, hashToken(refreshToken), expiresAt);

    return res.json({
      accessToken,
      refreshToken,
      user: safeUser(user)
    });

  } catch (err) {
    console.error('Login error:', err);
    return res.status(500).json({ error: 'Login failed.' });
  }
});

// ── POST /api/auth/refresh ───────────────────────────────────
router.post('/refresh', (req, res) => {
  const { refreshToken } = req.body;
  if (!refreshToken) return res.status(400).json({ error: 'refreshToken is required.' });

  const db = getDb();

  const session = db.prepare(`
    SELECT s.*, u.* FROM sessions s
    JOIN users u ON u.id = s.user_id
    WHERE s.token_hash = ? AND s.expires_at > CURRENT_TIMESTAMP
  `).get(hashToken(refreshToken));

  if (!session) {
    return res.status(401).json({ error: 'Invalid or expired refresh token.' });
  }

  // Rotate refresh token
  const newRefreshToken = makeRefreshToken();
  const expiresAt = new Date(Date.now() + REFRESH_EXPIRES_IN * 1000).toISOString();

  db.prepare('DELETE FROM sessions WHERE token_hash = ?').run(hashToken(refreshToken));
  db.prepare('INSERT INTO sessions (user_id, token_hash, expires_at) VALUES (?, ?, ?)')
    .run(session.user_id, hashToken(newRefreshToken), expiresAt);

  return res.json({
    accessToken: makeAccessToken(session),
    refreshToken: newRefreshToken
  });
});

// ── POST /api/auth/logout ────────────────────────────────────
router.post('/logout', (req, res) => {
  const { refreshToken } = req.body;
  if (refreshToken) {
    getDb().prepare('DELETE FROM sessions WHERE token_hash = ?').run(hashToken(refreshToken));
  }
  return res.json({ message: 'Logged out.' });
});

module.exports = router;
