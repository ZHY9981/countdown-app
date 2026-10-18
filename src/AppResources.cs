using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CountdownApp
{
    public static class AppResources
    {
        public static readonly string ThemeName;
        public static readonly float ScaleFactor;

        public static readonly Color Background;
        public static readonly Color Surface;
        public static readonly Color Header;
        public static readonly Color HeaderMuted;
        public static readonly Color HeaderText;
        public static readonly Color Text;
        public static readonly Color Muted;
        public static readonly Color Border;
        public static readonly Color Accent;
        public static readonly Color AccentHover;
        public static readonly Color Secondary;
        public static readonly Color GridHeaderBackground;
        public static readonly Color GridRowSelection;
        public static readonly Color GridAltRow;
        public static readonly Color ButtonBorder;
        public static readonly Color FieldBackground;
        public static readonly Color OnAccent;
        public static readonly Color Danger;
        public static readonly Color DangerPale;
        public static readonly Color Today;
        public static readonly Color TodayPale;
        public static readonly Color IconCircle;
        public static readonly Color IconGlyph;

        static AppResources()
        {
            ScaleFactor = DetectScaleFactor();
            ThemeName = ResolveRequestedTheme();

            Palette palette;
            if (ThemeName == "ink") palette = Ink();
            else if (ThemeName == "graphite") palette = Graphite();
            else palette = Default();

            Background = palette.Background;
            Surface = palette.Surface;
            Header = palette.Header;
            HeaderMuted = palette.HeaderMuted;
            HeaderText = palette.HeaderText;
            Text = palette.Text;
            Muted = palette.Muted;
            Border = palette.Border;
            Accent = palette.Accent;
            AccentHover = palette.AccentHover;
            Secondary = palette.Secondary;
            GridHeaderBackground = palette.GridHeaderBackground;
            GridRowSelection = palette.GridRowSelection;
            GridAltRow = palette.GridAltRow;
            ButtonBorder = palette.ButtonBorder;
            FieldBackground = palette.FieldBackground;
            OnAccent = palette.OnAccent;
            Danger = palette.Danger;
            DangerPale = palette.DangerPale;
            Today = palette.Today;
            TodayPale = palette.TodayPale;
            IconCircle = palette.IconCircle;
            IconGlyph = palette.IconGlyph;
        }

        private static float DetectScaleFactor()
        {
            float dpi = 96f;
            try
            {
                using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
                {
                    if (graphics.DpiX > 0f) dpi = graphics.DpiX;
                }
            }
            catch
            {
                // Keep 100% when the display context is unavailable.
            }
            return dpi / 96f;
        }

        public static int S(int value)
        {
            return (int)Math.Round(value * ScaleFactor);
        }

        public static float SF(float value)
        {
            return value * ScaleFactor;
        }

        public static Point SP(int x, int y)
        {
            return new Point(S(x), S(y));
        }

        public static Size SS(int width, int height)
        {
            return new Size(S(width), S(height));
        }

        public static Padding SPad(int left, int top, int right, int bottom)
        {
            return new Padding(S(left), S(top), S(right), S(bottom));
        }

        private sealed class Palette
        {
            public Color Background;
            public Color Surface;
            public Color Header;
            public Color HeaderMuted;
            public Color HeaderText;
            public Color Text;
            public Color Muted;
            public Color Border;
            public Color Accent;
            public Color AccentHover;
            public Color Secondary;
            public Color GridHeaderBackground;
            public Color GridRowSelection;
            public Color GridAltRow;
            public Color ButtonBorder;
            public Color FieldBackground;
            public Color OnAccent;
            public Color Danger;
            public Color DangerPale;
            public Color Today;
            public Color TodayPale;
            public Color IconCircle;
            public Color IconGlyph;
        }

        private static Palette Default()
        {
            Palette p = new Palette();
            p.Background = Color.FromArgb(245, 247, 250);
            p.Surface = Color.White;
            p.Header = Color.FromArgb(17, 24, 39);
            p.HeaderMuted = Color.FromArgb(177, 186, 201);
            p.HeaderText = Color.White;
            p.Text = Color.FromArgb(24, 32, 44);
            p.Muted = Color.FromArgb(102, 112, 133);
            p.Border = Color.FromArgb(227, 232, 239);
            p.Accent = Color.FromArgb(59, 111, 245);
            p.AccentHover = Color.FromArgb(47, 94, 216);
            p.Secondary = Color.FromArgb(72, 84, 103);
            p.GridHeaderBackground = Color.FromArgb(248, 250, 252);
            p.GridRowSelection = Color.FromArgb(234, 240, 255);
            p.GridAltRow = Color.FromArgb(252, 253, 254);
            p.ButtonBorder = Color.FromArgb(203, 213, 225);
            p.FieldBackground = Color.FromArgb(248, 250, 252);
            p.OnAccent = Color.White;
            p.Danger = Color.FromArgb(211, 58, 58);
            p.DangerPale = Color.FromArgb(255, 244, 243);
            p.Today = Color.FromArgb(7, 133, 82);
            p.TodayPale = Color.FromArgb(237, 250, 245);
            p.IconCircle = Color.FromArgb(17, 24, 39);
            p.IconGlyph = Color.White;
            return p;
        }

        private static Palette Ink()
        {
            Palette p = new Palette();
            p.Background = Color.FromArgb(18, 20, 24);
            p.Surface = Color.FromArgb(26, 29, 34);
            p.Header = Color.FromArgb(12, 14, 17);
            p.HeaderMuted = Color.FromArgb(140, 148, 160);
            p.HeaderText = Color.FromArgb(236, 239, 243);
            p.Text = Color.FromArgb(226, 230, 236);
            p.Muted = Color.FromArgb(148, 156, 168);
            p.Border = Color.FromArgb(44, 49, 58);
            p.Accent = Color.FromArgb(111, 143, 255);
            p.AccentHover = Color.FromArgb(135, 160, 255);
            p.Secondary = Color.FromArgb(150, 158, 170);
            p.GridHeaderBackground = Color.FromArgb(32, 36, 42);
            p.GridRowSelection = Color.FromArgb(32, 42, 69);
            p.GridAltRow = Color.FromArgb(31, 34, 40);
            p.ButtonBorder = Color.FromArgb(62, 68, 78);
            p.FieldBackground = Color.FromArgb(34, 38, 44);
            p.OnAccent = Color.White;
            p.Danger = Color.FromArgb(255, 92, 92);
            p.DangerPale = Color.FromArgb(58, 30, 32);
            p.Today = Color.FromArgb(46, 204, 113);
            p.TodayPale = Color.FromArgb(25, 53, 39);
            p.IconCircle = Color.FromArgb(236, 239, 243);
            p.IconGlyph = Color.FromArgb(18, 20, 24);
            return p;
        }

        private static Palette Graphite()
        {
            Palette p = new Palette();
            p.Background = Color.FromArgb(236, 238, 241);
            p.Surface = Color.White;
            p.Header = Color.FromArgb(22, 24, 28);
            p.HeaderMuted = Color.FromArgb(160, 166, 175);
            p.HeaderText = Color.White;
            p.Text = Color.FromArgb(17, 19, 23);
            p.Muted = Color.FromArgb(95, 102, 112);
            p.Border = Color.FromArgb(202, 208, 216);
            p.Accent = Color.FromArgb(37, 99, 235);
            p.AccentHover = Color.FromArgb(29, 78, 216);
            p.Secondary = Color.FromArgb(60, 68, 80);
            p.GridHeaderBackground = Color.FromArgb(238, 240, 243);
            p.GridRowSelection = Color.FromArgb(235, 241, 253);
            p.GridAltRow = Color.FromArgb(249, 250, 252);
            p.ButtonBorder = Color.FromArgb(176, 184, 194);
            p.FieldBackground = Color.FromArgb(244, 246, 249);
            p.OnAccent = Color.White;
            p.Danger = Color.FromArgb(200, 32, 45);
            p.DangerPale = Color.FromArgb(255, 242, 242);
            p.Today = Color.FromArgb(6, 130, 74);
            p.TodayPale = Color.FromArgb(236, 249, 242);
            p.IconCircle = Color.FromArgb(22, 24, 28);
            p.IconGlyph = Color.White;
            return p;
        }

        private static string ResolveRequestedTheme()
        {
            string value = null;
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] != null && args[i].StartsWith("--theme=", StringComparison.OrdinalIgnoreCase))
                {
                    value = args[i].Substring("--theme=".Length);
                    break;
                }
            }

            if (string.IsNullOrEmpty(value))
            {
                value = Environment.GetEnvironmentVariable("COUNTDOWN_THEME");
            }
            if (string.IsNullOrEmpty(value)) return "default";

            value = value.Trim().ToLowerInvariant();
            if (value == "ink" || value == "graphite" || value == "default") return value;
            return "default";
        }

        public static Font Font(float size, FontStyle style)
        {
            return new Font("Microsoft YaHei UI", size, style, GraphicsUnit.Point);
        }

        public static Font DigitFont(float size, FontStyle style)
        {
            return new Font("Bahnschrift", size, style, GraphicsUnit.Point);
        }

        public static string FormatDate(DateTime value)
        {
            return value == DateTime.MinValue
                ? "--"
                : value.ToString("yyyy年M月d日", CultureInfo.CurrentCulture);
        }

        public static string FormatShortDate(DateTime value)
        {
            return value == DateTime.MinValue
                ? "--"
                : value.ToString("MM月d日", CultureInfo.CurrentCulture);
        }

        public static string FormatTime(DateTime value)
        {
            return value.ToString("HH:mm:ss", CultureInfo.CurrentCulture);
        }

        public static Color StatusColor(CountdownStatus status, CountdownSnapshot snapshot)
        {
            if (status == CountdownStatus.Ended || status == CountdownStatus.Invalid) return Muted;
            if (status == CountdownStatus.Today) return Today;
            if (snapshot != null && snapshot.CalendarDays <= 7) return Danger;
            return Accent;
        }

        public static Color StatusBackground(CountdownStatus status, CountdownSnapshot snapshot)
        {
            if (status == CountdownStatus.Today) return TodayPale;
            if (status == CountdownStatus.Upcoming && snapshot != null && snapshot.CalendarDays <= 7) return DangerPale;
            return FieldBackground;
        }

        public static Icon CreateAppIcon()
        {
            return CreateIcon(64);
        }

        public static Icon CreateTrayIcon()
        {
            return CreateIcon(32);
        }

        private static Icon CreateIcon(int size)
        {
            using (Bitmap bitmap = CreateIconBitmap(size))
            {
                IntPtr handle = bitmap.GetHicon();
                try
                {
                    using (Icon temporary = Icon.FromHandle(handle))
                    {
                        return (Icon)temporary.Clone();
                    }
                }
                finally
                {
                    DestroyIcon(handle);
                }
            }
        }

        private static Bitmap CreateIconBitmap(int size)
        {
            Bitmap bitmap = new Bitmap(size, size);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);

                using (SolidBrush background = new SolidBrush(IconCircle))
                {
                    graphics.FillEllipse(background, 0, 0, size - 1, size - 1);
                }

                using (Pen clockPen = new Pen(IconGlyph, Math.Max(2f, size / 13f)))
                {
                    clockPen.StartCap = LineCap.Round;
                    clockPen.EndCap = LineCap.Round;
                    graphics.DrawEllipse(clockPen, size * 0.20f, size * 0.20f, size * 0.60f, size * 0.60f);
                    graphics.DrawLine(clockPen, size * 0.50f, size * 0.50f, size * 0.50f, size * 0.31f);
                    graphics.DrawLine(clockPen, size * 0.50f, size * 0.50f, size * 0.67f, size * 0.59f);
                }

                using (SolidBrush accent = new SolidBrush(Accent))
                {
                    float dot = Math.Max(4f, size * 0.14f);
                    graphics.FillEllipse(accent, size * 0.73f, size * 0.16f, dot, dot);
                }
            }
            return bitmap;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);
    }
}
