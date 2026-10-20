using System;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.UI;

namespace JNNTool.Core.Palette
{
    /// <summary>
    /// Dockable Pane host cực nhẹ — chỉ đăng ký với Revit lúc OnStartup.
    /// Nội dung thực (CommandPalettePage) được lazy-load lần đầu khi pane hiện ra,
    /// tránh crash Access Violation do WPF XAML phức tạp chạy quá sớm.
    /// </summary>
    public partial class PaletteHost : UserControl, IDockablePaneProvider
    {
        public static readonly DockablePaneId PaneId =
            new DockablePaneId(new Guid("D5C7B6E2-3A19-4F58-9A2E-8E14B927CF01"));

        public const string PaneTitle = "JNN Command Palette";

        private bool _paletteContentLoaded;

        public PaletteHost()
        {
            InitializeComponent();

            // Khi pane được Revit render lần đầu, inject nội dung thực
            IsVisibleChanged += OnVisibleChanged;
        }

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            try
            {
                data.FrameworkElement = this;
                data.VisibleByDefault = false;
                data.InitialState = new DockablePaneState
                {
                    DockPosition = DockPosition.Left,
                    MinimumWidth = 240
                };
            }
            catch (Exception ex)
            {
                JNNTool.Core.Logging.Logger.Error("Lỗi SetupDockablePane", ex);
            }
        }

        /// <summary>
        /// Lazy inject CommandPalettePage khi pane hiện ra lần đầu.
        /// </summary>
        private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_paletteContentLoaded) return;
            if (IsVisible)
            {
                try
                {
                    _paletteContentLoaded = true;
                    RootContainer.Children.Clear();
                    RootContainer.Children.Add(new CommandPalettePage());
                }
                catch (Exception ex)
                {
                    _paletteContentLoaded = false;
                    JNNTool.Core.Logging.Logger.Error("Lỗi lazy-load CommandPalettePage", ex);
                }
            }
        }

        /// <summary>
        /// Cho phép force-load nội dung (ví dụ từ TogglePaletteCmd).
        /// </summary>
        public void EnsureContentLoaded()
        {
            if (_paletteContentLoaded) return;
            try
            {
                _paletteContentLoaded = true;
                RootContainer.Children.Clear();
                RootContainer.Children.Add(new CommandPalettePage());
            }
            catch (Exception ex)
            {
                _paletteContentLoaded = false;
                JNNTool.Core.Logging.Logger.Error("Lỗi force-load CommandPalettePage", ex);
            }
        }
    }
}
