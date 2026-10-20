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

        public static PaletteHost? Instance { get; private set; }
        public static CommandPalettePage? PageInstance { get; private set; }

        private bool _paletteContentLoaded;

        public PaletteHost()
        {
            Instance = this;
            InitializeComponent();

            // Khi pane được Revit render lần đầu, inject nội dung thực
            IsVisibleChanged += OnVisibleChanged;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            EnsureContentLoaded();
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
                EnsureContentLoaded();
            }
        }

        /// <summary>
        /// Cho phép load nội dung một cách an toàn và lưu static reference để chống GC.
        /// </summary>
        public void EnsureContentLoaded()
        {
            if (_paletteContentLoaded && RootContainer.Children.Count > 0) return;

            try
            {
                JNNTool.Core.UI.WpfWindowHelper.EnsureApplicationContext();

                if (PageInstance == null)
                {
                    PageInstance = new CommandPalettePage();
                }

                if (!RootContainer.Children.Contains(PageInstance))
                {
                    if (PageInstance.Parent is Panel oldParent)
                    {
                        oldParent.Children.Remove(PageInstance);
                    }
                    RootContainer.Children.Clear();
                    RootContainer.Children.Add(PageInstance);
                }

                _paletteContentLoaded = true;
            }
            catch (Exception ex)
            {
                _paletteContentLoaded = false;
                JNNTool.Core.Logging.Logger.Error("Lỗi khởi tạo CommandPalettePage trong PaletteHost", ex);
            }
        }
    }
}
