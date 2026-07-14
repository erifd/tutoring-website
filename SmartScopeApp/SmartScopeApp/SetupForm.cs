using System;
using System.Drawing;
using System.Windows.Forms;

namespace SmartScopeApp
{
    /// <summary>
    /// Shown before the kiosk launches — lets admin set student name,
    /// class URL, duration, and password before locking down.
    /// </summary>
    public class SetupForm : Form
    {
        private SessionConfig config;
        private TextBox  txtStudentName  = null!;
        private TextBox  txtStudentEmail = null!;
        private TextBox  txtClassUrl     = null!;
        private TextBox  txtSubject      = null!;
        private NumericUpDown numDuration = null!;
        private TextBox  txtAdminPass    = null!;
        private CheckBox chkAutoStart    = null!;
        private CheckBox chkSuppressApps = null!;
        private Button   btnLaunch       = null!;

        public SetupForm()
        {
            config = SessionConfig.Load();
            InitSetupForm();
        }

        private void InitSetupForm()
        {
            this.Text            = "SmartScope — Session Setup";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.Size            = new Size(500, 580);
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.BackColor       = Color.FromArgb(22, 27, 34);
            this.ForeColor       = Color.White;
            this.Font            = new Font("Segoe UI", 10f);

            int y = 20;

            // ── Header ──────────────────────────────────────────
            var header = new Label
            {
                Text      = "SmartScope",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.FromArgb(79, 110, 247),
                AutoSize  = true,
                Location  = new Point(20, y),
            };
            this.Controls.Add(header);
            y += 40;

            var subHeader = new Label
            {
                Text      = "Set up the class session before locking the screen.",
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(139, 148, 158),
                AutoSize  = true,
                Location  = new Point(20, y),
            };
            this.Controls.Add(subHeader);
            y += 36;

            // ── Fields ──────────────────────────────────────────
            txtStudentName  = AddField("Student name",   config.StudentName,  ref y);
            txtStudentEmail = AddField("Student email",  config.StudentEmail, ref y);
            txtSubject      = AddField("Subject",        config.Subject,      ref y);
            txtClassUrl     = AddField("Class URL",      config.ClassUrl,     ref y);

            // Duration
            AddLabel("Session duration (minutes)", y);
            numDuration = new NumericUpDown
            {
                Minimum   = 10,
                Maximum   = 240,
                Value     = config.DurationMinutes,
                Increment = 5,
                Size      = new Size(440, 30),
                Location  = new Point(20, y + 22),
                BackColor = Color.FromArgb(13, 17, 23),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
            };
            this.Controls.Add(numDuration);
            y += 60;

            // Admin password
            txtAdminPass = AddField("Admin exit password", config.AdminPassword, ref y, isPassword: true);

            // Checkboxes
            chkAutoStart = new CheckBox
            {
                Text      = "Launch SmartScope automatically on Windows login",
                Checked   = config.AutoStartEnabled,
                Location  = new Point(20, y),
                AutoSize  = true,
                ForeColor = Color.FromArgb(200, 210, 230),
            };
            this.Controls.Add(chkAutoStart);
            y += 28;

            chkSuppressApps = new CheckBox
            {
                Text      = "Close distracting apps when session starts (Discord, Spotify...)",
                Checked   = config.SuppressApps,
                Location  = new Point(20, y),
                AutoSize  = true,
                ForeColor = Color.FromArgb(200, 210, 230),
            };
            this.Controls.Add(chkSuppressApps);
            y += 40;

            // ── Launch button ───────────────────────────────────
            btnLaunch = new Button
            {
                Text      = "🔒  Start Class Session",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                Size      = new Size(440, 50),
                Location  = new Point(20, y),
                BackColor = Color.FromArgb(79, 110, 247),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
            };
            btnLaunch.FlatAppearance.BorderSize = 0;
            btnLaunch.Click += BtnLaunch_Click;
            this.Controls.Add(btnLaunch);

            this.ClientSize = new Size(480, y + 70);
        }

        private TextBox AddField(string label, string value, ref int y, bool isPassword = false)
        {
            AddLabel(label, y);
            var tb = new TextBox
            {
                Text        = value,
                Size        = new Size(440, 28),
                Location    = new Point(20, y + 22),
                BackColor   = Color.FromArgb(13, 17, 23),
                ForeColor   = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font        = new Font("Segoe UI", 10f),
            };
            if (isPassword) tb.PasswordChar = '●';
            this.Controls.Add(tb);
            y += 58;
            return tb;
        }

        private void AddLabel(string text, int y)
        {
            this.Controls.Add(new Label
            {
                Text      = text,
                ForeColor = Color.FromArgb(139, 148, 158),
                AutoSize  = true,
                Location  = new Point(20, y),
                Font      = new Font("Segoe UI", 8.5f),
            });
        }

        private void BtnLaunch_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtStudentName.Text))
            {
                MessageBox.Show("Please enter the student name.", "Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtAdminPass.Text) || txtAdminPass.Text.Length < 6)
            {
                MessageBox.Show("Admin password must be at least 6 characters.", "Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Save config
            config.StudentName     = txtStudentName.Text.Trim();
            config.StudentEmail    = txtStudentEmail.Text.Trim();
            config.Subject         = txtSubject.Text.Trim();
            config.ClassUrl        = txtClassUrl.Text.Trim();
            config.DurationMinutes = (int)numDuration.Value;
            config.AdminPassword   = txtAdminPass.Text;
            config.AutoStartEnabled = chkAutoStart.Checked;
            config.SuppressApps    = chkSuppressApps.Checked;
            config.Save();

            // Handle auto-start registry
            if (chkAutoStart.Checked) KioskLauncher.EnableAutoStart();
            else                      KioskLauncher.DisableAutoStart();

            // Suppress apps if requested
            if (chkSuppressApps.Checked)
                KioskLauncher.SuppressDistractingApps();

            // Launch kiosk
            var kiosk = new KioskForm(config);
            kiosk.Show();
            kiosk.StartSession(config.DurationMinutes);
            this.Hide();
        }
    }
}
