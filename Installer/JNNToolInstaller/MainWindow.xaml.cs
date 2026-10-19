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
        _svc.StatusChanged += s => Dispatcher.Invoke(() => TxtStatus.Text = s);
        _svc.ProgressChanged += p => Dispatcher.Invoke(() =>
        {
            ProgressBar.Value = p;
            TxtProgressPercent.Text = p >= 100 ? "100%" : $"{p}%";
            if (p >= 100)
            {
                TxtStatus.Text = "Hoàn tất!";
            }
        });
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
            TxtHeaderInstalledVer.Text = installedVer;
            TxtHeaderInstalledVer.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34D399"));
            BtnUninstall.IsEnabled = true;
        }
        else
        {
            installedVer = "Chưa cài";
            TxtInstalledVerVal.Text = installedVer;
            TxtHeaderInstalledVer.Text = installedVer;
            TxtHeaderInstalledVer.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            BtnUninstall.IsEnabled = false;
        }

        string onlineVer = "v2.1.3";
        if (_checker.LatestManifest != null)
        {
            var m = _checker.LatestManifest;
            onlineVer = $"v{m.Version}";
            TxtOnlineVerVal.Text = onlineVer;
            TxtHeaderOnlineVer.Text = onlineVer;
            TxtChangelogVersion.Text = $"Version {m.Version}";

            if (m.Changelog.Count > 0)
            {
                ChangelogList.ItemsSource = m.Changelog;
            }
        }
        else
        {
            TxtOnlineVerVal.Text = onlineVer;
            TxtHeaderOnlineVer.Text = onlineVer;
        }

        // Status Headline & Badge Card
        if (_checker.UpdateAvailable)
        {
            TxtStatusHeadline.Text = "Sẵn sàng cập nhật";
            TxtStatusHeadline.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34D399"));
            StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
            StatusBadgeBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1510B981"));
            StatusBadgeBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3310B981"));
        }
        else if (_checker.IsInstalled)
        {
            TxtStatusHeadline.Text = "Đã là phiên bản mới nhất";
            TxtStatusHeadline.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#60A5FA"));
            StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
            StatusBadgeBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#153B82F6"));
            StatusBadgeBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333B82F6"));
        }
        else
        {
            TxtStatusHeadline.Text = "Sẵn sàng cài đặt";
            TxtStatusHeadline.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCD34D"));
            StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            StatusBadgeBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15F59E0B"));
            StatusBadgeBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#33F59E0B"));
        }

        // Revit Versions List (Full Ver 2022 - 2027)
        var detected = _svc.GetInstalledRevitVersions();
        _revitVersions.Clear();
        foreach (var year in new[] { "2022", "2023", "2024", "2025", "2026", "2027" })
        {
            bool inMachine = detected.Contains(year);
            var model = new RevitVersionModel
            {
                Year = year,
                IsInstalledInMachine = inMachine,
                IsSelectedForInstall = inMachine || (int.Parse(year) >= 2024)
            };
            model.PropertyChanged += (_, _) => UpdateSelectedCount();
            _revitVersions.Add(model);
        }

        UpdateSelectedCount();
    }

    private void UpdateSelectedCount()
    {
        int count = _revitVersions.Count(x => x.IsSelectedForInstall);
        TxtSelectedCount.Text = count.ToString();
        BadgeCountBorder.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    // ─── Tabs Switching ──────────────────────────────────────────────────────

    private void TabChangelog_Click(object sender, RoutedEventArgs e)
    {
        ChangelogView.Visibility = Visibility.Visible;
        InfoView.Visibility = Visibility.Collapsed;

        TabChangelogBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
        TabChangelogTxt.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#60A5FA"));

        TabInfoBorder.BorderBrush = Brushes.Transparent;
        TabInfoTxt.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
    }

    private void TabInfo_Click(object sender, RoutedEventArgs e)
    {
        ChangelogView.Visibility = Visibility.Collapsed;
        InfoView.Visibility = Visibility.Visible;

        TabInfoBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
        TabInfoTxt.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#60A5FA"));

        TabChangelogBorder.BorderBrush = Brushes.Transparent;
        TabChangelogTxt.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
    }

    // ─── Actions & Event Handlers ────────────────────────────────────────────

    private void RevitCard_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is RevitVersionModel model)
        {
            model.IsSelectedForInstall = !model.IsSelectedForInstall;
        }
    }

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
        TxtProgressPercent.Text = "";
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
                $"Bạn đang dùng phiên bản mới nhất (v{_checker.InstalledManifest?.Version ?? "2.1.3"}) từ Git!\n\n" +
                "Nếu cần cài lại, bạn có thể nhấn nút 'Cài đặt / Cập nhật'.",
                "Đã là phiên bản mới nhất",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show(
                $"Phiên bản mới nhất trên Git: v{_checker.LatestManifest?.Version ?? "2.1.3"}.\n" +
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

        try
        {
            await _svc.InstallAsync(_checker.LatestManifest, _cts.Token);
            await _checker.LoadAsync();
            RefreshUI();
            TxtStatus.Text = "Hoàn tất!";
            TxtProgressPercent.Text = "100%";
            ProgressBar.Value = 100;
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

        try
        {
            await _svc.UninstallAsync();
            await _checker.LoadAsync();
            RefreshUI();
            ProgressPanel.Visibility = Visibility.Collapsed;
            ProgressBar.IsIndeterminate = false;
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

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void SetBusy(bool busy)
    {
        BtnInstall.IsEnabled = !busy;
        BtnUninstall.IsEnabled = !busy && _checker.IsInstalled;
        BtnCheckUpdate.IsEnabled = !busy;
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
