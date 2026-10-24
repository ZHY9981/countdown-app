using System;
using System.Runtime.InteropServices;

namespace CountdownApp
{
    internal static class MainWindowTheme
    {
        private const int UseImmersiveDarkMode = 20;
        private const int UseImmersiveDarkModeLegacy = 19;
        private const int CaptionColor = 35;
        private const int TextColor = 36;
        private const int BorderColor = 34;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            int attribute,
            ref int value,
            int size);

        public static void Apply(IntPtr handle, bool dark)
        {
            if (handle == IntPtr.Zero) return;

            try
            {
                int enabled = dark ? 1 : 0;
                if (DwmSetWindowAttribute(
                    handle,
                    UseImmersiveDarkMode,
                    ref enabled,
                    sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(
                        handle,
                        UseImmersiveDarkModeLegacy,
                        ref enabled,
                        sizeof(int));
                }

                int caption = dark
                    ? ToColorRef(10, 16, 20)
                    : ToColorRef(233, 237, 241);
                int border = dark
                    ? ToColorRef(45, 53, 58)
                    : ToColorRef(208, 215, 222);
                int text = dark
                    ? ToColorRef(244, 247, 244)
                    : ToColorRef(52, 64, 74);
                DwmSetWindowAttribute(handle, CaptionColor, ref caption, sizeof(int));
                DwmSetWindowAttribute(handle, BorderColor, ref border, sizeof(int));
                DwmSetWindowAttribute(handle, TextColor, ref text, sizeof(int));
            }
            catch
            {
                // Older Windows versions do not support all caption attributes.
            }
        }

        private static int ToColorRef(int red, int green, int blue)
        {
            return red | (green << 8) | (blue << 16);
        }
    }
}
