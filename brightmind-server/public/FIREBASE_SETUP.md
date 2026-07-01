# Firebase Setup — SmartScope

This takes about 10 minutes. You only do it once.

---

## Step 1 — Create a Firebase project

1. Go to **console.firebase.google.com**
2. Click **Add project**
3. Name it `smartscope` → click Continue
4. Disable Google Analytics (you don't need it) → click **Create project**

---

## Step 2 — Enable Authentication

1. In the left sidebar click **Authentication** → **Get started**
2. Click **Email/Password** → toggle **Enable** → click Save

---

## Step 3 — Enable Firestore

1. In the left sidebar click **Firestore Database** → **Create database**
2. Choose **Start in test mode** (we'll add rules next)
3. Pick your region (us-east1 is fine) → click Enable

---

## Step 4 — Add your security rules

1. In Firestore → click the **Rules** tab
2. Delete everything there
3. Paste in the contents of `firestore.rules`
4. Click **Publish**

---

## Step 5 — Get your config keys

1. Click the ⚙️ gear icon → **Project settings**
2. Scroll down to **Your apps** → click the **</>** (Web) icon
3. Name it `SmartScope Web` → click **Register app**
4. Copy the `firebaseConfig` object — it looks like:

```js
const firebaseConfig = {
  apiKey: "AIza...",
  authDomain: "smartscope-xxxxx.firebaseapp.com",
  projectId: "smartscope-xxxxx",
  storageBucket: "smartscope-xxxxx.appspot.com",
  messagingSenderId: "123456789",
  appId: "1:123:web:abc123"
};
```

---

## Step 6 — Paste into firebase-config.js

Open `firebase-config.js` and replace each placeholder with your real value:

```js
const FIREBASE_CONFIG = {
  apiKey:            "AIza...",           // ← your real key
  authDomain:        "smartscope-xxxxx.firebaseapp.com",
  projectId:         "smartscope-xxxxx",
  storageBucket:     "smartscope-xxxxx.appspot.com",
  messagingSenderId: "123456789",
  appId:             "1:123:web:abc123"
};
```

Save the file.

---

## Step 7 — Put firebase-config.js and firebase-auth.js next to your HTML files

All your HTML files need to be in the same folder as:
- `firebase-config.js`
- `firebase-auth.js`

---

## Step 8 — Push to GitHub

```powershell
git add firebase-config.js firebase-auth.js
git commit -m "add firebase"
git push
```

GitHub Pages will serve it. Your site now uses Firebase for all auth.

---

## How the linking key works

1. **Student signs up** → Firebase creates their account and generates a random 6-character code (e.g. `X7KP2Q`), stored in Firestore
2. **Student finds their code** → Go to **My Dashboard → Grades** tab — the code shows at the bottom
3. **Parent signs up** → Goes to **My Children → Link a child account**
4. **Parent enters the code** → Firebase finds the student, links both accounts
5. **Parent can now see** the child's grades, homework, classes, and buy courses for them
6. **Courses parent buys** instantly appear as unlocked in the student's dashboard

---

## Firestore data structure

```
users/
  {uid}/
    firstName, lastName, email, role
    linkingKey    (students only — their 6-char code)
    parentUid     (students only — set when linked)
    children      (parents only — array of student UIDs)
    grade         (students only)

enrollments/
  {studentUid}_{courseIdx}/
    studentUid, courseIdx, courseName
    unlockedBy (parentUid), unlockedAt

grades/
  {studentUid}/
    subjects: { Mathematics: 9, Chemistry: 7, ... }

classes/
  {classId}/
    studentUid, subject, date, time, tutor, link, status

homework/
  {hwId}/
    title, subject, instructions, questions, assignedTo, dueDate

submissions/
  {hwId}_{studentUid}/
    answers, submittedAt, grade, feedback
```

---

## Files changed

| File | What changed |
|---|---|
| `firebase-config.js` | **You fill this in** with your Firebase keys |
| `firebase-auth.js` | All auth + Firestore helpers (don't edit) |
| `firestore.rules` | Paste into Firebase Console → Firestore → Rules |
| `tutoring-site.html` | Login/signup now uses Firebase |
| `student.html` | Loads data from Firestore, shows linking key |
| `parent.html` | Links to child via code, buys courses via Firestore |
