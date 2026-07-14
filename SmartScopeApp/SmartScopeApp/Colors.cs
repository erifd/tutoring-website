using System.Drawing;
namespace SmartScopeApp
{
    public static class SS
    {
        public static readonly Color BgDark   = Color.FromArgb(13,  17,  23);
        public static readonly Color Surface  = Color.FromArgb(22,  27,  34);
        public static readonly Color Surface2 = Color.FromArgb(28,  35,  51);
        public static readonly Color Border   = Color.FromArgb(48,  54,  61);
        public static readonly Color Accent   = Color.FromArgb(79,  110, 247);
        public static readonly Color AccentLt = Color.FromArgb(37,  47,  90);
        public static readonly Color Green    = Color.FromArgb(34,  197, 94);
        public static readonly Color Yellow   = Color.FromArgb(245, 158, 11);
        public static readonly Color Red      = Color.FromArgb(239, 68,  68);
        public static readonly Color Purple   = Color.FromArgb(124, 58,  237);
        public static readonly Color TextMain = Color.FromArgb(230, 237, 243);
        public static readonly Color TextMuted= Color.FromArgb(139, 148, 158);
        public static readonly Color White    = Color.White;

        public static Font TitleFont(float size) => new Font("Segoe UI", size, FontStyle.Bold);
        public static Font BodyFont(float size)  => new Font("Segoe UI", size, FontStyle.Regular);
        public static Font MonoFont(float size)  => new Font("Consolas",  size, FontStyle.Bold);

        public static void StyleButton(Button b, Color bg, Color fg)
        {
            b.BackColor = bg; b.ForeColor = fg;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Min(bg.R+20,255), Math.Min(bg.G+20,255), Math.Min(bg.B+20,255));
            b.Cursor = Cursors.Hand;
        }
    }
}
