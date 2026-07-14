using System;
using System.Drawing;
using System.Windows.Forms;

namespace SmartScopeApp
{
    /// <summary>
    /// God panel — only accessible when logged in as role "god".
    /// Create and delete admin/teacher accounts.
    /// </summary>
    public class GodPanelForm : Form
    {
        private TextBox newEmailBox  = null!;
        private TextBox newPassBox   = null!;
        private TextBox newFirstBox  = null!;
        private TextBox newLastBox   = null!;
        private ComboBox roleBox     = null!;
        private Label   statusLbl    = null!;
        private FlowLayoutPanel adminList = null!;

        public GodPanelForm()
        {
            Build();
            LoadAdmins();
        }

        private void Build()
        {
            Text            = "God Panel — Manage Admin Accounts";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            Size            = new Size(620, 580);
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = SS.BgDark;

            // Title
            Controls.Add(new Label { Text = "Manage Admin Accounts", Font = SS.TitleFont(14f), ForeColor = SS.Red, AutoSize = true, Location = new Point(20, 16) });
            Controls.Add(new Label { Text = "Create or remove teacher/admin accounts. Only visible to the god account.", Font = SS.BodyFont(8.5f), ForeColor = SS.TextMuted, AutoSize = true, Location = new Point(20, 44) });

            // ── Create new admin ──────────────────────────────────
            int y = 74;
            Controls.Add(Sep(y)); y += 14;
            Controls.Add(new Label { Text = "CREATE NEW ACCOUNT", Font = SS.TitleFont(8f), ForeColor = SS.TextMuted, AutoSize = true, Location = new Point(20, y) }); y += 26;

            int fw = 260;
            Controls.Add(Lbl("First name", new Point(20, y)));
            Controls.Add(Lbl("Last name",  new Point(fw - 50, y))); y += 18;
            newFirstBox = TB(new Point(20, y), new Size(fw - 80, 30));
            newLastBox  = TB(new Point(fw - 50, y), new Size(fw - 80, 30));
            Controls.Add(newFirstBox); Controls.Add(newLastBox); y += 40;

            Controls.Add(Lbl("Email", new Point(20, y))); y += 18;
            newEmailBox = TB(new Point(20, y), new Size(fw + 100, 30));
            Controls.Add(newEmailBox); y += 40;

            Controls.Add(Lbl("Password (min 8 chars)", new Point(20, y))); y += 18;
            newPassBox = TB(new Point(20, y), new Size(fw + 100, 30), pwd: true);
            Controls.Add(newPassBox); y += 40;

            Controls.Add(Lbl("Role", new Point(20, y))); y += 18;
            roleBox = new ComboBox { Location = new Point(20, y), Size = new Size(200, 30), BackColor = SS.Surface2, ForeColor = SS.TextMain, DropDownStyle = ComboBoxStyle.DropDownList, Font = SS.BodyFont(10f) };
            roleBox.Items.AddRange(new[] { "teacher", "admin" });
            roleBox.SelectedIndex = 0;
            Controls.Add(roleBox); y += 44;

            statusLbl = new Label { Text = "", Font = SS.BodyFont(9f), ForeColor = SS.Green, AutoSize = true, Location = new Point(20, y) };
            Controls.Add(statusLbl); y += 22;

            var createBtn = new Button { Text = "Create account", Font = SS.TitleFont(10f), Size = new Size(200, 40), Location = new Point(20, y) };
            SS.StyleButton(createBtn, SS.Purple, SS.White);
            createBtn.Click += CreateAccount;
            Controls.Add(createBtn); y += 56;

            // ── Existing admins ───────────────────────────────────
            Controls.Add(Sep(y)); y += 14;
            Controls.Add(new Label { Text = "EXISTING ADMINS & TEACHERS", Font = SS.TitleFont(8f), ForeColor = SS.TextMuted, AutoSize = true, Location = new Point(20, y) }); y += 26;

            adminList = new FlowLayoutPanel
            {
                Location      = new Point(20, y),
                Size          = new Size(560, 160),
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                AutoScroll    = true,
            };
            Controls.Add(adminList);
        }

        private async void CreateAccount(object? s, EventArgs e)
        {
            string email     = newEmailBox.Text.Trim();
            string pass      = newPassBox.Text;
            string firstName = newFirstBox.Text.Trim();
            string lastName  = newLastBox.Text.Trim();
            string role      = roleBox.SelectedItem?.ToString() ?? "teacher";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(firstName))
            { ShowStatus("Fill in all fields.", SS.Red); return; }
            if (pass.Length < 8)
            { ShowStatus("Password must be at least 8 characters.", SS.Red); return; }

            ShowStatus("Creating...", SS.TextMuted);
            try
            {
                await ApiClient.CreateAdminAccountAsync(email, pass, firstName, lastName, role);
                ShowStatus($"{firstName} ({role}) created successfully!", SS.Green);
                newEmailBox.Clear(); newPassBox.Clear(); newFirstBox.Clear(); newLastBox.Clear();
                LoadAdmins();
            }
            catch (Exception ex) { ShowStatus("Error: " + ex.Message, SS.Red); }
        }

        private async void LoadAdmins()
        {
            adminList.Controls.Clear();
            adminList.Controls.Add(new Label { Text = "Loading...", Font = SS.BodyFont(9f), ForeColor = SS.TextMuted, AutoSize = true });
            try
            {
                // Load pending applications first
                var apps = await ApiClient.GetPendingApplicationsAsync();
                adminList.Controls.Clear();

                if (apps.Count > 0)
                {
                    adminList.Controls.Add(new Label { Text = $"PENDING APPLICATIONS ({apps.Count})", Font = SS.TitleFont(8f), ForeColor = SS.Yellow, AutoSize = true, Margin = new Padding(0,0,0,6) });
                    foreach (var app in apps)
                    {
                        var row = new Panel { Size = new Size(540, 64), BackColor = Color.FromArgb(40, 30, 10), Margin = new Padding(0,0,0,6) };
                        row.Controls.Add(new Label { Text = app.FullName, Font = SS.TitleFont(9.5f), ForeColor = SS.TextMain, AutoSize = true, Location = new Point(12, 8) });
                        row.Controls.Add(new Label { Text = $"{app.Email}  ·  {app.Subject}", Font = SS.BodyFont(8.5f), ForeColor = SS.TextMuted, AutoSize = true, Location = new Point(12, 28) });
                        if (!string.IsNullOrEmpty(app.Bio))
                            row.Controls.Add(new Label { Text = app.Bio.Length > 60 ? app.Bio[..60]+"..." : app.Bio, Font = SS.BodyFont(8f), ForeColor = SS.TextMuted, AutoSize = true, Location = new Point(12, 46) });

                        var approveBtn = new Button { Text = "Approve", Font = SS.BodyFont(8f), Size = new Size(70, 26), Location = new Point(390, 10) };
                        SS.StyleButton(approveBtn, Color.FromArgb(20,60,30), SS.Green);
                        var rejectBtn  = new Button { Text = "Reject",  Font = SS.BodyFont(8f), Size = new Size(62, 26), Location = new Point(468, 10) };
                        SS.StyleButton(rejectBtn, Color.FromArgb(60,20,20), SS.Red);

                        var appCopy = app;
                        approveBtn.Click += async (s, e) =>
                        {
                            using var passForm = new Form { Text="Set temp password", Size=new Size(340,140), StartPosition=FormStartPosition.CenterParent, BackColor=SS.BgDark };
                            var tb = new TextBox { Location=new Point(20,20), Size=new Size(280,28), BackColor=SS.Surface2, ForeColor=SS.TextMain, BorderStyle=BorderStyle.FixedSingle };
                            tb.PlaceholderText = "Temporary password (min 8 chars)";
                            var ok = new Button { Text="Approve", Location=new Point(20,60), Size=new Size(130,34) };
                            SS.StyleButton(ok, SS.Green, SS.White);
                            ok.Click += (s2,e2)=>passForm.DialogResult=DialogResult.OK;
                            passForm.Controls.AddRange(new Control[]{tb,ok});
                            if (passForm.ShowDialog(this) == DialogResult.OK && tb.Text.Length >= 8)
                            {
                                try
                                {
                                    await ApiClient.ApproveTeacherAsync(appCopy.Id, appCopy.FirstName, appCopy.LastName, appCopy.Email, tb.Text);
                                    MessageBox.Show($"{appCopy.FirstName} approved!
Temp password: {tb.Text}
Ask them to change it after first login.", "Approved");
                                    LoadAdmins();
                                }
                                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
                            }
                        };
                        rejectBtn.Click += async (s, e) =>
                        {
                            if (MessageBox.Show($"Reject {appCopy.FullName}?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                            { await ApiClient.RejectTeacherAsync(appCopy.Id); LoadAdmins(); }
                        };
                        row.Controls.Add(approveBtn); row.Controls.Add(rejectBtn);
                        adminList.Controls.Add(row);
                    }
                    adminList.Controls.Add(new Label { Text = "", AutoSize = true, Margin = new Padding(0,4,0,4) });
                }

                // Then show existing admins
                var admins = await ApiClient.GetAdminAccountsAsync();
                adminList.Controls.Add(new Label { Text = "EXISTING ADMINS & TEACHERS", Font = SS.TitleFont(8f), ForeColor = SS.TextMuted, AutoSize = true, Margin = new Padding(0,4,0,6) });
                if (admins.Count == 0) { adminList.Controls.Add(new Label { Text = "No admin accounts yet.", Font = SS.BodyFont(9f), ForeColor = SS.TextMuted, AutoSize = true }); return; }
                foreach (var a in admins)
                {
                    var row = new Panel { Size = new Size(540, 46), BackColor = SS.Surface, Margin = new Padding(0,0,0,6) };
                    row.Controls.Add(new Label { Text = $"{a.firstName} {a.lastName}", Font = SS.TitleFont(9.5f), ForeColor = SS.TextMain, AutoSize = true, Location = new Point(12, 8) });
                    row.Controls.Add(new Label { Text = $"{a.email}  ·  {a.role}", Font = SS.BodyFont(8.5f), ForeColor = SS.TextMuted, AutoSize = true, Location = new Point(12, 28) });
                    if (a.role != "god")
                    {
                        var del = new Button { Text = "Remove", Font = SS.BodyFont(8.5f), Size = new Size(70, 28), Location = new Point(460, 9) };
                        SS.StyleButton(del, Color.FromArgb(60,20,20), SS.Red);
                        string uid = a.uid;
                        del.Click += async (s, e) =>
                        {
                            if (MessageBox.Show($"Remove {a.firstName}?","Confirm",MessageBoxButtons.YesNo)==DialogResult.Yes)
                            { await ApiClient.DeleteAdminAccountAsync(uid); LoadAdmins(); }
                        };
                        row.Controls.Add(del);
                    }
                    adminList.Controls.Add(row);
                }
            }
            catch (Exception ex)
            {
                adminList.Controls.Clear();
                adminList.Controls.Add(new Label { Text = "Error: " + ex.Message, Font = SS.BodyFont(9f), ForeColor = SS.Red, AutoSize = true });
            }
        }

        private void ShowStatus(string msg, Color color)
        {
            statusLbl.Text = msg; statusLbl.ForeColor = color;
        }

        private Panel Sep(int y) => new Panel { Location = new Point(20, y), Size = new Size(560, 1), BackColor = SS.Border };
        private Label Lbl(string t, Point p) => new Label { Text = t, Font = SS.BodyFont(8.5f), ForeColor = SS.TextMuted, AutoSize = true, Location = p };
        private TextBox TB(Point p, Size s, bool pwd = false) => new TextBox { Location = p, Size = s, BackColor = SS.Surface2, ForeColor = SS.TextMain, BorderStyle = BorderStyle.FixedSingle, Font = SS.BodyFont(10f), PasswordChar = pwd ? (char)9679 : '\0' };
    }
}
