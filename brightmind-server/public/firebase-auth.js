// ─────────────────────────────────────────────────────────────────
//  SmartScope — Firebase Auth & Firestore helpers
//  Include this on every page AFTER the Firebase SDK scripts
// ─────────────────────────────────────────────────────────────────

// Import from Firebase CDN (added in each HTML file)
// These are available globally after the SDK scripts load

const { initializeApp }                              = window.firebaseApp    || {};
const { getAuth, createUserWithEmailAndPassword,
        signInWithEmailAndPassword, signOut,
        onAuthStateChanged }                         = window.firebaseAuth   || {};
const { getFirestore, doc, setDoc, getDoc,
        updateDoc, collection, query, where,
        getDocs, addDoc, serverTimestamp,
        arrayUnion, arrayRemove, onSnapshot }        = window.firebaseFirestore || {};

// ── Init ──────────────────────────────────────────────────────────
const app  = initializeApp(FIREBASE_CONFIG);
const auth = getAuth(app);
const db   = getFirestore(app);

// ── Linking key generator ─────────────────────────────────────────
function generateLinkingKey() {
  const chars = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789'; // no confusing chars
  let key = '';
  for (let i = 0; i < 6; i++) key += chars[Math.floor(Math.random() * chars.length)];
  return key;
}

// ── SIGN UP ───────────────────────────────────────────────────────
async function firebaseSignup({ firstName, lastName, email, password, role, grade }) {
  // Create auth user
  const cred = await createUserWithEmailAndPassword(auth, email, password);
  const uid  = cred.user.uid;

  const userData = {
    firstName,
    lastName,
    email: email.toLowerCase(),
    role,
    createdAt: serverTimestamp(),
  };

  if (role === 'student') {
    userData.grade      = grade || null;
    userData.linkingKey = generateLinkingKey();
    userData.parentUid  = null;
    // Make sure linking key is unique
    // (collision chance is negligible for small user bases)
  }

  if (role === 'parent') {
    userData.children = [];
  }

  await setDoc(doc(db, 'users', uid), userData);
  return { uid, ...userData };
}

// ── SIGN IN ───────────────────────────────────────────────────────
async function firebaseLogin(email, password) {
  const cred = await signInWithEmailAndPassword(auth, email, password);
  const uid  = cred.user.uid;
  const snap = await getDoc(doc(db, 'users', uid));
  if (!snap.exists()) throw new Error('User profile not found.');
  return { uid, ...snap.data() };
}

// ── SIGN OUT ──────────────────────────────────────────────────────
async function firebaseLogout() {
  await signOut(auth);
  localStorage.removeItem('ss_tok');
  localStorage.removeItem('ss_user');
}

// ── GET USER PROFILE ──────────────────────────────────────────────
async function getUserProfile(uid) {
  const snap = await getDoc(doc(db, 'users', uid));
  if (!snap.exists()) return null;
  return { uid, ...snap.data() };
}

// ── LINK PARENT → STUDENT via linking key ─────────────────────────
async function linkParentToStudent(parentUid, linkingKey) {
  // Find student with this linking key
  const q    = query(collection(db, 'users'), where('linkingKey', '==', linkingKey.toUpperCase()), where('role', '==', 'student'));
  const snap = await getDocs(q);

  if (snap.empty) throw new Error('No student found with that code. Double-check and try again!');

  const studentDoc  = snap.docs[0];
  const studentUid  = studentDoc.id;
  const studentData = studentDoc.data();

  if (studentData.parentUid) throw new Error('This student is already linked to a parent account.');

  // Link both ways
  await updateDoc(doc(db, 'users', studentUid), { parentUid });
  await updateDoc(doc(db, 'users', parentUid),  { children: arrayUnion(studentUid) });

  return { uid: studentUid, ...studentData };
}

// ── UNLINK ────────────────────────────────────────────────────────
async function unlinkParentFromStudent(parentUid, studentUid) {
  await updateDoc(doc(db, 'users', studentUid), { parentUid: null });
  await updateDoc(doc(db, 'users', parentUid),  { children: arrayRemove(studentUid) });
}

// ── ENROLL STUDENT IN COURSE (parent buys) ───────────────────────
async function enrollStudentInCourse(studentUid, parentUid, courseIdx, courseName) {
  const docId = `${studentUid}_${courseIdx}`;
  await setDoc(doc(db, 'enrollments', docId), {
    studentUid,
    courseIdx,
    courseName,
    unlockedBy:  parentUid,
    unlockedAt:  serverTimestamp(),
  });
}

// ── GET STUDENT ENROLLMENTS ───────────────────────────────────────
async function getStudentEnrollments(studentUid) {
  const q    = query(collection(db, 'enrollments'), where('studentUid', '==', studentUid));
  const snap = await getDocs(q);
  return snap.docs.map(d => d.data());
}

// ── GRADES ────────────────────────────────────────────────────────
async function getStudentGrades(studentUid) {
  const snap = await getDoc(doc(db, 'grades', studentUid));
  return snap.exists() ? snap.data().subjects || {} : {};
}

async function setStudentGrades(studentUid, subjects) {
  await setDoc(doc(db, 'grades', studentUid), { subjects, updatedAt: serverTimestamp() }, { merge: true });
}

// ── CLASSES ───────────────────────────────────────────────────────
async function getStudentClasses(studentUid) {
  const q    = query(collection(db, 'classes'), where('studentUid', '==', studentUid));
  const snap = await getDocs(q);
  return snap.docs.map(d => ({ id: d.id, ...d.data() }));
}

async function addStudentClass(studentUid, classData) {
  return await addDoc(collection(db, 'classes'), {
    studentUid,
    ...classData,
    createdAt: serverTimestamp(),
  });
}

async function deleteStudentClass(classId) {
  const { deleteDoc } = window.firebaseFirestore || {};
  await deleteDoc(doc(db, 'classes', classId));
}

// ── HOMEWORK ──────────────────────────────────────────────────────
async function getAllHomework() {
  const snap = await getDocs(collection(db, 'homework'));
  return snap.docs.map(d => ({ id: d.id, ...d.data() }));
}

async function getHomeworkForStudent(studentUid) {
  const all = await getAllHomework();
  return all.filter(h => h.assignedTo === 'all' || h.assignedTo === studentUid);
}

async function createHomework(data) {
  return await addDoc(collection(db, 'homework'), { ...data, createdAt: serverTimestamp() });
}

async function deleteHomework(homeworkId) {
  const { deleteDoc } = window.firebaseFirestore || {};
  await deleteDoc(doc(db, 'homework', homeworkId));
}

// ── SUBMISSIONS ───────────────────────────────────────────────────
async function getSubmission(homeworkId, studentUid) {
  const snap = await getDoc(doc(db, 'submissions', `${homeworkId}_${studentUid}`));
  return snap.exists() ? snap.data() : null;
}

async function submitHomework(homeworkId, studentUid, answers) {
  await setDoc(doc(db, 'submissions', `${homeworkId}_${studentUid}`), {
    answers,
    submittedAt: serverTimestamp(),
    grade:       null,
    feedback:    '',
  });
}

async function gradeSubmission(homeworkId, studentUid, grade, feedback) {
  await updateDoc(doc(db, 'submissions', `${homeworkId}_${studentUid}`), { grade, feedback });
}

async function getAllSubmissionsForHomework(homeworkId, studentUids) {
  const results = [];
  for (const uid of studentUids) {
    const sub = await getSubmission(homeworkId, uid);
    if (sub) results.push({ studentUid: uid, ...sub });
  }
  return results;
}

// ── Auth state listener (call on every page) ──────────────────────
function onAuthReady(callback) {
  onAuthStateChanged(auth, async (firebaseUser) => {
    if (firebaseUser) {
      const profile = await getUserProfile(firebaseUser.uid);
      callback(profile);
    } else {
      callback(null);
    }
  });
}

// ── Save/load user to localStorage for fast access ────────────────
function cacheUser(user) { localStorage.setItem('ss_user', JSON.stringify(user)); }
function getCachedUser() { try { return JSON.parse(localStorage.getItem('ss_user')); } catch { return null; } }
function clearCache()    { localStorage.removeItem('ss_user'); }
