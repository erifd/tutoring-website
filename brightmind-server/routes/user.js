// routes/user.js — protected endpoints for the logged-in user
const express = require('express');
const bcrypt = require('bcryptjs');
const { getDb } = require('../db');
const { requireAuth } = require('../middleware/auth');

const router = express.Router();

function safeUser(user) {
  const { password, ...rest } = user;
  return rest;
}

// ── GET /api/user/me ─────────────────────────────────────────
// Returns the current user's profile
router.get('/me', requireAuth, (req, res) => {
  const user = getDb().prepare('SELECT * FROM users WHERE id = ?').get(req.user.sub);
  if (!user) return res.status(404).json({ error: 'User not found.' });
  return res.json(safeUser(user));
});

// ── PUT /api/user/me ─────────────────────────────────────────
// Update name or grade
router.put('/me', requireAuth, (req, res) => {
  const { first_name, last_name, grade } = req.body;

  if (grade !== undefined && (grade < 1 || grade > 10)) {
    return res.status(400).json({ error: 'Grade must be between 1 and 10.' });
  }

  const db = getDb();
  db.prepare(`
    UPDATE users
    SET
      first_name = COALESCE(?, first_name),
      last_name  = COALESCE(?, last_name),
      grade      = COALESCE(?, grade)
    WHERE id = ?
  `).run(first_name || null, last_name || null, grade || null, req.user.sub);

  const updated = db.prepare('SELECT * FROM users WHERE id = ?').get(req.user.sub);
  return res.json(safeUser(updated));
});

// ── PUT /api/user/me/password ─────────────────────────────────
// Change password
router.put('/me/password', requireAuth, async (req, res) => {
  const { current_password, new_password } = req.body;
  if (!current_password || !new_password) {
    return res.status(400).json({ error: 'current_password and new_password are required.' });
  }
  if (new_password.length < 8) {
    return res.status(400).json({ error: 'New password must be at least 8 characters.' });
  }

  const db = getDb();
  const user = db.prepare('SELECT * FROM users WHERE id = ?').get(req.user.sub);
  const match = await bcrypt.compare(current_password, user.password);
  if (!match) return res.status(401).json({ error: 'Current password is incorrect.' });

  const hash = await bcrypt.hash(new_password, 12);
  db.prepare('UPDATE users SET password = ? WHERE id = ?').run(hash, req.user.sub);

  // Invalidate all other sessions so other devices are logged out
  db.prepare('DELETE FROM sessions WHERE user_id = ?').run(req.user.sub);

  return res.json({ message: 'Password updated. Please log in again.' });
});

// ── DELETE /api/user/me ──────────────────────────────────────
// Delete own account
router.delete('/me', requireAuth, async (req, res) => {
  const { password } = req.body;
  if (!password) return res.status(400).json({ error: 'Password is required to delete your account.' });

  const db = getDb();
  const user = db.prepare('SELECT * FROM users WHERE id = ?').get(req.user.sub);
  const match = await bcrypt.compare(password, user.password);
  if (!match) return res.status(401).json({ error: 'Incorrect password.' });

  db.prepare('DELETE FROM users WHERE id = ?').run(req.user.sub);
  return res.json({ message: 'Account deleted.' });
});

module.exports = router;
