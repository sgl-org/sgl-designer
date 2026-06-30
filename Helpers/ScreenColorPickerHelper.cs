using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SglDesigner
{
    /// <summary>
    /// 屏幕取色辅助类 - 使用 Win32 API 从屏幕上任意位置获取像素颜色
    /// </summary>
    public static class ScreenColorPickerHelper
    {
        #region Win32 API

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        #endregion

        /// <summary>
        /// 启动屏幕取色。全屏覆盖窗口，用户点击任意位置后返回该像素颜色。
        /// 按 ESC 取消取色，返回 null。
        /// 使用 GetCursorPos 获取物理像素坐标，正确处理 DPI 缩放。
        /// </summary>
        public static Task<Color?> PickColorAsync(Window owner)
        {
            var tcs = new TaskCompletionSource<Color?>();

            // 保存当前光标状态
            var savedCursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = Cursors.Cross;

            // 创建全屏覆盖窗口（覆盖所有显示器）
            var pickerWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                // alpha=1 近乎透明，但 WPF 需要至少 alpha>0 才能接收鼠标事件
                Background = new SolidColorBrush(Color.FromArgb(1, 255, 255, 255)),
                Topmost = true,
                ShowInTaskbar = false,
                ShowActivated = false,
                WindowState = WindowState.Normal,
                Left = SystemParameters.VirtualScreenLeft,
                Top = SystemParameters.VirtualScreenTop,
                Width = SystemParameters.VirtualScreenWidth,
                Height = SystemParameters.VirtualScreenHeight,
                Cursor = Cursors.Cross,
                Focusable = true,
            };

            bool completed = false;

            // 用户点击 → 取色
            pickerWindow.MouseLeftButtonDown += (s, e) =>
            {
                if (completed) return;
                completed = true;

                try
                {
                    // 使用 GetCursorPos 获取物理像素坐标（正确处理 DPI）
                    GetCursorPos(out POINT pt);
                    Color color = GetPixelColor(pt.X, pt.Y);
                    tcs.TrySetResult(color);
                }
                catch
                {
                    tcs.TrySetResult(null);
                }
                finally
                {
                    pickerWindow.Close();
                }
            };

            // ESC 取消
            pickerWindow.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape && !completed)
                {
                    completed = true;
                    tcs.TrySetResult(null);
                    pickerWindow.Close();
                }
            };

            // 窗口关闭时的清理（处理 Alt+F4 等意外关闭）
            pickerWindow.Closed += (s, e) =>
            {
                if (!completed)
                {
                    completed = true;
                    tcs.TrySetResult(null);
                }
                Mouse.OverrideCursor = savedCursor;
            };

            pickerWindow.Show();
            pickerWindow.Activate();

            return tcs.Task;
        }

        /// <summary>
        /// 从屏幕物理像素坐标获取颜色
        /// </summary>
        private static Color GetPixelColor(int physicalX, int physicalY)
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            uint pixel = GetPixel(hdc, physicalX, physicalY);
            ReleaseDC(IntPtr.Zero, hdc);

            byte r = (byte)(pixel & 0x000000FF);
            byte g = (byte)((pixel & 0x0000FF00) >> 8);
            byte b = (byte)((pixel & 0x00FF0000) >> 16);

            return Color.FromRgb(r, g, b);
        }
    }
}
