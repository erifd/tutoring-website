using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace SmartScopeApp
{
    public class KioskForm : Form
    {
        // ── Win32 API imports for kiosk lockdown ─────────────────
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        // ── Hook types ────────────────────────────────────────────
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelKeyboardProc? _keyboardProc;
        private IntPtr _hookID = IntPtr.Zero;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN     = 0x0100;
        private const int WM_SYSKEYDOWN  = 0x0104;

        // Virtual key codes to block
        private const uint VK_TAB    = 0x09;
        private const uint VK_ESCAPE = 0x1B;
        private const uint VK_F4     = 0x73;
        private const uint VK_F11    = 0x7A;
        private const uint VK_LWIN   = 0x5B;
        private const uint VK_RWIN   = 0x5C;
        private const uint VK_DELETE  = 0x2E;

        // ── UI Controls ───────────────────────────────────────────
        private WebView2 webView = null!;
        private Panel    topBar  = null!;
        private Label    titleLabel = null!;
        private Label    timerLabel = null!;
        private Panel    loginPanel = null!;
        private System.Windows.Forms.Timer sessionTimer = null!;
        private System.Windows.Forms.Timer clockTimer   = null!;

        private DateTime sessionStart;
        private bool     sessionActive = false;
        private string   adminPassword = "Admin2026!";
        private int      sessionDurationMinutes = 60;
        private SessionConfig config;

        public KioskForm(SessionConfig? cfg = null)
        {
            config                = cfg ?? SessionConfig.Load();
            adminPassword         = config.AdminPassword;
            sessionDurationMinutes = config.DurationMinutes;
            InitializeKioskForm();
            InstallKeyboardHook();
            HideTaskbar();
        }

        // ── Form setup ────────────────────────────────────────────
        private void InitializeKioskForm()
        {
            this.Text = $"SmartScope — {config.Subject} — {config.StudentName}";
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState     = FormWindowState.Maximized;
            this.TopMost         = true;
            this.BackColor       = Color.FromArgb(13, 17, 23);
            this.ShowInTaskbar   = false;
            this.KeyPreview      = true;

            // Prevent Alt+F4
            this.FormClosing += (s, e) =>
            {
                if (sessionActive)
                {
                    e.Cancel = true;
                    ShowExitBlockedMessage();
                }
            };

            BuildTopBar();
            BuildLoginPanel();
            BuildWebView();
            BuildTimerLabels();

            // Clock update every second
            clockTimer          = new System.Windows.Forms.Timer();
            clockTimer.Interval = 1000;
            clockTimer.Tick    += ClockTimer_Tick;
            clockTimer.Start();
        }

        // ── Top bar ───────────────────────────────────────────────
        private void BuildTopBar()
        {
            topBar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 54,
                BackColor = Color.FromArgb(22, 27, 34),
            };

            // SmartScope logo label
            titleLabel = new Label
            {
                Text      = "SmartScope",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize  = true,
                Location  = new Point(20, 14),
            };
            topBar.Controls.Add(titleLabel);

            // Session label
            var sessionLabel = new Label
            {
                Text      = "CLASS SESSION",
                Font      = new Font("Segoe UI", 7f, FontStyle.Bold),
                ForeColor = Color.FromArgb(139, 148, 158),
                AutoSize  = true,
                Location  = new Point(150, 19),
            };
            topBar.Controls.Add(sessionLabel);

            // Live dot
            var liveDot = new Label
            {
                Text      = "● LIVE",
                Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(34, 197, 94),
                AutoSize  = true,
                Location  = new Point(260, 19),
            };
            topBar.Controls.Add(liveDot);

            // Timer label (right side)
            timerLabel = new Label
            {
                Text      = "00:00",
                Font      = new Font("Segoe UI Mono", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize  = true,
                Location  = new Point(Screen.PrimaryScreen!.Bounds.Width - 200, 16),
            };
            topBar.Controls.Add(timerLabel);

            // Exit button (requires admin password)
            var exitBtn = new Button
            {
                Text      = "End Session",
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(239, 68, 68),
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(110, 32),
                Location  = new Point(Screen.PrimaryScreen!.Bounds.Width - 140, 11),
                Cursor    = Cursors.Hand,
            };
            exitBtn.FlatAppearance.BorderSize = 0;
            exitBtn.Click += ExitBtn_Click;
            topBar.Controls.Add(exitBtn);

            this.Controls.Add(topBar);
        }

        // ── WebView2 (main content area) ──────────────────────────
        private void BuildWebView()
        {
            webView = new WebView2
            {
                Dock = DockStyle.Fill,
            };
            webView.CoreWebView2InitializationCompleted += WebView_Initialized;
            webView.EnsureCoreWebView2Async(null);
            this.Controls.Add(webView);
            webView.BringToFront();
            // topBar stays on top
            topBar.BringToFront();
        }

        private void WebView_Initialized(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (!e.IsSuccess) return;

            // Disable context menu and devtools in kiosk mode
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled  = false;
            webView.CoreWebView2.Settings.AreDevToolsEnabled              = false;
            webView.CoreWebView2.Settings.IsStatusBarEnabled              = false;
            webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled  = false;

            // Block new window/tab opens
            webView.CoreWebView2.NewWindowRequested += (s, args) =>
            {
                args.Handled = true; // block popups
                webView.CoreWebView2.Navigate(args.Uri); // load in same view
            };

            // Load the SmartScope student dashboard
            webView.CoreWebView2.Navigate(config.ClassUrl);
        }

        // ── Login panel (shown at start if needed) ────────────────
        private void BuildLoginPanel()
        {
            loginPanel = new Panel
            {
                Size      = new Size(400, 380),
                BackColor = Color.FromArgb(22, 27, 34),
                Visible   = false,
            };

            // Centre it
            loginPanel.Location = new Point(
                (Screen.PrimaryScreen!.Bounds.Width  - loginPanel.Width)  / 2,
                (Screen.PrimaryScreen!.Bounds.Height - loginPanel.Height) / 2
            );

            var lbl = new Label
            {
                Text      = "Admin password required to exit",
                Font      = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(139, 148, 158),
                AutoSize  = true,
                Location  = new Point(20, 20),
            };
            loginPanel.Controls.Add(lbl);

            var passBox = new TextBox
            {
                PasswordChar = '●',
                Font         = new Font("Segoe UI", 12f),
                Size         = new Size(360, 36),
                Location     = new Point(20, 60),
                BackColor    = Color.FromArgb(13, 17, 23),
                ForeColor    = Color.White,
                BorderStyle  = BorderStyle.FixedSingle,
                Name         = "passBox",
            };
            loginPanel.Controls.Add(passBox);

            var confirmBtn = new Button
            {
                Text      = "Exit Session",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                Size      = new Size(360, 46),
                Location  = new Point(20, 110),
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
            };
            confirmBtn.FlatAppearance.BorderSize = 0;
            confirmBtn.Click += (s, e) =>
            {
                if (passBox.Text == adminPassword)
                    ForceExit();
                else
                {
                    lbl.Text      = "❌ Wrong password. Try again.";
                    lbl.ForeColor = Color.FromArgb(239, 68, 68);
                    passBox.Clear();
                }
            };
            loginPanel.Controls.Add(confirmBtn);

            var cancelBtn = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 10f),
                Size      = new Size(360, 36),
                Location  = new Point(20, 166),
                BackColor = Color.FromArgb(48, 54, 61),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
            };
            cancelBtn.FlatAppearance.BorderSize = 0;
            cancelBtn.Click += (s, e) => loginPanel.Visible = false;
            loginPanel.Controls.Add(cancelBtn);

            this.Controls.Add(loginPanel);
            loginPanel.BringToFront();
        }

        private void BuildTimerLabels() { /* timerLabel already built in BuildTopBar */ }

        // ── Session timer ─────────────────────────────────────────
        public void StartSession(int durationMinutes = 60)
        {
            sessionActive          = true;
            sessionStart           = DateTime.Now;
            sessionDurationMinutes = durationMinutes;
            sessionTimer           = new System.Windows.Forms.Timer { Interval = 1000 };
            sessionTimer.Tick     += SessionTimer_Tick;
            sessionTimer.Start();
        }

        private void SessionTimer_Tick(object? sender, EventArgs e)
        {
            var elapsed   = DateTime.Now - sessionStart;
            var remaining = TimeSpan.FromMinutes(sessionDurationMinutes) - elapsed;

            if (remaining <= TimeSpan.Zero)
            {
                sessionTimer.Stop();
                SessionEnded();
                return;
            }

            timerLabel.Text      = remaining.ToString(@"hh\:mm\:ss");
            timerLabel.ForeColor = remaining.TotalMinutes < 5
                ? Color.FromArgb(239, 68, 68)   // red when < 5 min
                : Color.White;
        }

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            if (!sessionActive)
                timerLabel.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void SessionEnded()
        {
            sessionActive = false;
            MessageBox.Show("Class session has ended. Great work today! 🎉",
                "Session Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ForceExit();
        }

        // ── Exit handling ─────────────────────────────────────────
        private void ExitBtn_Click(object? sender, EventArgs e)
        {
            loginPanel.Visible = true;
            loginPanel.BringToFront();
            // Focus password box
            foreach (Control c in loginPanel.Controls)
                if (c.Name == "passBox") { c.Focus(); break; }
        }

        private void ShowExitBlockedMessage()
        {
            // Flash the top bar red briefly
            topBar.BackColor = Color.FromArgb(127, 29, 29);
            var t = new System.Windows.Forms.Timer { Interval = 600 };
            t.Tick += (s, e) => { topBar.BackColor = Color.FromArgb(22, 27, 34); t.Stop(); };
            t.Start();
        }

        private void ForceExit()
        {
            UnhookWindowsHookEx(_hookID);
            ShowTaskbar();
            sessionActive = false;
            Application.Exit();
        }

        // ── Keyboard hook — block Win, Alt+Tab, Alt+F4, Ctrl+Esc ──
        private void InstallKeyboardHook()
        {
            _keyboardProc = HookCallback;
            using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
            using var curModule  = curProcess.MainModule!;
            _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc,
                GetModuleHandle(curModule.ModuleName!), 0);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);

                bool isAlt  = (Control.ModifierKeys & Keys.Alt)     != 0;
                bool isCtrl = (Control.ModifierKeys & Keys.Control)  != 0;
                bool isWin  = vkCode == VK_LWIN || vkCode == VK_RWIN;

                // Block Win key
                if (isWin) return (IntPtr)1;

                // Block Alt+Tab, Alt+F4, Alt+Esc
                if (isAlt && (vkCode == VK_TAB || vkCode == VK_F4 || vkCode == VK_ESCAPE))
                    return (IntPtr)1;

                // Block Ctrl+Alt+Delete (partial — can't fully block at user level)
                if (isCtrl && isAlt && vkCode == VK_DELETE)
                    return (IntPtr)1;

                // Block Escape standalone
                if (vkCode == VK_ESCAPE && !isAlt && !isCtrl)
                    return (IntPtr)1;

                // Block F11 (fullscreen toggle)
                if (vkCode == VK_F11)
                    return (IntPtr)1;
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        // ── Taskbar hide/show ─────────────────────────────────────
        private void HideTaskbar()
        {
            IntPtr taskbar = FindWindow("Shell_TrayWnd", "");
            if (taskbar != IntPtr.Zero)
                ShowWindow(taskbar, 0); // SW_HIDE
        }

        private void ShowTaskbar()
        {
            IntPtr taskbar = FindWindow("Shell_TrayWnd", "");
            if (taskbar != IntPtr.Zero)
                ShowWindow(taskbar, 5); // SW_SHOW
        }

        // ── Keep window on top always ─────────────────────────────
        protected override void WndProc(ref Message m)
        {
            const int WM_ACTIVATE      = 0x0006;
            const int WM_ACTIVATEAPP   = 0x001C;
            const int WM_SETFOCUS      = 0x0007;

            base.WndProc(ref m);

            if (m.Msg == WM_ACTIVATE || m.Msg == WM_ACTIVATEAPP || m.Msg == WM_SETFOCUS)
            {
                this.TopMost = true;
                this.BringToFront();
                this.Focus();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            UnhookWindowsHookEx(_hookID);
            ShowTaskbar();
            base.OnFormClosed(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Block Alt+F4 at form level too
            if (keyData == (Keys.Alt | Keys.F4)) return true;
            if (keyData == Keys.Escape)          return true;
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
