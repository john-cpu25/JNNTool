using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JNNToolInstaller.Services;

namespace JNNToolInstaller;

public partial class MainWindow : Window
{
    private readonly VersionChecker   _checker  = new();
    private readonly InstallerService _svc      = new();
    private CancellationTokenSource?  _cts;

    public MainWindow()
    {
        InitializeComponent();
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
        // Installed
        if (_checker.IsInstalled && _checker.InstalledManifest != null)
        {
            var m = _checker.InstalledManifest;
            TxtInstalledVersion.Text = $"v{m.Version}";
            TxtInstallDate.Text      = m.ReleaseDate;
            BtnUninstall.IsEnabled   = true;
        }
        else
        {
            TxtInstalledVersion.Text = "Chưa cài";
            TxtInstallDate.Text      = "";
            BtnUninstall.IsEnabled   = false;
        }

        // Latest
        if (_checker.LatestManifest != null)
        {
            var m = _checker.LatestManifest;
            TxtLatestVersion.Text = $"v{m.Version}";
            TxtLatestDate.Text    = m.ReleaseDate;

            // Changelog
            if (m.Changelog.Count > 0)
            {
                ChangelogList.ItemsSource = m.Changelog;
                ChangelogPanel.Visibility = Visibility.Visible;
            }
        }

        // Update badge
        if (_checker.UpdateAvailable)
        {
            TxtUpdateTitle.Text = $"Có phiên bản mới: v{_checker.LatestManifest!.Version}";
            TxtUpdateSub.Text   = "Nhấn 'Cài đặt / Cập nhật' để nâng cấp";
            UpdateBadge.Visibility = Visibility.Visible;
        }

        // BtnInstall label
        BtnInstall.Content = _checker.IsInstalled
            ? (_checker.UpdateAvailable ? "⬆  Cập nhật ngay" : "🔄  Cài lại")
            : "⬇  Cài đặt";

        // Revit versions
        RevitVersionsPanel.Children.Clear();
        var detected = _svc.GetInstalledRevitVersions();
        foreach (var year in new[] { "2022", "2023", "2024", "2025", "2026" })
        {
            bool found = detected.Contains(year);
            RevitVersionsPanel.Children.Add(MakeVersionBadge(year, found));
        }
    }

    // ─── Buttons ─────────────────────────────────────────────────────────────

    private async void BtnInstall_Click(object sender, RoutedEventArgs e)
    {
        if (_checker.LatestManifest == null)
        {
            MessageBox.Show("Kh\u00f4ng \u0111\u1ecdc \u0111\u01b0\u1ee3c version manifest.", "L\u1ed7i",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // Kiem tra downloadUrl truoc khi download
        var url = _checker.LatestManifest.DownloadUrl;
        if (string.IsNullOrWhiteSpace(url) || url.Contains("YOUR_USERNAME"))
        {
            MessageBox.Show(
                "Ch\u01b0a c\u00f3 link t\u1ea3i v\u1ec1 trong manifest.\n\n" +
                "T\u00e1c gi\u1ea3 c\u1ea7n upload release l\u00ean GitHub tr\u01b0\u1edbc.\n" +
                "Vui l\u00f2ng th\u1eed l\u1ea1i sau ho\u1eb7c xem: github.com/john-cpu25/JNNTool/releases",
                "Ch\u01b0a c\u00f3 b\u1ea3n ph\u00e1t h\u00e0nh",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _cts = new CancellationTokenSource();
        SetBusy(true);
        ProgressPanel.Visibility = Visibility.Visible;
        ProgressBar.Value = 0;

        _svc.StatusChanged   += s => Dispatcher.Invoke(() => TxtStatus.Text    = s);
        _svc.ProgressChanged += p => Dispatcher.Invoke(() => ProgressBar.Value = p);

        try
        {
            await _svc.InstallAsync(_checker.LatestManifest, _cts.Token);
            await _checker.LoadAsync();
            RefreshUI();
            ShowSuccess("C\u00e0i \u0111\u1eb7t th\u00e0nh c\u00f4ng! Kh\u1edfi \u0111\u1ed9ng l\u1ea1i Revit \u0111\u1ec3 \u00e1p d\u1ee5ng.");
        }
        catch (OperationCanceledException)
        {
            TxtStatus.Text = "\u0110\u00e3 h\u1ee7y.";
        }
        catch (System.Net.Http.HttpRequestException httpEx) when
            (httpEx.Message.Contains("404") || httpEx.StatusCode ==
             System.Net.HttpStatusCode.NotFound)
        {
            MessageBox.Show(
                "Kh\u00f4ng t\u00ecm th\u1ea5y file t\u1ea3i v\u1ec1 tr\u00ean GitHub (404).\n\n" +
                "H\u00e3y upload file MSI l\u00ean GitHub Releases r\u1ed3i th\u1eed l\u1ea1i.\n" +
                "Link: github.com/john-cpu25/JNNTool/releases",
                "Kh\u00f4ng t\u00ecm th\u1ea5y b\u1ea3n ph\u00e1t h\u00e0nh",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"L\u1ed7i c\u00e0i \u0111\u1eb7t:\n{ex.Message}", "L\u1ed7i",
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

        _svc.StatusChanged   += s => Dispatcher.Invoke(() => TxtStatus.Text    = s);
        _svc.ProgressChanged += p => Dispatcher.Invoke(() => ProgressBar.Value = p);

        try
        {
            await _svc.UninstallAsync();
            await _checker.LoadAsync();
            RefreshUI();
            ProgressPanel.Visibility = Visibility.Collapsed;
            ShowSuccess("Đã gỡ cài đặt thành công. Vui lòng khởi động lại Revit.");
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
        BtnInstall.IsEnabled   = !busy;
        BtnUninstall.IsEnabled = !busy && _checker.IsInstalled;
    }

    private void ShowSuccess(string msg)
    {
        TxtStatus.Text    = msg;
        TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(74, 222, 128)); // green
    }

    private static Border MakeVersionBadge(string year, bool detected)
    {
        var color = detected ? "#1B2B1F" : "#1E2340";
        var fg    = detected ? "#4ADE80" : "#3A4270";
        var icon  = detected ? "✓" : "—";

        return new Border
        {
            Background     = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
            CornerRadius   = new CornerRadius(8),
            Padding        = new Thickness(12, 6, 12, 6),
            Margin         = new Thickness(0, 0, 8, 8),
            Child          = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children    =
                {
                    new TextBlock
                    {
                        Text       = $"{icon}  Revit {year}",
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg)),
                        FontSize   = 12,
                        FontWeight = detected ? FontWeights.SemiBold : FontWeights.Normal
                    }
                }
            }
        };
    }

    // ─── Window chrome ────────────────────────────────────────────────────────
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
