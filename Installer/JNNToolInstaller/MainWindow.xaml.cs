using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using JNNToolInstaller.Models;
using JNNToolInstaller.Services;

namespace JNNToolInstaller;

public partial class MainWindow : Window
{
    private readonly VersionChecker _checker = new();
    private readonly InstallerService _svc = new();
    private readonly ObservableCollection<RevitVersionModel> _revitVersions = new();
    private CancellationTokenSource? _cts;

    public MainWindow()
    {
        InitializeComponent();
        RevitVersionCards.ItemsSource = _revitVersions;
        Loaded += async (_, _) => await InitAsync();
    }

    // ─── Init ────────────────────────────────────────────────────────────────

    private async Task InitAsync()
    {
        SetBusy(true);
        await _checker.LoadAsync();
        RefreshUI();
        SetBusy(false);
    }

    private void RefreshUI()
    {
        string installedVer = "Chưa rõ";
        if (_checker.IsInstalled && _checker.InstalledManifest != null)
        {
            installedVer = $"v{_checker.InstalledManifest.Version}";
            TxtInstalledVerVal.Text = installedVer;
            BtnUninstall.IsEnabled = true;
        }
        else
        {
            installedVer = "Chưa cài";
            TxtInstalledVerVal.Text = installedVer;
            BtnUninstall.IsEnabled = false;
        }

        string onlineVer = "Chưa rõ";
        if (_checker.LatestManifest != null)
        {
            var m = _checker.LatestManifest;
            onlineVer = $"v{m.Version}";
            TxtOnlineVerVal.Text = onlineVer;
            TxtChangelogVersion.Text = $"Version: {m.Version}";

            if (m.Changelog.Count > 0)
            {
                ChangelogList.ItemsSource = m.Changelog;
            }
        }

        // Header Status
        TxtHeaderOnlineVer.Text = onlineVer;
        TxtHeaderInstalledVer.Text = installedVer;

        // Status Badge Card
        if (_checker.UpdateAvailable)
        {
            TxtStatusHeadline.Text = "● Sẵn sàng cập nhật.";
            TxtStatusHeadline.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"));
            StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"));
            StatusBadgeBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0FDF4"));
            StatusBadgeBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC"));
        }
        else if (_checker.IsInstalled)
        {
            TxtStatusHeadline.Text = "● Đã là phiên bản mới nhất.";
            TxtStatusHeadline.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
            StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
            StatusBadgeBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F9FF"));
            StatusBadgeBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BAE6FD"));
        }
        else
        {
            TxtStatusHeadline.Text = "● Sẵn sàng cài đặt.";
            TxtStatusHeadline.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
            StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
            StatusBadgeBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFBEB"));
            StatusBadgeBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDE68A"));
        }

        // Revit Versions List (Full Ver 2022 - 2027)
        var detected = _svc.GetInstalledRevitVersions();
        _revitVersions.Clear();
        foreach (var year in new[] { "2022", "2023", "2024", "2025", "2026", "2027" })
        {
            bool inMachine = detected.Contains(year);
            _revitVersions.Add(new RevitVersionModel
            {
                Year = year,
                IsInstalledInMachine = inMachine,
                IsSelectedForInstall = inMachine || (int.Parse(year) >= 2024) // Mặc định chọn các bản có trong máy hoặc từ 2024 trở lên
            });
        }
    }

    // ─── Actions & Event Handlers ────────────────────────────────────────────

    private void BtnSelectAllRevit_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _revitVersions)
        {
            item.IsSelectedForInstall = true;
        }
    }

    private void BtnDeselectAllRevit_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _revitVersions)
        {
            item.IsSelectedForInstall = false;
        }
    }

    private async void BtnCheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true);
        ProgressPanel.Visibility = Visibility.Visible;
        TxtStatus.Text = "Đang kiểm tra phiên bản mới nhất từ Git (GitHub)...";
        TxtStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
        ProgressBar.IsIndeterminate = true;

        await _checker.LoadAsync();
        RefreshUI();

        ProgressBar.IsIndeterminate = false;
        ProgressPanel.Visibility = Visibility.Collapsed;
        SetBusy(false);

        if (_checker.UpdateAvailable)
        {
            MessageBox.Show(
                $"Đã tìm thấy phiên bản mới trên Git: v{_checker.LatestManifest!.Version}!\n" +
                $"Ngày phát hành: {_checker.LatestManifest.ReleaseDate}\n\n" +
                "Bạn có thể nhấn nút 'Cài đặt / Cập nhật' để tự động tải và cài đặt ngay.",
                "Có bản cập nhật mới",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else if (_checker.IsInstalled)
        {
            MessageBox.Show(
                $"Bạn đang dùng phiên bản mới nhất (v{_checker.InstalledManifest?.Version ?? "2.0.4"}) từ Git!\n\n" +
                "Nếu cần cài lại, bạn có thể nhấn nút 'Cài đặt / Cập nhật'.",
                "Đã là phiên bản mới nhất",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show(
                $"Phiên bản mới nhất trên Git: v{_checker.LatestManifest?.Version ?? "2.0.4"}.\n" +
                "Nhấn 'Cài đặt / Cập nhật' để tiến hành cài đặt vào Revit.",
                "Thông tin phiên bản",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void BtnInstall_Click(object sender, RoutedEventArgs e)
    {
        if (_checker.LatestManifest == null)
        {
            MessageBox.Show("Không đọc được version manifest.", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var selectedVersions = _revitVersions.Where(x => x.IsSelectedForInstall).ToList();
        if (selectedVersions.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn ít nhất một phiên bản Revit để cài đặt.", "Chưa chọn phiên bản Revit",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var url = _checker.LatestManifest.DownloadUrl;
        if (string.IsNullOrWhiteSpace(url) || url.Contains("YOUR_USERNAME"))
        {
            MessageBox.Show(
                "Chưa có link tải về trong manifest.\n\n" +
                "Tác giả cần upload release lên GitHub trước.\n" +
                "Link: github.com/john-cpu25/JNNTool/releases",
                "Chưa có bản phát hành",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _cts = new CancellationTokenSource();
        SetBusy(true);
        ProgressPanel.Visibility = Visibility.Visible;
        ProgressBar.Value = 0;
        ProgressBar.IsIndeterminate = false;

        _svc.StatusChanged += s => Dispatcher.Invoke(() => TxtStatus.Text = s);
        _svc.ProgressChanged += p => Dispatcher.Invoke(() => ProgressBar.Value = p);

        try
        {
            await _svc.InstallAsync(_checker.LatestManifest, _cts.Token);
            await _checker.LoadAsync();
            RefreshUI();
            ShowSuccess("Cài đặt thành công! Khởi động lại Revit để áp dụng.");
            MessageBox.Show("Cài đặt / Cập nhật JNNTool thành công!\nVui lòng khởi động lại Revit để sử dụng.", "Thành công",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            TxtStatus.Text = "Đã hủy thao tác.";
        }
        catch (System.Net.Http.HttpRequestException httpEx) when
            (httpEx.Message.Contains("404") || httpEx.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            MessageBox.Show(
                "Không tìm thấy file tải về trên GitHub (404).\n\n" +
                "Hãy upload file MSI lên GitHub Releases rồi thử lại.\n" +
                "Link: github.com/john-cpu25/JNNTool/releases",
                "Không tìm thấy bản phát hành",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi cài đặt:\n{ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void BtnUninstall_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Bạn có chắc muốn gỡ cài đặt JNNTool Plugin không?\n" +
            "Plugin sẽ bị xóa khỏi tất cả các phiên bản Revit.",
            "Xác nhận gỡ cài đặt",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        SetBusy(true);
        ProgressPanel.Visibility = Visibility.Visible;
        ProgressBar.IsIndeterminate = true;

        _svc.StatusChanged += s => Dispatcher.Invoke(() => TxtStatus.Text = s);
        _svc.ProgressChanged += p => Dispatcher.Invoke(() => ProgressBar.Value = p);

        try
        {
            await _svc.UninstallAsync();
            await _checker.LoadAsync();
            RefreshUI();
            ProgressPanel.Visibility = Visibility.Collapsed;
            ProgressBar.IsIndeterminate = false;
            ShowSuccess("Đã gỡ cài đặt thành công. Vui lòng khởi động lại Revit.");
            MessageBox.Show("Đã gỡ cài đặt JNNTool hoàn tất.", "Gỡ cài đặt",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi gỡ cài đặt:\n{ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void BtnQuickGuide_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/john-cpu25/JNNTool#readme",
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void BtnInstallGuide_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/john-cpu25/JNNTool/releases",
                UseShellExecute = true
            });
        }
        catch { }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void SetBusy(bool busy)
    {
        BtnInstall.IsEnabled = !busy;
        BtnUninstall.IsEnabled = !busy && _checker.IsInstalled;
        BtnCheckUpdate.IsEnabled = !busy;
    }

    private void ShowSuccess(string msg)
    {
        TxtStatus.Text = msg;
        TxtStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"));
    }

    // ─── Window Chrome ───────────────────────────────────────────────────────

    private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            DragMove();
    }

    private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        WindowState = WindowState.Minimized;
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        Close();
    }
}
