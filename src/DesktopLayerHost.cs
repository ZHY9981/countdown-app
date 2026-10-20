using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace CountdownApp
{
    // The Z-order placement approach is adapted from DesktopMusicWidget
    // (DuanLingLan/DesktopMusicWidget, MIT). The original Rust implementation
    // was not copied; this is a C# reimplementation.
    internal static class DesktopLayerHost
    {
        private const uint GW_HWNDPREV = 3;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int GWL_EXSTYLE = -20;
        private const uint WS_EX_TOPMOST = 0x00000008;
        private const string ProgmanClass = "Progman";
        private const string WorkerWClass = "WorkerW";

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(
            IntPtr hWnd,
            StringBuilder text,
            int count);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint command);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(
            string className,
            string windowName);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr insertAfter,
            int x,
            int y,
            int width,
            int height,
            uint flags);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int index);

        public static bool PlaceBehindAppsAndAboveWallpaper(IntPtr widgetHandle)
        {
            if (widgetHandle == IntPtr.Zero) return false;

            IntPtr anchor = FindWallpaperAnchor();
            if (anchor == IntPtr.Zero)
            {
                anchor = FindDesktopBandAnchor();
            }
            if (anchor == IntPtr.Zero) return false;

            IntPtr insertAfter = GetWindow(anchor, GW_HWNDPREV);
            if (insertAfter == widgetHandle)
            {
                insertAfter = GetWindow(insertAfter, GW_HWNDPREV);
            }
            if (insertAfter != IntPtr.Zero
                && (GetWindowLong(insertAfter, GWL_EXSTYLE) & WS_EX_TOPMOST) != 0)
            {
                insertAfter = IntPtr.Zero;
            }

            return SetWindowPos(
                widgetHandle,
                insertAfter,
                0,
                0,
                0,
                0,
                SWP_NOACTIVATE | SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }

        private static IntPtr FindWallpaperAnchor()
        {
            IntPtr result = IntPtr.Zero;
            EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
            {
                if (!IsWindowVisible(hWnd)) return true;

                uint processId;
                GetWindowThreadProcessId(hWnd, out processId);
                try
                {
                    Process process = Process.GetProcessById((int)processId);
                    string name = process.ProcessName;
                    if (name != null
                        && name.StartsWith("wallpaper", StringComparison.OrdinalIgnoreCase))
                    {
                        result = hWnd;
                        return false;
                    }
                }
                catch
                {
                    // Processes can exit while the window list is being read.
                }
                return true;
            }, IntPtr.Zero);
            return result;
        }

        private static IntPtr FindDesktopBandAnchor()
        {
            IntPtr progman = FindWindow(ProgmanClass, null);
            IntPtr result = IntPtr.Zero;
            EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
            {
                if (!IsWindowVisible(hWnd)) return true;

                StringBuilder className = new StringBuilder(128);
                GetClassName(hWnd, className, className.Capacity);
                string value = className.ToString();
                if (value == WorkerWClass)
                {
                    result = hWnd;
                    return false;
                }
                if (value == ProgmanClass)
                {
                    result = hWnd;
                }
                return true;
            }, IntPtr.Zero);
            return result == IntPtr.Zero ? progman : result;
        }
    }
}
