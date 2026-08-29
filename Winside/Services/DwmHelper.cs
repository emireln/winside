using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Winside.Services
{
    public static class DwmHelper
    {
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        private const int DWMWA_MICA_EFFECT = 1029;

        // Backdrop types
        // 0 = Auto, 1 = None, 2 = MainWindow (Mica), 3 = Transient (Acrylic), 4 = Tabbed (Mica Alt)
        public enum BackdropType
        {
            Auto = 0,
            None = 1,
            Mica = 2,
            Acrylic = 3,
            MicaAlt = 4
        }

        // Corner preferences
        public enum CornerPreference
        {
            Default = 0,
            DoNotRound = 1,
            Round = 2,
            RoundSmall = 3
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int width, int height, uint flags);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_DLGMODALFRAME = 0x0001;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOZORDER = 0x0004;
        private const int SWP_FRAMECHANGED = 0x0020;
        private const uint WM_SETICON = 0x0080;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;

        public static void RemoveTitlebarIcon(Window window)
        {
            var helper = new WindowInteropHelper(window);
            IntPtr hwnd = helper.Handle;
            if (hwnd == IntPtr.Zero) return;

            SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_SMALL, IntPtr.Zero);
            SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_BIG, IntPtr.Zero);

            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_DLGMODALFRAME);
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
        }

        public static void ApplyWindows11Style(Window window, BackdropType backdrop = BackdropType.Mica, CornerPreference corner = CornerPreference.Round)
        {
            var helper = new WindowInteropHelper(window);
            IntPtr hwnd = helper.Handle;
            if (hwnd == IntPtr.Zero) return;

            // 1. Enable Immersive Dark Mode
            int darkMode = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

            // 2. Enable Window Rounded Corners
            int cornerPref = (int)corner;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(int));

            // 3. Enable System Backdrop (Mica / Acrylic)
            int backdropVal = (int)backdrop;
            int res = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropVal, sizeof(int));

            // Fallback for Windows 11 Build 22000 (21H2) which used DWMWA_MICA_EFFECT
            if (res != 0 && backdrop == BackdropType.Mica)
            {
                int micaTrue = 1;
                DwmSetWindowAttribute(hwnd, DWMWA_MICA_EFFECT, ref micaTrue, sizeof(int));
            }
        }
    }
}
