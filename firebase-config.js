// ─────────────────────────────────────────────────────────────────
//  FIREBASE CONFIG — SmartScope
//  Replace these values with your own from Firebase Console:
//  console.firebase.google.com → Project Settings → Your apps → Web
// ─────────────────────────────────────────────────────────────────
const FIREBASE_CONFIG = {
  apiKey:            "YOUR_API_KEY",
  authDomain:        "YOUR_PROJECT_ID.firebaseapp.com",
  projectId:         "YOUR_PROJECT_ID",
  storageBucket:     "YOUR_PROJECT_ID.appspot.com",
  messagingSenderId: "YOUR_MESSAGING_SENDER_ID",
  appId:             "YOUR_APP_ID"
};

// ── Firestore data structure (for reference) ─────────────────────
//
//  users/{uid}
//    role:         'student' | 'parent'
//    firstName:    string
//    lastName:     string
//    email:        string
//    grade:        number | null        (students only)
//    linkingKey:   string (6-char)      (students only)
//    parentUid:    string | null        (students only — set when parent links)
//    children:     string[]             (parents only — array of student UIDs)
//    createdAt:    timestamp
//
//  enrollments/{studentUid}_{courseIdx}
//    studentUid:   string
//    courseIdx:    number
//    courseName:   string
//    unlockedBy:   string (parentUid)
//    unlockedAt:   timestamp
//
//  grades/{studentUid}
//    subjects:     { [subject]: number }
//    updatedAt:    timestamp
//
//  classes/{classId}
//    studentUid:   string
//    subject:      string
//    date:         string (YYYY-M-D)
//    time:         string
//    tutor:        string
//    link:         string
//    status:       'scheduled' | 'live' | 'soon'
//    note:         string
//    createdAt:    timestamp
//
//  homework/{homeworkId}
//    title:        string
//    subject:      string
//    instructions: string
//    questions:    { text: string }[]
//    assignedTo:   'all' | string (studentUid)
//    dueDate:      string
//    createdAt:    timestamp
//
//  submissions/{homeworkId}_{studentUid}
//    answers:      string[]
//    submittedAt:  timestamp
//    grade:        number | null
//    feedback:     string
