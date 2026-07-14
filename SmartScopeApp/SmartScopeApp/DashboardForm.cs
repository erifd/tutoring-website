using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace SmartScopeApp
{
    public class DashboardForm : Form
    {
        private readonly UserProfile user;
        private Panel  sidebar=null!, mainArea=null!, topBar=null!;
        private Label  clockLbl=null!, timerLbl=null!;
        private Button navOverview=null!, navClasses=null!, navCourses=null!, navHomework=null!;
        private Panel  overviewPanel=null!, classesPanel=null!, coursesPanel=null!;
        private WebView2 webView=null!;
        private LowLevelKeyboardHook keyHook=null!;
        private System.Windows.Forms.Timer clockTimer=null!;
        private DateTime sessionStart;
        private bool sessionActive=true;
        private List<ClassSession> classes=new();
        private List<CourseInfo>   courses=new();

        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr FindWindow(string c,string w);
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr h,int n);
        private void HideTaskbar(){var h=FindWindow("Shell_TrayWnd","");if(h!=IntPtr.Zero)ShowWindow(h,0);}
        private void ShowTaskbar(){var h=FindWindow("Shell_TrayWnd","");if(h!=IntPtr.Zero)ShowWindow(h,5);}

        public DashboardForm(UserProfile u)
        {
            user=u; sessionStart=DateTime.Now;
            Build(); InstallLockdown(); LoadData();
        }

        private void Build()
        {
            Text=$"SmartScope — {user.FullName}";
            FormBorderStyle=FormBorderStyle.None;
            WindowState=FormWindowState.Maximized;
            TopMost=true; BackColor=SS.BgDark; ShowInTaskbar=false;
            FormClosing+=(s,e)=>{ if(sessionActive) e.Cancel=true; };

            BuildTopBar(); BuildSidebar(); BuildMain();
            BuildOverview(); BuildClasses(); BuildCourses(); BuildWebView();
            ShowPanel(overviewPanel,navOverview);

            clockTimer=new System.Windows.Forms.Timer{Interval=1000};
            clockTimer.Tick+=(s,e)=>Tick();
            clockTimer.Start();
        }

        // ── TOP BAR ──────────────────────────────────────────────
        private void BuildTopBar()
        {
            int sw=Screen.PrimaryScreen!.Bounds.Width;
            topBar=new Panel{Dock=DockStyle.Top,Height=58,BackColor=SS.Surface};
            Controls.Add(topBar);

            topBar.Controls.Add(new Label{Text="SmartScope",Font=SS.TitleFont(14f),ForeColor=SS.Accent,AutoSize=true,Location=new Point(20,15)});
            topBar.Controls.Add(new Label{Text="● LIVE",Font=SS.TitleFont(8f),ForeColor=SS.Green,AutoSize=true,Location=new Point(152,20)});

            // User chip
            topBar.Controls.Add(new Label{Text=$"{user.Initials}  {user.firstName}",Font=SS.TitleFont(9f),ForeColor=SS.Accent,BackColor=SS.AccentLt,AutoSize=true,Location=new Point(sw-600,16),Padding=new Padding(8,4,8,4)});

            clockLbl=new Label{Text="00:00",Font=SS.MonoFont(13f),ForeColor=SS.TextMain,AutoSize=true,Location=new Point(sw-220,14)};
            timerLbl=new Label{Text="00:00:00",Font=SS.MonoFont(9f),ForeColor=SS.TextMuted,AutoSize=true,Location=new Point(sw-220,36)};
            topBar.Controls.Add(clockLbl);
            topBar.Controls.Add(timerLbl);

            var exitBtn=new Button{Text="Exit",Font=SS.BodyFont(9f),Size=new Size(70,32),Location=new Point(sw-88,13)};
            SS.StyleButton(exitBtn,SS.Surface2,SS.TextMuted);
            exitBtn.Click+=ExitClicked;
            topBar.Controls.Add(exitBtn);
            topBar.BringToFront();
        }

        // ── SIDEBAR ───────────────────────────────────────────────
        private void BuildSidebar()
        {
            sidebar=new Panel{Width=220,BackColor=SS.Surface,Dock=DockStyle.Left};
            Controls.Add(sidebar);
            int y=74;
            sidebar.Controls.Add(SideLabel("MENU",y)); y+=28;
            navOverview =SideBtn("🏠  Overview", y,()=>ShowPanel(overviewPanel,navOverview));  y+=46;
            navClasses  =SideBtn("📅  My Classes",y,()=>ShowPanel(classesPanel, navClasses));  y+=46;
            navCourses  =SideBtn("🎬  Courses",  y,()=>ShowPanel(coursesPanel, navCourses));  y+=46;
            navHomework =SideBtn("📝  Homework", y,()=>OpenUrl("https://smartscope-tutoring.onrender.com/homework.html")); y+=46;
            sidebar.Controls.Add(navOverview); sidebar.Controls.Add(navClasses);
            sidebar.Controls.Add(navCourses);  sidebar.Controls.Add(navHomework);
            y+=16; sidebar.Controls.Add(SideLabel("ACCOUNT",y)); y+=28;
            var profileBtn=SideBtn($"👤  {user.firstName}",y,()=>OpenUrl("https://smartscope-tutoring.onrender.com/student.html"));
            sidebar.Controls.Add(profileBtn); y+=56;

            // Linking key box
            if(!string.IsNullOrEmpty(user.linkingKey))
            {
                var box=new Panel{Size=new Size(188,68),Location=new Point(14,y),BackColor=SS.Surface2};
                box.Controls.Add(new Label{Text="Your linking code",Font=SS.BodyFont(8f),ForeColor=SS.TextMuted,AutoSize=true,Location=new Point(10,8)});
                box.Controls.Add(new Label{Text=user.linkingKey,Font=SS.MonoFont(18f),ForeColor=SS.TextMain,AutoSize=true,Location=new Point(10,26)});
                sidebar.Controls.Add(box);
            }
        }

        // ── MAIN AREA ─────────────────────────────────────────────
        private void BuildMain()
        {
            mainArea=new Panel{Dock=DockStyle.Fill,BackColor=SS.BgDark,Padding=new Padding(28,24,28,24)};
            Controls.Add(mainArea);
        }

        // ── OVERVIEW ─────────────────────────────────────────────
        private void BuildOverview()
        {
            overviewPanel=new Panel{Dock=DockStyle.Fill,BackColor=SS.BgDark};
            overviewPanel.Controls.Add(Title("Overview 🏠","Here's what's on today."));
            overviewPanel.Controls.Add(new FlowLayoutPanel{Name="stats",Location=new Point(0,80),Size=new Size(1000,110),FlowDirection=FlowDirection.LeftToRight,WrapContents=false,BackColor=Color.Transparent});
            overviewPanel.Controls.Add(new Panel{Name="liveBanner",Location=new Point(0,210),Size=new Size(900,90),BackColor=Color.Transparent});
            overviewPanel.Controls.Add(SLabel("UPCOMING",310));
            overviewPanel.Controls.Add(new FlowLayoutPanel{Name="upcoming",Location=new Point(0,338),Size=new Size(900,500),FlowDirection=FlowDirection.TopDown,WrapContents=false,BackColor=Color.Transparent,AutoScroll=true});
        }

        // ── CLASSES ───────────────────────────────────────────────
        private void BuildClasses()
        {
            classesPanel=new Panel{Dock=DockStyle.Fill,BackColor=SS.BgDark};
            classesPanel.Controls.Add(Title("My Classes 📅","All your scheduled sessions."));
            classesPanel.Controls.Add(new FlowLayoutPanel{Name="list",Location=new Point(0,80),Size=new Size(900,700),FlowDirection=FlowDirection.TopDown,WrapContents=false,BackColor=Color.Transparent,AutoScroll=true});
        }

        // ── COURSES ───────────────────────────────────────────────
        private void BuildCourses()
        {
            coursesPanel=new Panel{Dock=DockStyle.Fill,BackColor=SS.BgDark};
            coursesPanel.Controls.Add(Title("Video Courses 🎬","Courses unlocked for you."));
            coursesPanel.Controls.Add(new FlowLayoutPanel{Name="grid",Location=new Point(0,80),Size=new Size(1000,700),FlowDirection=FlowDirection.LeftToRight,WrapContents=true,BackColor=Color.Transparent,AutoScroll=true});
        }

        // ── WEBVIEW ───────────────────────────────────────────────
        private void BuildWebView()
        {
            webView=new WebView2{Dock=DockStyle.Fill,Visible=false};
            webView.CoreWebView2InitializationCompleted+=(s,e)=>{
                if(!e.IsSuccess)return;
                webView.CoreWebView2.Settings.AreDevToolsEnabled=false;
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled=false;
                webView.CoreWebView2.NewWindowRequested+=(s2,a)=>{a.Handled=true;webView.CoreWebView2.Navigate(a.Uri);};
            };
            webView.EnsureCoreWebView2Async(null);
            mainArea.Controls.Add(webView);
        }

        // ── LOAD DATA ────────────────────────────────────────────
        private async void LoadData()
        {
            try
            {
                var ct = ApiClient.GetClassesAsync(user.uid);
                var et = ApiClient.GetEnrolledIndexesAsync(user.uid);
                await System.Threading.Tasks.Task.WhenAll(ct,et);
                classes=ct.Result;
                // Build placeholder courses — names filled in by localStorage via web
                courses.Clear();
                foreach(var idx in et.Result)
                    courses.Add(new CourseInfo{Index=idx,Name=$"Course {idx+1}",Subject=""});
                if(InvokeRequired) Invoke(RefreshUI); else RefreshUI();
            }
            catch{ /* show empty state */ }
        }

        private void RefreshUI(){ RenderOverview(); RenderClasses(); RenderCourses(); }

        // ── RENDER OVERVIEW ──────────────────────────────────────
        private void RenderOverview()
        {
            var stats   =overviewPanel.Controls["stats"]      as FlowLayoutPanel;
            var banner  =overviewPanel.Controls["liveBanner"] as Panel;
            var upcoming=overviewPanel.Controls["upcoming"]   as FlowLayoutPanel;
            if(stats==null||banner==null||upcoming==null) return;

            stats.Controls.Clear();
            stats.Controls.Add(Stat("Classes",   classes.Count.ToString(),            "total"));
            stats.Controls.Add(Stat("Courses",   courses.Count.ToString(),            "unlocked"));
            stats.Controls.Add(Stat("Today",     classes.FindAll(c=>c.IsToday).Count.ToString(),"sessions today"));

            banner.Controls.Clear();
            var live=classes.Find(c=>c.IsLive);
            if(live!=null) banner.Controls.Add(LiveBanner(live));

            upcoming.Controls.Clear();
            var today=DateTime.Today;
            var upc=classes.FindAll(c=>c.ParsedDate.HasValue&&c.ParsedDate.Value.Date>=today);
            upc.Sort((a,b)=>(a.ParsedDate??DateTime.MaxValue).CompareTo(b.ParsedDate??DateTime.MaxValue));
            foreach(var c in upc) upcoming.Controls.Add(ClassCard(c,compact:true));
            if(upc.Count==0) upcoming.Controls.Add(Empty("No upcoming classes yet.\nYour tutor will add sessions to your calendar."));
        }

        // ── RENDER CLASSES ────────────────────────────────────────
        private void RenderClasses()
        {
            var list=classesPanel.Controls["list"] as FlowLayoutPanel;
            if(list==null)return;
            list.Controls.Clear();
            if(classes.Count==0){list.Controls.Add(Empty("No classes scheduled yet."));return;}
            var sorted=new List<ClassSession>(classes);
            sorted.Sort((a,b)=>(a.ParsedDate??DateTime.MaxValue).CompareTo(b.ParsedDate??DateTime.MaxValue));
            foreach(var c in sorted) list.Controls.Add(ClassCard(c,compact:false));
        }

        // ── RENDER COURSES ────────────────────────────────────────
        private void RenderCourses()
        {
            var grid=coursesPanel.Controls["grid"] as FlowLayoutPanel;
            if(grid==null)return;
            grid.Controls.Clear();
            if(courses.Count==0){grid.Controls.Add(Empty("No courses unlocked yet.\nAsk your parent to buy a course on the SmartScope website."));return;}
            foreach(var c in courses) grid.Controls.Add(CourseCard(c));
        }

        // ── LIVE BANNER ───────────────────────────────────────────
        private Panel LiveBanner(ClassSession c)
        {
            int w=Math.Min(mainArea.Width-60, 900);
            var p=new Panel{Size=new Size(w,82),BackColor=Color.FromArgb(21,128,61,22)};
            p.Controls.Add(new Label{Text="🔴  LIVE NOW",Font=SS.TitleFont(7.5f),ForeColor=SS.Green,AutoSize=true,Location=new Point(16,8)});
            p.Controls.Add(new Label{Text=c.Subject,Font=SS.TitleFont(13f),ForeColor=SS.TextMain,AutoSize=true,Location=new Point(16,26)});
            p.Controls.Add(new Label{Text=string.IsNullOrEmpty(c.Tutor)?c.Time:$"with {c.Tutor} · {c.Time}",Font=SS.BodyFont(9f),ForeColor=SS.TextMuted,AutoSize=true,Location=new Point(16,54)});
            if(!string.IsNullOrEmpty(c.Link))
            {
                var btn=new Button{Text="Join Now →",Font=SS.TitleFont(10f),Size=new Size(130,38),Location=new Point(w-154,22)};
                SS.StyleButton(btn,SS.Green,SS.White);
                string link=c.Link;
                btn.Click+=(s,e)=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(link){UseShellExecute=true});
                p.Controls.Add(btn);
            }
            return p;
        }

        // ── CLASS CARD ────────────────────────────────────────────
        private Panel ClassCard(ClassSession c, bool compact)
        {
            int w=Math.Min(mainArea.Width-60,900), h=compact?66:88;
            var p=new Panel{Size=new Size(w,h),BackColor=SS.Surface,Margin=new Padding(0,0,0,8)};
            Color dot=c.IsLive?SS.Green:c.IsSoon?SS.Yellow:SS.Border;
            var d=new Panel{Size=new Size(10,10),Location=new Point(16,h/2-5),BackColor=dot};
            d.Region=new System.Drawing.Region(new System.Drawing.RectangleF(0,0,10,10));
            p.Controls.Add(d);
            p.Controls.Add(new Label{Text=c.Subject.ToUpper(),Font=SS.TitleFont(7.5f),ForeColor=SS.Accent,AutoSize=true,Location=new Point(36,10)});
            string ds=c.ParsedDate?.ToString("ddd, MMM d")??c.Date;
            p.Controls.Add(new Label{Text=$"{ds}{(string.IsNullOrEmpty(c.Time)?"":" at "+c.Time)}",Font=SS.TitleFont(10f),ForeColor=SS.TextMain,AutoSize=true,Location=new Point(36,26)});
            if(!compact&&!string.IsNullOrEmpty(c.Tutor))
                p.Controls.Add(new Label{Text=$"with {c.Tutor}",Font=SS.BodyFont(9f),ForeColor=SS.TextMuted,AutoSize=true,Location=new Point(36,52)});
            if(c.IsLive&&!string.IsNullOrEmpty(c.Link))
            {
                var btn=new Button{Text="Join →",Font=SS.TitleFont(9f),Size=new Size(88,30),Location=new Point(w-110,h/2-15)};
                SS.StyleButton(btn,SS.Green,SS.White);
                string link=c.Link;
                btn.Click+=(s,e)=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(link){UseShellExecute=true});
                p.Controls.Add(btn);
            }
            else
            {
                Color sc=c.IsLive?SS.Green:c.IsSoon?SS.Yellow:SS.TextMuted;
                string st=c.IsLive?"Live":c.IsSoon?"Soon":"Scheduled";
                p.Controls.Add(new Label{Text=st,Font=SS.TitleFont(8.5f),ForeColor=sc,AutoSize=true,Location=new Point(w-100,h/2-10)});
            }
            return p;
        }

        // ── COURSE CARD ───────────────────────────────────────────
        private Panel CourseCard(CourseInfo c)
        {
            var p=new Panel{Size=new Size(260,180),BackColor=SS.Surface,Margin=new Padding(0,0,16,16)};
            Color[] thumbs={Color.FromArgb(30,58,95),Color.FromArgb(20,83,45),Color.FromArgb(30,27,75),Color.FromArgb(124,45,18),Color.FromArgb(31,41,55)};
            string[] icons={"📐","⚗️","💡","🇫🇷","📚"};
            int ti=Math.Abs((c.Name+c.Index).GetHashCode())%thumbs.Length;
            var thumb=new Panel{Size=new Size(260,80),Location=new Point(0,0),BackColor=thumbs[ti]};
            thumb.Controls.Add(new Label{Text=icons[ti],Font=new Font("Segoe UI Emoji",22f),AutoSize=true,Location=new Point(100,16)});
            p.Controls.Add(thumb);
            p.Controls.Add(new Label{Text=string.IsNullOrEmpty(c.Subject)?"COURSE":c.Subject.ToUpper(),Font=SS.TitleFont(7f),ForeColor=SS.Accent,AutoSize=true,Location=new Point(12,88)});
            p.Controls.Add(new Label{Text=c.Name,Font=SS.TitleFont(9.5f),ForeColor=SS.TextMain,AutoSize=false,Size=new Size(236,36),Location=new Point(12,102)});
            p.Controls.Add(new Label{Text=c.Lessons>0?$"{c.Lessons} lessons":"",Font=SS.BodyFont(8.5f),ForeColor=SS.TextMuted,AutoSize=true,Location=new Point(12,140)});
            var watch=new Button{Text="Watch →",Font=SS.TitleFont(8.5f),Size=new Size(80,28),Location=new Point(168,140)};
            SS.StyleButton(watch,SS.Accent,SS.White);
            int idx=c.Index;
            watch.Click+=(s,e)=>OpenUrl($"https://smartscope-tutoring.onrender.com/video.html?idx={idx}");
            p.Controls.Add(watch);
            return p;
        }

        // ── PANEL SWITCHING ───────────────────────────────────────
        private void ShowPanel(Panel target, Button active)
        {
            webView.Visible=false;
            overviewPanel.Visible=false; classesPanel.Visible=false; coursesPanel.Visible=false;
            if(target.Parent==null){target.Dock=DockStyle.Fill;mainArea.Controls.Add(target);}
            target.Visible=true; target.BringToFront();
            foreach(var b in new[]{navOverview,navClasses,navCourses,navHomework})
            {b.BackColor=b==active?SS.AccentLt:SS.Surface; b.ForeColor=b==active?SS.Accent:SS.TextMuted;}
        }

        private void OpenUrl(string url)
        {
            webView.Visible=true; webView.BringToFront();
            overviewPanel.Visible=false; classesPanel.Visible=false; coursesPanel.Visible=false;
            foreach(var b in new[]{navOverview,navClasses,navCourses,navHomework}){b.BackColor=SS.Surface;b.ForeColor=SS.TextMuted;}
            if(webView.CoreWebView2!=null) webView.CoreWebView2.Navigate(url);
        }

        // ── EXIT ──────────────────────────────────────────────────
        private void ExitClicked(object? s, EventArgs e)
        {
            if(MessageBox.Show("End your class session?","Exit SmartScope",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes)
                ForceExit();
        }

        private void ForceExit()
        {
            sessionActive=false; clockTimer?.Stop(); keyHook?.Uninstall(); ShowTaskbar();
            KioskLauncher.LogSession(user.FullName,sessionStart,DateTime.Now,"Session");
            Application.Exit();
        }

        // ── CLOCK ─────────────────────────────────────────────────
        private void Tick()
        {
            if(clockLbl.IsDisposed)return;
            clockLbl.Text=DateTime.Now.ToString("HH:mm");
            timerLbl.Text=(DateTime.Now-sessionStart).ToString(@"hh\:mm\:ss");
        }

        // ── LOCKDOWN ──────────────────────────────────────────────
        private void InstallLockdown()
        {
            keyHook=new LowLevelKeyboardHook(); keyHook.Install(); HideTaskbar();
            var t=new System.Windows.Forms.Timer{Interval=500};
            t.Tick+=(s,e)=>{if(!IsDisposed){TopMost=true;BringToFront();}};
            t.Start();
        }

        // ── HELPERS ───────────────────────────────────────────────
        private Panel Title(string t,string sub){var p=new Panel{Location=new Point(0,0),Size=new Size(1200,70),BackColor=Color.Transparent};p.Controls.Add(new Label{Text=t,Font=SS.TitleFont(18f),ForeColor=SS.TextMain,AutoSize=true,Location=new Point(0,0)});p.Controls.Add(new Label{Text=sub,Font=SS.BodyFont(10f),ForeColor=SS.TextMuted,AutoSize=true,Location=new Point(2,32)});return p;}
        private Label SLabel(string t,int y)=>new Label{Text=t,Font=SS.TitleFont(8f),ForeColor=SS.TextMuted,AutoSize=true,Location=new Point(0,y)};
        private Panel Stat(string lbl,string val,string sub){var p=new Panel{Size=new Size(155,88),BackColor=SS.Surface,Margin=new Padding(0,0,16,0)};p.Controls.Add(new Label{Text=lbl,Font=SS.BodyFont(8.5f),ForeColor=SS.TextMuted,AutoSize=true,Location=new Point(14,12)});p.Controls.Add(new Label{Text=val,Font=SS.TitleFont(26f),ForeColor=SS.TextMain,AutoSize=true,Location=new Point(12,28)});p.Controls.Add(new Label{Text=sub,Font=SS.BodyFont(8f),ForeColor=SS.TextMuted,AutoSize=true,Location=new Point(14,66)});return p;}
        private Panel Empty(string msg){var p=new Panel{Size=new Size(700,100),BackColor=Color.Transparent};p.Controls.Add(new Label{Text=msg,Font=SS.BodyFont(10f),ForeColor=SS.TextMuted,AutoSize=true,Location=new Point(0,16)});return p;}
        private Button SideBtn(string t,int y,Action a){var b=new Button{Text=t,Font=SS.BodyFont(10f),Size=new Size(216,40),Location=new Point(2,y),TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(14,0,0,0)};SS.StyleButton(b,SS.Surface,SS.TextMuted);b.Click+=(s,e)=>a();return b;}
        private Label SideLabel(string t,int y)=>new Label{Text=t,Font=SS.TitleFont(7.5f),ForeColor=SS.Border,AutoSize=true,Location=new Point(16,y)};

        protected override void WndProc(ref Message m){base.WndProc(ref m);if((m.Msg==0x0006||m.Msg==0x001C)&&sessionActive){TopMost=true;BringToFront();}}
        protected override void OnFormClosed(FormClosedEventArgs e){keyHook?.Uninstall();ShowTaskbar();base.OnFormClosed(e);}
    }
}
