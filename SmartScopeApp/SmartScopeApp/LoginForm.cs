using System;
using System.Drawing;
using System.Windows.Forms;

namespace SmartScopeApp
{
    public class LoginForm : Form
    {
        // ── Tab state ─────────────────────────────────────────────
        private bool isTeacherTab = false;

        // ── Controls ──────────────────────────────────────────────
        private TextBox emailBox  = null!;
        private TextBox passBox   = null!;
        private Button  loginBtn  = null!;
        private Label   errorLbl  = null!;
        private Button  tabStudent = null!;
        private Button  tabTeacher = null!;
        private Label   tabDesc   = null!;

        public LoginForm() { Build(); }

        private void Build()
        {
            int sw   = Screen.PrimaryScreen!.Bounds.Width;
            int sh   = Screen.PrimaryScreen!.Bounds.Height;
            int winW = sw / 2;
            int winH = sh / 2;

            Text            = "SmartScope";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox     = false;
            ClientSize      = new Size(winW, winH);
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = SS.BgDark;
            KeyPreview      = true;
            KeyDown        += (s, e) => { if (e.KeyCode == Keys.Enter) DoLogin(); };

            // ── Left branding panel ───────────────────────────────
            var left = new Panel
            {
                Size      = new Size(winW / 2, winH),
                Location  = new Point(0, 0),
                BackColor = SS.Surface,
            };
            Controls.Add(left);

            int lx = 30, ly = winH / 2 - 110;

            int smartW = TextRenderer.MeasureText("Smart", SS.TitleFont(22f)).Width;
            left.Controls.Add(new Label { Text = "Smart", Font = SS.TitleFont(22f), ForeColor = SS.TextMain, AutoSize = true, Location = new Point(lx, ly) });
            left.Controls.Add(new Label { Text = "Scope", Font = SS.TitleFont(22f), ForeColor = SS.Accent,   AutoSize = true, Location = new Point(lx + smartW - 4, ly) });
            left.Controls.Add(new Label { Text = "Your focused learning environment.", Font = SS.BodyFont(9f), ForeColor = SS.TextMuted, AutoSize = true, Location = new Point(lx, ly + 46) });

            string[] feats = { "  Focus mode during class", "  See your full schedule", "  Launch live sessions", "  Track course progress" };
            for (int i = 0; i < feats.Length; i++)
                left.Controls.Add(new Label { Text = feats[i], Font = SS.BodyFont(8.5f), ForeColor = SS.Green, AutoSize = true, Location = new Point(lx, ly + 84 + i * 26) });

            left.Controls.Add(new Label { Text = "SmartScope Desktop v1.0", Font = SS.BodyFont(7.5f), ForeColor = SS.Border, AutoSize = true, Location = new Point(lx, winH - 26) });

            // ── Right login panel ─────────────────────────────────
            var right = new Panel
            {
                Size      = new Size(winW / 2, winH),
                Location  = new Point(winW / 2, 0),
                BackColor = SS.BgDark,
            };
            Controls.Add(right);

            int fw   = winW / 2 - 48;
            var card = new Panel
            {
                Size      = new Size(fw + 28, winH - 32),
                Location  = new Point(10, 16),
                BackColor = SS.Surface,
            };
            right.Controls.Add(card);

            // ── Role tabs ─────────────────────────────────────────
            int tabY = 18;
            tabStudent = MakeTab("Student", new Point(20, tabY), active: true);
            tabTeacher = MakeTab("Teacher", new Point(20 + fw / 2 + 4, tabY), active: false);
            tabStudent.Click += (s, e) => SwitchTab(false);
            tabTeacher.Click += (s, e) => SwitchTab(true);
            card.Controls.Add(tabStudent);
            card.Controls.Add(tabTeacher);

            int y = 66;

            // Title
            card.Controls.Add(Lbl("Welcome back!", SS.TitleFont(12f), SS.TextMain,  new Point(20, y), new Size(fw, 28))); y += 30;

            // Description (changes by tab)
            tabDesc = Lbl("Sign in to access your classes and courses.", SS.BodyFont(8.5f), SS.TextMuted, new Point(20, y), new Size(fw, 18));
            card.Controls.Add(tabDesc); y += 28;

            // Email
            card.Controls.Add(Lbl("Email", SS.BodyFont(8.5f), SS.TextMuted, new Point(20, y), new Size(100, 16))); y += 18;
            emailBox = MakeTB(new Point(20, y), new Size(fw, 32));
            emailBox.PlaceholderText = "you@email.com";
            card.Controls.Add(emailBox); y += 42;

            // Password
            card.Controls.Add(Lbl("Password", SS.BodyFont(8.5f), SS.TextMuted, new Point(20, y), new Size(100, 16))); y += 18;
            passBox = MakeTB(new Point(20, y), new Size(fw, 32), pwd: true);
            passBox.PlaceholderText = "••••••••";
            passBox.KeyDown += (s2, e2) => { if (e2.KeyCode == Keys.Enter) DoLogin(); };
            card.Controls.Add(passBox); y += 42;

            // Error
            errorLbl = new Label { Text = "", Font = SS.BodyFont(8.5f), ForeColor = SS.Red, AutoSize = false, Size = new Size(fw, 18), Location = new Point(20, y), Visible = false };
            card.Controls.Add(errorLbl); y += 22;

            // Login button
            loginBtn = new Button { Text = "Sign in as Student", Font = SS.TitleFont(10f), Size = new Size(fw, 40), Location = new Point(20, y) };
            SS.StyleButton(loginBtn, SS.Accent, SS.White);
            loginBtn.Click += (s, e) => DoLogin();
            card.Controls.Add(loginBtn);
        }

        // ── Switch between Student / Teacher tabs ─────────────────
        private void SwitchTab(bool teacher)
        {
            isTeacherTab = teacher;
            errorLbl.Visible = false;
            emailBox.Clear(); passBox.Clear();

            if (teacher)
            {
                tabTeacher.BackColor = SS.Accent;   tabTeacher.ForeColor = SS.White;
                tabStudent.BackColor = SS.Surface2;  tabStudent.ForeColor = SS.TextMuted;
                tabDesc.Text   = "Sign in with your teacher or admin account.";
                loginBtn.Text  = "Sign in as Teacher";
                SS.StyleButton(loginBtn, SS.Purple, SS.White);
            }
            else
            {
                tabStudent.BackColor = SS.Accent;   tabStudent.ForeColor = SS.White;
                tabTeacher.BackColor = SS.Surface2;  tabTeacher.ForeColor = SS.TextMuted;
                tabDesc.Text   = "Sign in to access your classes and courses.";
                loginBtn.Text  = "Sign in as Student";
                SS.StyleButton(loginBtn, SS.Accent, SS.White);
            }
        }

        // ── Login logic ───────────────────────────────────────────
        private async void DoLogin()
        {
            string email = emailBox.Text.Trim();
            string pass  = passBox.Text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            { ShowErr("Please enter your email and password."); return; }

            loginBtn.Text = "Signing in..."; loginBtn.Enabled = false; errorLbl.Visible = false;

            try
            {
                var user = await ApiClient.LoginAsync(email, pass);

                if (isTeacherTab)
                {
                    // Teacher tab — must be admin, teacher, or god role
                    if (user.role != "admin" && user.role != "teacher" && user.role != "god")
                    {
                        ShowErr("This account is not a teacher account. Use the Student tab.");
                        await ApiClient.LogoutAsync();
                        return;
                    }
                    var adminDash = new AdminDashboardForm(user);
                    adminDash.Show();
                    Hide();
                }
                else
                {
                    // Student tab — must be student role
                    if (user.role != "student")
                    {
                        ShowErr("This is not a student account. Use the Teacher tab.");
                        await ApiClient.LogoutAsync();
                        return;
                    }
                    var dash = new DashboardForm(user);
                    dash.Show();
                    Hide();
                }
            }
            catch (Exception ex) { ShowErr(ex.Message); }
            finally { loginBtn.Text = isTeacherTab ? "Sign in as Teacher" : "Sign in as Student"; loginBtn.Enabled = true; }
        }

        private void ShowErr(string m) { errorLbl.Text = m; errorLbl.Visible = true; }

        // ── UI helpers ────────────────────────────────────────────
        private Button MakeTab(string text, Point loc, bool active)
        {
            var b = new Button
            {
                Text      = text,
                Font      = SS.TitleFont(9f),
                Size      = new Size((ClientSize.Width / 2 - 48) / 2, 34),
                Location  = loc,
                BackColor = active ? SS.Accent : SS.Surface2,
                ForeColor = active ? SS.White  : SS.TextMuted,
            };
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.Cursor = Cursors.Hand;
            return b;
        }

        private Label Lbl(string t, Font f, Color c, Point p, Size s) =>
            new Label { Text = t, Font = f, ForeColor = c, Location = p, Size = s, AutoSize = false };

        private TextBox MakeTB(Point p, Size s, bool pwd = false) =>
            new TextBox { Location = p, Size = s, BackColor = SS.Surface2, ForeColor = SS.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = SS.BodyFont(10f), PasswordChar = pwd ? (char)9679 : '\0' };
    }
}
