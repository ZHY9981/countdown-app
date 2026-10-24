using System.Drawing;

namespace CountdownApp
{
    internal sealed class MainThemePalette
    {
        public Color Background;
        public Color ActionBar;
        public Color ActionButton;
        public Color ActionButtonHover;
        public Color Text;
        public Color Muted;
        public Color Border;
        public Color Accent;
        public Color AccentHover;
        public Color OnAccent;
        public Color Field;
        public Color Danger;
    }

    internal static class MainTheme
    {
        public static readonly MainThemePalette Dark = CreateDark();
        public static readonly MainThemePalette Light = CreateLight();

        private static MainThemePalette CreateDark()
        {
            MainThemePalette value = new MainThemePalette();
            value.Background = Color.FromArgb(15, 21, 26);
            value.ActionBar = Color.FromArgb(9, 15, 19);
            value.ActionButton = Color.FromArgb(21, 29, 35);
            value.ActionButtonHover = Color.FromArgb(30, 40, 46);
            value.Text = Color.FromArgb(245, 247, 244);
            value.Muted = Color.FromArgb(141, 153, 149);
            value.Border = Color.FromArgb(48, 61, 66);
            value.Accent = Color.FromArgb(228, 179, 106);
            value.AccentHover = Color.FromArgb(239, 194, 127);
            value.OnAccent = Color.FromArgb(23, 18, 12);
            value.Field = Color.FromArgb(21, 29, 35);
            value.Danger = Color.FromArgb(232, 128, 128);
            return value;
        }

        private static MainThemePalette CreateLight()
        {
            MainThemePalette value = new MainThemePalette();
            value.Background = Color.FromArgb(242, 244, 247);
            value.ActionBar = Color.FromArgb(233, 237, 241);
            value.ActionButton = Color.FromArgb(248, 249, 251);
            value.ActionButtonHover = Color.FromArgb(221, 228, 233);
            value.Text = Color.FromArgb(23, 32, 41);
            value.Muted = Color.FromArgb(116, 128, 138);
            value.Border = Color.FromArgb(208, 215, 222);
            value.Accent = Color.FromArgb(178, 138, 74);
            value.AccentHover = Color.FromArgb(194, 154, 87);
            value.OnAccent = Color.FromArgb(255, 255, 255);
            value.Field = Color.FromArgb(229, 234, 237);
            value.Danger = Color.FromArgb(185, 64, 64);
            return value;
        }
    }
}
