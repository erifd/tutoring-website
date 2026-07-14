using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartScopeApp
{
    public static class ApiClient
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(12) };
        private const string API_KEY   = "AIzaSyBfHhEOEc8nUS9OzSTsN71QLRetLdtMTVU";
        private const string AUTH_URL  = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=" + API_KEY;
        private const string FS_BASE   = "https://firestore.googleapis.com/v1/projects/";
        public  static string ProjectId { get; set; } = "YOUR_PROJECT_ID";

        public static string?      IdToken { get; private set; }
        public static UserProfile? Current { get; private set; }

        public static async Task<UserProfile> LoginAsync(string email, string password)
        {
            var body = JsonSerializer.Serialize(new { email, password, returnSecureToken = true });
            var resp = await Http.PostAsync(AUTH_URL, new StringContent(body, Encoding.UTF8, "application/json"));
            var json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                var e = JsonSerializer.Deserialize<FbErrWrap>(json);
                throw new Exception(e?.error?.message switch
                {
                    "EMAIL_NOT_FOUND"           => "No account found with that email.",
                    "INVALID_PASSWORD"          => "Wrong password. Try again.",
                    "INVALID_LOGIN_CREDENTIALS" => "Wrong email or password.",
                    "USER_DISABLED"             => "This account has been disabled.",
                    "TOO_MANY_ATTEMPTS_TRY_LATER" => "Too many attempts. Try again later.",
                    var m => "Login failed: " + m
                });
            }
            var auth = JsonSerializer.Deserialize<FbAuthResult>(json)!;
            IdToken  = auth.idToken;
            Http.DefaultRequestHeaders.Remove("Authorization");
            Http.DefaultRequestHeaders.Add("Authorization", $"Bearer {IdToken}");
            Current  = await GetProfileAsync(auth.localId);
            return Current;
        }

        public static async Task<UserProfile> GetProfileAsync(string uid)
        {
            var resp = await Http.GetAsync($"{FS_BASE}{ProjectId}/databases/(default)/documents/users/{uid}");
            var json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new Exception("Could not load user profile.");
            var doc  = JsonSerializer.Deserialize<FsDoc>(json)!;
            var f    = doc.fields ?? new();
            return new UserProfile
            {
                uid        = uid,
                firstName  = Str(f,"firstName"),
                lastName   = Str(f,"lastName"),
                email      = Str(f,"email"),
                role       = Str(f,"role"),
                linkingKey = Str(f,"linkingKey"),
                grade      = Int(f,"grade"),
            };
        }

        public static async Task<List<ClassSession>> GetClassesAsync(string uid)
        {
            var resp = await Http.GetAsync($"{FS_BASE}{ProjectId}/databases/(default)/documents/classes?pageSize=200");
            var list = new List<ClassSession>();
            if (!resp.IsSuccessStatusCode) return list;
            var doc  = JsonSerializer.Deserialize<FsListResp>(await resp.Content.ReadAsStringAsync());
            foreach (var d in doc?.documents ?? new())
            {
                var f = d.fields ?? new();
                if (Str(f,"studentUid") != uid) continue;
                list.Add(new ClassSession
                {
                    Id      = d.name?.Split('/')[^1] ?? "",
                    Subject = Str(f,"subject"), Date = Str(f,"date"),
                    Time    = Str(f,"time"),    Tutor= Str(f,"tutor"),
                    Link    = Str(f,"link"),    Status=Str(f,"status"),
                    Note    = Str(f,"note"),
                });
            }
            return list;
        }

        public static async Task<List<int>> GetEnrolledIndexesAsync(string uid)
        {
            var resp = await Http.GetAsync($"{FS_BASE}{ProjectId}/databases/(default)/documents/enrollments?pageSize=200");
            var list = new List<int>();
            if (!resp.IsSuccessStatusCode) return list;
            var doc  = JsonSerializer.Deserialize<FsListResp>(await resp.Content.ReadAsStringAsync());
            foreach (var d in doc?.documents ?? new())
            {
                var f = d.fields ?? new();
                if (Str(f,"studentUid") != uid) continue;
                var idx = Int(f,"courseIdx");
                if (idx.HasValue) list.Add(idx.Value);
            }
            return list;
        }

        public static async Task<Dictionary<string,int>> GetGradesAsync(string uid)
        {
            var result = new Dictionary<string,int>();
            var resp   = await Http.GetAsync($"{FS_BASE}{ProjectId}/databases/(default)/documents/grades/{uid}");
            if (!resp.IsSuccessStatusCode) return result;
            var doc    = JsonSerializer.Deserialize<FsDoc>(await resp.Content.ReadAsStringAsync());
            if (doc?.fields != null && doc.fields.TryGetValue("subjects", out var sv) && sv.mapValue?.fields != null)
                foreach (var kv in sv.mapValue.fields)
                    if (int.TryParse(kv.Value.integerValue ?? kv.Value.doubleValue, out int g))
                        result[kv.Key] = g;
            return result;
        }

        // ── LOGOUT ───────────────────────────────────────────────
        public static async Task LogoutAsync()
        {
            IdToken = null; Current = null;
            Http.DefaultRequestHeaders.Remove("Authorization");
            await Task.CompletedTask;
        }

        // ── GET ALL STUDENTS from Firestore ───────────────────────
        public static async Task<List<UserProfile>> GetAllStudentsAsync()
        {
            var resp = await Http.GetAsync($"{FS_BASE}{ProjectId}/databases/(default)/documents/users?pageSize=200");
            var list = new List<UserProfile>();
            if (!resp.IsSuccessStatusCode) return list;
            var doc  = JsonSerializer.Deserialize<FsListResp>(await resp.Content.ReadAsStringAsync());
            foreach (var d in doc?.documents ?? new())
            {
                var f = d.fields ?? new();
                if (Str(f,"role") != "student") continue;
                list.Add(new UserProfile
                {
                    uid        = d.name?.Split('/')[^1] ?? "",
                    firstName  = Str(f,"firstName"),
                    lastName   = Str(f,"lastName"),
                    email      = Str(f,"email"),
                    role       = Str(f,"role"),
                    linkingKey = Str(f,"linkingKey"),
                    grade      = Int(f,"grade"),
                });
            }
            return list;
        }

        // ── GET ADMIN ACCOUNTS ────────────────────────────────────
        public static async Task<List<UserProfile>> GetAdminAccountsAsync()
        {
            var resp = await Http.GetAsync($"{FS_BASE}{ProjectId}/databases/(default)/documents/users?pageSize=200");
            var list = new List<UserProfile>();
            if (!resp.IsSuccessStatusCode) return list;
            var doc  = JsonSerializer.Deserialize<FsListResp>(await resp.Content.ReadAsStringAsync());
            foreach (var d in doc?.documents ?? new())
            {
                var f = d.fields ?? new();
                string role = Str(f,"role");
                if (role != "admin" && role != "teacher" && role != "god") continue;
                list.Add(new UserProfile
                {
                    uid       = d.name?.Split('/')[^1] ?? "",
                    firstName = Str(f,"firstName"),
                    lastName  = Str(f,"lastName"),
                    email     = Str(f,"email"),
                    role      = role,
                });
            }
            return list;
        }

        // ── CREATE ADMIN ACCOUNT via Firebase Auth REST ───────────
        public static async Task CreateAdminAccountAsync(string email, string password, string firstName, string lastName, string role)
        {
            // 1. Create Firebase Auth user
            var signupUrl  = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={API_KEY}";
            var body       = JsonSerializer.Serialize(new { email, password, returnSecureToken = true });
            var resp       = await Http.PostAsync(signupUrl, new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
            var json       = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                var err = JsonSerializer.Deserialize<FbErrWrap>(json);
                throw new Exception(err?.error?.message ?? "Could not create account.");
            }
            var auth = JsonSerializer.Deserialize<FbAuthResult>(json)!;
            string uid = auth.localId;

            // 2. Write user doc to Firestore (using god's auth token)
            var fsUrl  = $"{FS_BASE}{ProjectId}/databases/(default)/documents/users/{uid}";
            var fsBody = JsonSerializer.Serialize(new
            {
                fields = new
                {
                    firstName  = new { stringValue = firstName },
                    lastName   = new { stringValue = lastName },
                    email      = new { stringValue = email },
                    role       = new { stringValue = role },
                    createdAt  = new { stringValue = DateTime.UtcNow.ToString("O") },
                }
            });
            var fsResp = await Http.PatchAsync(fsUrl, new StringContent(fsBody, System.Text.Encoding.UTF8, "application/json"));
            if (!fsResp.IsSuccessStatusCode)
                throw new Exception("Account created but profile save failed.");
        }

        // ── DELETE ADMIN ACCOUNT ─────────────────────────────────
        public static async Task DeleteAdminAccountAsync(string uid)
        {
            var fsUrl = $"{FS_BASE}{ProjectId}/databases/(default)/documents/users/{uid}";
            await Http.DeleteAsync(fsUrl);
        }

        // ── GET PENDING TEACHER APPLICATIONS ─────────────────────
        public static async Task<List<TeacherApplication>> GetPendingApplicationsAsync()
        {
            var url  = $"{FS_BASE}{ProjectId}/databases/(default)/documents/pending_teachers?pageSize=100";
            var resp = await Http.GetAsync(url);
            var list = new List<TeacherApplication>();
            if (!resp.IsSuccessStatusCode) return list;
            var doc  = JsonSerializer.Deserialize<FsListResp>(await resp.Content.ReadAsStringAsync());
            foreach (var d in doc?.documents ?? new())
            {
                var f = d.fields ?? new();
                if (Str(f,"status") != "pending") continue;
                list.Add(new TeacherApplication
                {
                    Id        = d.name?.Split('/')[^1] ?? "",
                    FirstName = Str(f,"firstName"),
                    LastName  = Str(f,"lastName"),
                    Email     = Str(f,"email"),
                    Subject   = Str(f,"subject"),
                    Bio       = Str(f,"bio"),
                });
            }
            return list;
        }

        // ── APPROVE TEACHER APPLICATION ───────────────────────────
        public static async Task ApproveTeacherAsync(string appDocId, string firstName, string lastName, string email, string tempPassword)
        {
            // Create Firebase Auth account
            await CreateAdminAccountAsync(email, tempPassword, firstName, lastName, "teacher");
            // Mark application approved
            var url  = $"{FS_BASE}{ProjectId}/databases/(default)/documents/pending_teachers/{appDocId}";
            var body = JsonSerializer.Serialize(new { fields = new { status = new { stringValue = "approved" } } });
            await Http.PatchAsync(url + "?updateMask.fieldPaths=status", new System.Net.Http.StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        }

        // ── REJECT TEACHER APPLICATION ────────────────────────────
        public static async Task RejectTeacherAsync(string appDocId)
        {
            var url  = $"{FS_BASE}{ProjectId}/databases/(default)/documents/pending_teachers/{appDocId}";
            var body = JsonSerializer.Serialize(new { fields = new { status = new { stringValue = "rejected" } } });
            await Http.PatchAsync(url + "?updateMask.fieldPaths=status", new System.Net.Http.StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        }

        // Firestore field helpers
        private static string Str(Dictionary<string,FsVal> f, string k) => f.TryGetValue(k,out var v) ? v.stringValue??""  : "";
        private static int?   Int(Dictionary<string,FsVal> f, string k)
        {
            if (!f.TryGetValue(k,out var v)) return null;
            if (int.TryParse(v.integerValue,out int i)) return i;
            if (int.TryParse(v.doubleValue, out int d)) return d;
            return null;
        }

        // JSON models
        private class FbAuthResult  { public string idToken{get;set;}=""; public string localId{get;set;}=""; }
        private class FbErrWrap     { public FbErr? error{get;set;} }
        private class FbErr         { public string message{get;set;}=""; }
        private class FsDoc         { public string? name{get;set;} public Dictionary<string,FsVal>? fields{get;set;} }
        private class FsListResp    { public List<FsDoc>? documents{get;set;} }
        private class FsVal
        {
            public string?      stringValue  {get;set;}
            public string?      integerValue {get;set;}
            public string?      doubleValue  {get;set;}
            public FsMapVal?    mapValue     {get;set;}
        }
        private class FsMapVal { public Dictionary<string,FsVal>? fields{get;set;} }
    }

    public class UserProfile
    {
        public string uid        {get;set;}="";
        public string firstName  {get;set;}="";
        public string lastName   {get;set;}="";
        public string email      {get;set;}="";
        public string role       {get;set;}="student";
        public string linkingKey {get;set;}="";
        public int?   grade      {get;set;}
        public string FullName   => $"{firstName} {lastName}".Trim();
        public string Initials   => $"{(firstName.Length>0?firstName[0]:'?')}{(lastName.Length>0?lastName[0]:'?')}".ToUpper();
    }

    public class ClassSession
    {
        public string Id{get;set;}=""; public string Subject{get;set;}="";
        public string Date{get;set;}=""; public string Time{get;set;}="";
        public string Tutor{get;set;}=""; public string Link{get;set;}="";
        public string Status{get;set;}="scheduled"; public string Note{get;set;}="";
        public DateTime? ParsedDate { get { if(DateTime.TryParse(Date,out var d))return d; return null; } }
        public bool IsToday => ParsedDate?.Date == DateTime.Today;
        public bool IsLive  => Status=="live";
        public bool IsSoon  => Status=="soon";
    }

    public class TeacherApplication
    {
        public string Id        { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName  { get; set; } = "";
        public string Email     { get; set; } = "";
        public string Subject   { get; set; } = "";
        public string Bio       { get; set; } = "";
        public string FullName  => $"{FirstName} {LastName}".Trim();
    }

    public class CourseInfo
    {
        public int Index{get;set;} public string Name{get;set;}="";
        public string Subject{get;set;}=""; public int Lessons{get;set;} public int Done{get;set;}
    }
}
