require('dotenv').config();
const express    = require('express');
const cors       = require('cors');
const bcrypt     = require('bcryptjs');
const jwt        = require('jsonwebtoken');
const rateLimit  = require('express-rate-limit');
const { v4: uuidv4 } = require('uuid');
const db         = require('./db');

const app  = express();
const PORT = process.env.PORT || 3000;
const JWT_SECRET = process.env.JWT_SECRET || 'change-this-in-production';

app.use(express.json());
app.use(cors({
  origin: process.env.ALLOWED_ORIGIN || '*',
  methods: ['GET', 'POST', 'PUT', 'DELETE'],
  allowedHeaders: ['Content-Type', 'Authorization'],
}));
app.use(express.static('public'));

const authLimiter = rateLimit({
  windowMs: 15 * 60 * 1000,
  max: 20,
  message: { error: 'Too many attempts, please try again later.' },
});

function requireAuth(req, res, next) {
  const header = req.headers.authorization;
  if (!header || !header.startsWith('Bearer ')) {
    return res.status(401).json({ error: 'No token provided.' });
  }
  try {
    req.user = jwt.verify(header.slice(7), JWT_SECRET);
    next();
  } catch {
    res.status(401).json({ error: 'Invalid or expired token.' });
  }
}

app.get('/api/health', (req, res) => {
  res.json({ status: 'ok', timestamp: new Date().toISOString() });
});

app.post('/api/signup', authLimiter, async (req, res) => {
  try {
    const { firstName, lastName, email, password, grade } = req.body;
    if (!firstName || !lastName || !email || !password)
      return res.status(400).json({ error: 'All fields are required.' });
    if (password.length < 8)
      return res.status(400).json({ error: 'Password must be at least 8 characters.' });
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email))
      return res.status(400).json({ error: 'Invalid email address.' });

    const users = db.get('users').value();
    if (users.find(u => u.email.toLowerCase() === email.toLowerCase()))
      return res.status(409).json({ error: 'An account with this email already exists.' });

    const passwordHash = await bcrypt.hash(password, 12);
    const newUser = {
      id: uuidv4(),
      firstName, lastName,
      email: email.toLowerCase(),
      passwordHash,
      grade: grade || null,
      role: 'student',
      createdAt: new Date().toISOString(),
    };

    db.get('users').push(newUser).write();
    const token = jwt.sign({ id: newUser.id, email: newUser.email, role: newUser.role }, JWT_SECRET, { expiresIn: '7d' });
    res.status(201).json({ token, user: sanitize(newUser) });
  } catch (err) {
    console.error('Signup error:', err);
    res.status(500).json({ error: 'Something went wrong. Please try again.' });
  }
});

app.post('/api/login', authLimiter, async (req, res) => {
  try {
    const { email, password } = req.body;
    if (!email || !password)
      return res.status(400).json({ error: 'Email and password are required.' });

    const user = db.get('users').find(u => u.email === email.toLowerCase()).value();
    if (!user) return res.status(401).json({ error: 'Invalid email or password.' });

    const match = await bcrypt.compare(password, user.passwordHash);
    if (!match) return res.status(401).json({ error: 'Invalid email or password.' });

    const token = jwt.sign({ id: user.id, email: user.email, role: user.role }, JWT_SECRET, { expiresIn: '7d' });
    res.json({ token, user: sanitize(user) });
  } catch (err) {
    console.error('Login error:', err);
    res.status(500).json({ error: 'Something went wrong. Please try again.' });
  }
});

app.get('/api/me', requireAuth, (req, res) => {
  const user = db.get('users').find({ id: req.user.id }).value();
  if (!user) return res.status(404).json({ error: 'User not found.' });
  res.json({ user: sanitize(user) });
});

app.put('/api/me/grades', requireAuth, (req, res) => {
  const { subject, grade } = req.body;
  if (!subject || grade === undefined)
    return res.status(400).json({ error: 'subject and grade are required.' });
  const user = db.get('users').find({ id: req.user.id }).value();
  if (!user) return res.status(404).json({ error: 'User not found.' });
  const updatedGrades = { ...user.grades, [subject]: grade };
  db.get('users').find({ id: req.user.id }).assign({ grades: updatedGrades }).write();
  res.json({ grades: updatedGrades });
});

// Public: student count for hero stat
app.get('/api/stats', (req, res) => {
  const count = db.get('users').value().length;
  res.json({ studentCount: count });
});

// Admin: full student list (protected by admin key header)
app.get('/api/admin/students', (req, res) => {
  const key = req.headers['x-admin-key'];
  const ADMIN_KEY = process.env.ADMIN_KEY || 'Admin2026!';
  if (key !== ADMIN_KEY) return res.status(403).json({ error: 'Forbidden.' });
  const students = db.get('users').value().map(sanitize);
  res.json({ students });
});

function sanitize(user) {
  const { passwordHash, ...safe } = user;
  return safe;
}

app.listen(PORT, () => console.log(`SmartScope server running on port ${PORT}`));