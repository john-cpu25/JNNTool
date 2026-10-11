using System;
using System.Windows;
using System.Windows.Interop;
using Autodesk.Revit.UI;

namespace JNN.Stair.Core
{
    /// <summary>
    /// Tiện ích quản lý vòng đời và hiển thị an toàn cửa sổ WPF trong Revit Add-in.
    /// Khắc phục triệt để lỗi "The Application object is being shut down" (InvalidOperationException).
    /// </summary>
    public static class WpfWindowHelper
    {
        private static readonly object _lock = new object();

        /// <summary>
        /// Đảm bảo môi trường WPF Application tồn tại an toàn trong Revit với ShutdownMode.OnExplicitShutdown.
        /// </summary>
        public static void EnsureApplicationContext()
        {
            if (Application.Current == null)
            {
                lock (_lock)
                {
                    if (Application.Current == null)
                    {
                        new Application();
                    }
                    if (Application.Current != null)
                    {
                        // ⚠️ SỐNG CÒN: Ngăn không cho WPF tự hủy diệt môi trường UI khi đóng cửa sổ cuối cùng
                        Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                    }
                }
            }
            else if (Application.Current.ShutdownMode != ShutdownMode.OnExplicitShutdown)
            {
                try
                {
                    Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                }
                catch
                {
                    // Bỏ qua nếu môi trường không cho phép đổi lại
                }
            }
        }

        /// <summary>
        /// Mở cửa sổ dạng Modal (Khóa Revit cho đến khi đóng) với HWND Owner chuẩn và môi trường WPF được bảo vệ.
        /// </summary>
        public static bool? ShowModal(Window window, UIApplication uiApp)
        {
            EnsureApplicationContext();

            if (uiApp?.MainWindowHandle != null && uiApp.MainWindowHandle != IntPtr.Zero)
            {
                var helper = new WindowInteropHelper(window)
                {
                    Owner = uiApp.MainWindowHandle
                };
            }

            return window.ShowDialog();
        }

        /// <summary>
        /// Mở cửa sổ dạng Modeless (Người dùng vẫn tương tác được với Revit) với HWND Owner chuẩn.
        /// </summary>
        public static void ShowModeless(Window window, UIApplication uiApp)
        {
            EnsureApplicationContext();

            if (uiApp?.MainWindowHandle != null && uiApp.MainWindowHandle != IntPtr.Zero)
            {
                var helper = new WindowInteropHelper(window)
                {
                    Owner = uiApp.MainWindowHandle
                };
            }

            window.Show();
        }
    }
}
