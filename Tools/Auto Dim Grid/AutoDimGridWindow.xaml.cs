using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Core.Logging;
using JNNTool.Core.Settings;
using JNNTool.Tools.AutoDimGrid.Models;

namespace JNNTool.Tools.AutoDimGrid
{
    public partial class AutoDimGridWindow : Window
    {
        private UIApplication _uiApp;
        private ExternalEvent _exEvent;
        private AutoDimGridHandler _handler;

        public AutoDimGridWindow(UIApplication uiApp, ExternalEvent exEvent, AutoDimGridHandler handler, List<DimensionType> dimTypes)
        {
            InitializeComponent();
            _uiApp = uiApp;
            _exEvent = exEvent;
            _handler = handler;

            // Gắn Owner vào Revit thay vì Topmost để cửa sổ không che các ứng dụng khác.
            try { new WindowInteropHelper(this).Owner = uiApp.MainWindowHandle; }
            catch (Exception ex) { Logger.Error("Auto Dim Grid: không gắn được Owner", ex); }

            _handler.Completed = () => Dispatcher.Invoke(() => btnRun.IsEnabled = true);

            // Exception lọt ra khỏi event handler WPF sẽ làm Revit văng "unrecoverable error"; chặn và ghi log.
            Dispatcher.UnhandledException += Dispatcher_UnhandledException;

            cbDimType.ItemsSource = dimTypes;
            LoadSettings(dimTypes);
        }

        private void LoadSettings(List<DimensionType> dimTypes)
        {
            AutoDimGridSettings s = SettingsService<AutoDimGridSettings>.Load() ?? new AutoDimGridSettings();

            tcMain.SelectedIndex = s.LastTabIndex >= 0 && s.LastTabIndex <= 1 ? s.LastTabIndex : 0;
            rb3Layers.IsChecked = s.LayersCount >= 3;
            rb2Layers.IsChecked = s.LayersCount < 3;
            cbOffsetFromGridEndpoint.IsChecked = s.IsOffsetFromGridEndpoint;
            tbDistanceToFirstDim.Text = s.DistanceToFirstDimMm.ToString(CultureInfo.InvariantCulture);
            tbDistanceBetweenDims.Text = s.DistanceBetweenDimsMm.ToString(CultureInfo.InvariantCulture);

            int idx = dimTypes.FindIndex(d => d.Name == s.DimTypeName);
            if (idx >= 0) cbDimType.SelectedIndex = idx;
            else if (dimTypes.Any()) cbDimType.SelectedIndex = 0;
        }

        private void SaveSettings()
        {
            try
            {
                SettingsService<AutoDimGridSettings>.Save(new AutoDimGridSettings
                {
                    LastTabIndex = tcMain.SelectedIndex,
                    LayersCount = _handler.LayersCount,
                    IsOffsetFromGridEndpoint = _handler.IsOffsetFromGridEndpoint,
                    DistanceToFirstDimMm = _handler.DistanceToFirstDimMm,
                    DistanceBetweenDimsMm = _handler.DistanceBetweenDimsMm,
                    DimTypeName = (cbDimType.SelectedItem as DimensionType)?.Name ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Auto Dim Grid: lỗi lưu cài đặt", ex);
            }
        }

        private static bool TryParsePositive(string text, out double value)
        {
            // Chấp nhận cả dấu phẩy lẫn dấu chấm thập phân.
            string t = (text ?? string.Empty).Trim().Replace(',', '.');
            return double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value > 0;
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            _handler.IsAutoAllSides = true;
            _handler.LayersCount = rb3Layers.IsChecked == true ? 3 : 2;
            _handler.IsOffsetFromGridEndpoint = cbOffsetFromGridEndpoint.IsChecked == true;
            _handler.SelectedDimType = cbDimType.SelectedItem as DimensionType;

            if (!TryParsePositive(tbDistanceToFirstDim.Text, out double dist1) ||
                !TryParsePositive(tbDistanceBetweenDims.Text, out double dist2))
            {
                MessageBox.Show(this, "Please enter positive numbers for dimension distances (mm on sheet).", "Input Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _handler.DistanceToFirstDimMm = dist1;
            _handler.DistanceBetweenDimsMm = dist2;

            SaveSettings();

            // Giữ cửa sổ mở để chạy lại; khóa nút tới khi handler báo xong để tránh Raise trùng.
            btnRun.IsEnabled = false;
            ExternalEventRequest req = _exEvent.Raise();
            if (req != ExternalEventRequest.Accepted)
            {
                btnRun.IsEnabled = true;
                MessageBox.Show(this, $"Revit is busy, please try again ({req}).", "Auto Dim Grid",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // Chỉ xử lý khi cửa sổ còn mở, tránh nuốt lỗi của các add-in khác dùng chung Dispatcher.
            if (!IsLoaded) return;
            e.Handled = true;
            Logger.Error("Auto Dim Grid: lỗi giao diện", e.Exception);
            btnRun.IsEnabled = true;
            MessageBox.Show(this, $"Lỗi: {e.Exception.Message}", "Auto Dim Grid", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        protected override void OnClosed(EventArgs e)
        {
            Dispatcher.UnhandledException -= Dispatcher_UnhandledException;
            _handler.Completed = null;
            base.OnClosed(e);
        }
    }
}
