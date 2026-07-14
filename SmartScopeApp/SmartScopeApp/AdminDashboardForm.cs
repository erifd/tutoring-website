using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmartScopeApp
{
    /// <summary>
    /// Teacher/Admin dashboard — not locked down, normal window.
    /// Shows students, classes, homework, grades management.
    /// </summary>
    public class AdminDashboardForm : Form
    {
        private readonly UserProfile user;
        private Panel topBar    = null!;
        private Panel mainArea  = null!;
        private Panel sidebar   = null!;
        private Button navStudents  = null!;
        private Button navClasses   = null!;
        private Button navHomework  = null!;
        private Button navGrades    = null!;
        private Panel studentsPanel = null!;
        private Panel classesPanel  = null!;

        public AdminDashboardForm(UserProfile u)
        {
            user = u;
            Build();
        }

        private void Build()
        {
            Text            = "SmartScope Admin";
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState     = FormWindowState.Maximized;
            BackColor       = SS.BgDark;
            MinimumSize     = new Size(900, 600);

            BuildTopBar();
            BuildSidebar();
            BuildMain();
            BuildStudentsPanel();
            BuildClassesPanel();

            ShowPanel(studentsPanel, navStudents);
        }

        // ── TOP BAR ──────────────────────────────────────────────
        private void BuildTopBar()
        {
            topBar = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = SS.Surface };
            Controls.Add(topBar);

            topBar.Controls.Add(new Label { Text = "SmartScope", Font = SS.TitleFont(14f), ForeColor = SS.Accent, AutoSize = true, Location = new Point(20, 14) });

            var roleTag = new Label
            {
                Text      = user.role.ToUpper(),
                Font      = SS.TitleFont(7.5f),
                ForeColor = SS.White,
                BackColor = user.role == "god" ? SS.Red : SS.Purple,
                AutoSize  = true,
                Location  = new Point(150, 18),
                Padding   = new Padding(6, 3, 6, 3),
            };
            topBar.Controls.Add(roleTag);

            int sw = Screen.PrimaryScreen!.Bounds.Width;
            topBar.Controls.Add(new Label { Text = $"{user.firstName} {user.lastName}", Font = SS.TitleFont(9f), ForeColor = SS.TextMuted, AutoSize = true, Location = new Point(sw - 280, 18) });

            var exitBtn = new Button { Text = "Log out", Font = SS.BodyFont(9f), Size = new Size(80, 30), Location = new Point(sw - 110, 12) };
            SS.StyleButton(exitBtn, SS.Surface2, SS.TextMuted);
            exitBtn.Click += (s, e) => { Application.Exit(); };
            topBar.Controls.Add(exitBtn);
        }

        // ── SIDEBAR ───────────────────────────────────────────────
        private void BuildSidebar()
        {
            sidebar = new Panel { Width = 210, BackColor = SS.Surface, Dock = DockStyle.Left };
            Controls.Add(sidebar);

            int y = 74;
            sidebar.Controls.Add(SLabel("MANAGE", y)); y += 28;

            navStudents = SideBtn("👥  Students",  y, () => ShowPanel(studentsPanel, navStudents)); y += 44;
            navClasses  = SideBtn("📅  Classes",   y, () => ShowPanel(classesPanel,  navClasses));  y += 44;
            navHomework = SideBtn("📝  Homework",  y, () => OpenWeb("homework.html")); y += 44;
            navGrades   = SideBtn("📊  Grades",    y, () => OpenWeb("tutoring-site.html")); y += 44;

            sidebar.Controls.Add(navStudents); sidebar.Controls.Add(navClasses);
            sidebar.Controls.Add(navHomework); sidebar.Controls.Add(navGrades);

            // God-only section
            if (user.role == "god")
            {
                y += 16;
                sidebar.Controls.Add(SLabel("GOD CONTROLS", y)); y += 28;
                var manageAdmins = SideBtn("⚡  Manage Admins", y, ShowGodPanel);
                manageAdmins.ForeColor = SS.Red;
                sidebar.Controls.Add(manageAdmins);
            }
        }

        // ── MAIN ─────────────────────────────────────────────────
        private void BuildMain()
        {
            mainArea = new Panel { Dock = DockStyle.Fill, BackColor = SS.BgDark, Padding = new Padding(24, 20, 24, 20) };
            Controls.Add(mainArea);
        }

        // ── STUDENTS PANEL ────────────────────────────────────────
        private void BuildStudentsPanel()
        {
            studentsPanel = new Panel { Dock = DockStyle.Fill, BackColor = SS.BgDark };
            studentsPanel.Controls.Add(PTitle("Students", "All registered students."));

            var list = new FlowLayoutPanel
            {
                Name          = "list",
                Location      = new Point(0, 78),
                Size          = new Size(900, 700),
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                AutoScroll    = true,
            };
            studentsPanel.Controls.Add(list);

            // Load students async
            LoadStudentsAsync(list);
        }

        private async void LoadStudentsAsync(FlowLayoutPanel list)
        {
            list.Controls.Clear();
            list.Controls.Add(new Label { Text = "Loading...", Font = SS.BodyFont(9f), ForeColor = SS.TextMuted, AutoSize = true });

            try
            {
                var students = await ApiClient.GetAllStudentsAsync();
                if (InvokeRequired) { Invoke(() => RenderStudents(list, students)); }
                else RenderStudents(list, students);
            }
            catch (Exception ex)
            {
                list.Controls.Clear();
                list.Controls.Add(new Label { Text = "Could not load students: " + ex.Message, Font = SS.BodyFont(9f), ForeColor = SS.Red, AutoSize = true });
            }
        }

        private void RenderStudents(FlowLayoutPanel list, List<UserProfile> students)
        {
            list.Controls.Clear();
            if (students.Count == 0)
            {
                list.Controls.Add(new Label { Text = "No students registered yet.", Font = SS.BodyFont(10f), ForeColor = SS.TextMuted, AutoSize = true });
                return;
            }

            foreach (var s in students)
            {
                int rowW = Math.Min(mainArea.Width - 60, 900);
                var row = new Panel { Size = new Size(rowW, 60), BackColor = SS.Surface, Margin = new Padding(0, 0, 0, 8) };

                var av = new Label { Text = s.Initials, Font = SS.TitleFont(10f), ForeColor = SS.Accent, BackColor = SS.AccentLt, AutoSize = false, Size = new Size(38, 38), Location = new Point(12, 11), TextAlign = ContentAlignment.MiddleCenter };
                row.Controls.Add(av);
                row.Controls.Add(new Label { Text = s.FullName, Font = SS.TitleFont(10f), ForeColor = SS.TextMain, AutoSize = true, Location = new Point(60, 10) });
                row.Controls.Add(new Label { Text = s.email + (s.grade.HasValue ? $"  ·  Grade {s.grade}" : ""), Font = SS.BodyFont(8.5f), ForeColor = SS.TextMuted, AutoSize = true, Location = new Point(60, 32) });
                row.Controls.Add(new Label { Text = string.IsNullOrEmpty(s.linkingKey) ? "" : $"Code: {s.linkingKey}", Font = SS.MonoFont(9f), ForeColor = SS.Accent, AutoSize = true, Location = new Point(rowW - 140, 22) });

                list.Controls.Add(row);
            }
        }

        // ── CLASSES PANEL ─────────────────────────────────────────
        private void BuildClassesPanel()
        {
            classesPanel = new Panel { Dock = DockStyle.Fill, BackColor = SS.BgDark };
            classesPanel.Controls.Add(PTitle("Classes", "Manage student class sessions."));

            var note = new Label
            {
                Text      = "Add and manage classes from the admin panel on the website (smartscope-tutoring.onrender.com).",
                Font      = SS.BodyFont(9.5f),
                ForeColor = SS.TextMuted,
                AutoSize  = false,
                Size      = new Size(700, 40),
                Location  = new Point(0, 80),
            };
            classesPanel.Controls.Add(note);

            var openBtn = new Button { Text = "Open Admin Panel on Website", Font = SS.TitleFont(10f), Size = new Size(280, 42), Location = new Point(0, 130) };
            SS.StyleButton(openBtn, SS.Accent, SS.White);
            openBtn.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://smartscope-tutoring.onrender.com/tutoring-site.html") { UseShellExecute = true });
            classesPanel.Controls.Add(openBtn);
        }

        // ── GOD PANEL (manage admin accounts) ─────────────────────
        private void ShowGodPanel()
        {
            var godForm = new GodPanelForm();
            godForm.ShowDialog(this);
        }

        private void OpenWeb(string page)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo($"https://smartscope-tutoring.onrender.com/{page}") { UseShellExecute = true });
        }

        // ── PANEL SWITCHING ───────────────────────────────────────
        private void ShowPanel(Panel target, Button active)
        {
            studentsPanel.Visible = target == studentsPanel;
            classesPanel.Visible  = target == classesPanel;

            if (target.Parent == null) { target.Dock = DockStyle.Fill; mainArea.Controls.Add(target); }
            target.BringToFront();

            foreach (var b in new[] { navStudents, navClasses, navHomework, navGrades })
            { b.BackColor = b == active ? SS.AccentLt : SS.Surface; b.ForeColor = b == active ? SS.Accent : SS.TextMuted; }
        }

        // ── HELPERS ───────────────────────────────────────────────
        private Panel PTitle(string t, string sub) { var p = new Panel { Location = new Point(0, 0), Size = new Size(1200, 68), BackColor = Color.Transparent }; p.Controls.Add(new Label { Text = t, Font = SS.TitleFont(18f), ForeColor = SS.TextMain, AutoSize = true, Location = new Point(0, 0) }); p.Controls.Add(new Label { Text = sub, Font = SS.BodyFont(10f), ForeColor = SS.TextMuted, AutoSize = true, Location = new Point(2, 30) }); return p; }
        private Button SideBtn(string t, int y, Action a) { var b = new Button { Text = t, Font = SS.BodyFont(10f), Size = new Size(206, 38), Location = new Point(2, y), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(14, 0, 0, 0) }; SS.StyleButton(b, SS.Surface, SS.TextMuted); b.Click += (s, e) => a(); return b; }
        private Label SLabel(string t, int y) => new Label { Text = t, Font = SS.TitleFont(7.5f), ForeColor = SS.Border, AutoSize = true, Location = new Point(16, y) };
    }
}
