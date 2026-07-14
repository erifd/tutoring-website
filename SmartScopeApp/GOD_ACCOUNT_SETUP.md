# God Account Setup

The god account is the master account that can create and delete admin/teacher accounts.
It is NOT hardcoded in any source file — it lives only in Firebase and in your head.

## Step 1 — Create the god account manually in Firebase

1. Go to console.firebase.google.com → Authentication → Users
2. Click "Add user"
3. Enter your god email and a very strong password (12+ chars, symbols)
4. Copy the UID Firebase gives it

## Step 2 — Set the role in Firestore

1. Go to Firestore → users collection
2. Create a document with the UID from Step 1
3. Add these fields:
   - firstName: "God" (or your name)
   - lastName:  "Account"
   - email:     your god email
   - role:      "god"

## Step 3 — Save your credentials somewhere safe

Save to a password manager (Bitwarden, 1Password etc.) — not in any file in the repo.

## What the god account can do

- Log in via the Teacher tab on the desktop app
- See a red "GOD" badge in the admin dashboard
- Access the "Manage Admins" panel in the sidebar
- Create new teacher/admin accounts (they get role "teacher" or "admin")
- Remove admin/teacher accounts

## What admins/teachers can do

- Log in via the Teacher tab
- See the admin dashboard (students, classes, homework, grades)
- Cannot create other admin accounts
- Cannot see the God panel

## On the website

Press Ctrl+Shift+G on the login page to open the god panel (coming soon).
