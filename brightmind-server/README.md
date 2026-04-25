# BrightMind — Auth Server

Node.js + Express backend for BrightMind Tutoring.  
Handles sign-up, login, and student data via a simple file-based database.

---

## Stack

| Layer | Tech |
|---|---|
| Runtime | Node.js 18+ |
| Framework | Express |
| Auth | JWT (jsonwebtoken) + bcryptjs |
| Database | lowdb (JSON file — easy to swap for Postgres later) |
| Hosting | Railway (free tier) |

---

## Local setup

```bash
# 1. Clone / copy this folder
cd brightmind-server

# 2. Install dependencies
npm install

# 3. Create your .env file
cp .env.example .env
# Then open .env and set JWT_SECRET to a long random string:
node -e "console.log(require('crypto').randomBytes(64).toString('hex'))"

# 4. Start the dev server
npm run dev     # uses nodemon for auto-reload
# or
npm start       # plain node

# Server runs at http://localhost:3000
```

---

## Deploy to Railway (free)

1. **Push to GitHub** — commit this folder to a GitHub repo.

2. **Create Railway project**
   - Go to [railway.app](https://railway.app) → New Project → Deploy from GitHub repo
   - Select your repo

3. **Set environment variables** in Railway dashboard → Variables:
   ```
   JWT_SECRET=<your-long-random-string>
   ALLOWED_ORIGIN=https://your-frontend-url.com
   ```

4. **Railway auto-detects** Node.js and runs `npm start`. Done!

5. **Copy your Railway URL** (e.g. `https://brightmind-production.up.railway.app`)
   and paste it into `tutoring-site.html`:
   ```js
   const API_BASE = 'https://brightmind-production.up.railway.app/api';
   ```

---

## API Endpoints

### `POST /api/signup`
```json
{ "firstName": "Jane", "lastName": "Smith", "email": "jane@example.com", "password": "secret123", "grade": 9 }
```
Returns `{ token, user }`.

### `POST /api/login`
```json
{ "email": "jane@example.com", "password": "secret123" }
```
Returns `{ token, user }`.

### `GET /api/me`
Header: `Authorization: Bearer <token>`  
Returns `{ user }` for the logged-in student.

### `PUT /api/me/grades`
Header: `Authorization: Bearer <token>`  
```json
{ "subject": "Mathematics", "grade": 9 }
```
Updates a grade. Returns `{ grades }`.

### `GET /api/health`
Returns `{ status: "ok" }`. Used by Railway for health checks.

---

## Upgrading the database

`lowdb` stores data in `data/db.json`. When you're ready to scale:
1. Add `pg` or `@prisma/client` to package.json
2. Replace `db.js` with a Postgres connection
3. Set `DATABASE_URL` in Railway variables (Railway offers a free Postgres add-on)

The rest of the server code stays the same.
