using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JNNToolInstaller.Models;
using JNNToolInstaller.Services;

namespace JNNToolInstaller;

public partial class MainWindow : Window
{
    private readonly VersionChecker _checker = new();
    private readonly InstallerService _svc = new();
    private readonly ObservableCollection<RevitVersionModel> _revitVersions = new();
    private readonly ObservableCollection<GitHubReleaseItem> _releasesList = new();
    private GitHubReleaseItem? _selectedRelease;
    private CancellationTokenSource? _cts;

    public MainWindow()
    {
        InitializeComponent();
        RevitVersionCards.ItemsSource = _revitVersions;
        CmbReleaseSelect.ItemsSource = _releasesList;

        _svc.StatusChanged += s => Dispatcher.Invoke(() => TxtStatus.Text = s);
        _svc.ProgressChanged += p => Dispatcher.Invoke(() =>
        {
            ProgressBar.Value = p;
            TxtProgressPercent.Text = p >= 100 ? "100%" : $"{p}%";
            if (p >= 100)
            {
                TxtStatus.Text = LocalizationService.Completed;
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
        ApplyLanguage();
        SetBusy(false);
    }

    private void RefreshUI()
    {
        // 1. Installed Version
        string installedVer;
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
            installedVer = LocalizationService.NotInstalled;
            TxtInstalledVerVal.Text = installedVer;
            TxtHeaderInstalledVer.Text = installedVer;
            TxtHeaderInstalledVer.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            BtnUninstall.IsEnabled = false;
        }

        // 2. Online Version
        string onlineVer = "v2.1.3";
        if (_checker.LatestManifest != null)
        {
            onlineVer = $"v{_checker.LatestManifest.Version}";
        }
        TxtOnlineVerVal.Text = onlineVer;
        TxtHeaderOnlineVer.Text = onlineVer;

        // 3. Status Headline & Badge Card
        UpdateStatusBadge();

        // 4. Releases Dropdown (Versions List)
        _releasesList.Clear();
        foreach (var rel in _checker.AvailableReleases)
        {
            _releasesList.Add(rel);
        }

        if (_releasesList.Count > 0)
        {
            CmbReleaseSelect.SelectedIndex = 0;
            _selectedRelease = _releasesList[0];
            UpdateChangelogView(_selectedRelease);
        }
        else if (_checker.LatestManifest != null)
        {
            var fallback = new GitHubReleaseItem
            {
                TagName = $"v{_checker.LatestManifest.Version}",
                IsLatest = true,
                Changelog = _checker.LatestManifest.Changelog
            };
            _releasesList.Add(fallback);
            CmbReleaseSelect.SelectedIndex = 0;
            _selectedRelease = fallback;
            UpdateChangelogView(fallback);
        }

        // 5. Revit Versions List (Full Ver 2022 - 2027)
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
        UpdateInstallActionButton();
    }

    private void UpdateStatusBadge()
    {
        if (_checker.UpdateAvailable)
        {
            TxtStatusHeadline.Text = LocalizationService.StatusReadyUpdate;
            TxtStatusHeadline.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34D399"));
            StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
            StatusBadgeBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1510B981"));
            StatusBadgeBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3310B981"));
        }
        else if (_checker.IsInstalled)
        {
            TxtStatusHeadline.Text = LocalizationService.StatusUpToDate;
            TxtStatusHeadline.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#60A5FA"));
            StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
            StatusBadgeBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#153B82F6"));
            StatusBadgeBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333B82F6"));
        }
        else
        {
            TxtStatusHeadline.Text = LocalizationService.StatusReadyInstall;
            TxtStatusHeadline.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCD34D"));
            StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            StatusBadgeBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15F59E0B"));
            StatusBadgeBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#33F59E0B"));
        }
    }

    private void UpdateChangelogView(GitHubReleaseItem item)
    {
        TxtChangelogVersion.Text = $"Version {item.Version}";
        ChangelogList.ItemsSource = item.Changelog;
    }

    private void UpdateInstallActionButton()
    {
        if (_checker.IsInstalled && _checker.InstalledManifest != null && _selectedRelease != null)
        {
            if (_selectedRelease.AsVersion() < _checker.InstalledManifest.AsVersion())
            {
                TxtInstallAction.Text = LocalizationService.DowngradeBtn;
                return;
            }
        }
        TxtInstallAction.Text = LocalizationService.InstallBtn;
    }

    private void UpdateSelectedCount()
    {
        int count = _revitVersions.Count(x => x.IsSelectedForInstall);
        TxtSelectedCount.Text = count.ToString();
        BadgeCountBorder.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    // ─── Bilingual Live Toggle ───────────────────────────────────────────────

    private void BtnLang_Click(object sender, RoutedEventArgs e)
    {
        LocalizationService.ToggleLanguage();
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        BtnLang.Content = LocalizationService.LangBtnText;
        TxtHeaderOnlineLabel.Text = LocalizationService.OnlineLabel;
        TxtHeaderMachineLabel.Text = LocalizationService.MachineLabel;
        TxtHeaderWarning.Text = LocalizationService.HeaderWarning;

        if (!_checker.IsInstalled || _checker.InstalledManifest == null)
        {
            TxtHeaderInstalledVer.Text = LocalizationService.NotInstalled;
            TxtInstalledVerVal.Text = LocalizationService.NotInstalled;
        }

        TxtOnlineVerLabel.Text = LocalizationService.OnlineVersionLabel;
        TxtInstalledVerLabel.Text = LocalizationService.CurrentVersionLabel;
        TxtHint.Text = LocalizationService.HintText;

        TabChangelogTxt.Text = LocalizationService.TabChangelog;
        TabInfoTxt.Text = LocalizationService.TabInfo;
        TxtChangelogSubtitle.Text = LocalizationService.ChangelogSubtitle;

        TxtInfoHeader.Text = LocalizationService.InfoHeader;
        TxtInfoSubHeader.Text = LocalizationService.InfoSubHeader;
        TxtInfoPlatform.Text = LocalizationService.InfoPlatform;
        TxtInfoDeveloper.Text = LocalizationService.InfoDeveloper;
        TxtInfoRepo.Text = LocalizationService.InfoRepo;
        TxtInfoScope.Text = LocalizationService.InfoScope;

        TxtRevitSectionTitle.Text = LocalizationService.RevitVersionsTitle;
        TxtRevitSectionSubtitle.Text = LocalizationService.RevitVersionsSubtitle;
        BtnSelectAll.Content = LocalizationService.SelectAll;
        BtnDeselectAll.Content = LocalizationService.DeselectAll;

        BtnUninstall.Content = LocalizationService.UninstallBtn;
        BtnCheckUpdate.Content = LocalizationService.CheckFileBtn;

        UpdateStatusBadge();
        UpdateInstallActionButton();

        // Refresh Revit Version status texts
        foreach (var v in _revitVersions)
        {
            v.RefreshLanguage();
        }

        // Refresh dropdown items text
        int currentIdx = CmbReleaseSelect.SelectedIndex;
        CmbReleaseSelect.Items.Refresh();
        CmbReleaseSelect.SelectedIndex = currentIdx;
    }

    // ─── Release Selection ───────────────────────────────────────────────────

    private void CmbReleaseSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbReleaseSelect.SelectedItem is GitHubReleaseItem selected)
        {
            _selectedRelease = selected;
            UpdateChangelogView(selected);
            UpdateInstallActionButton();
        }
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
        TxtStatus.Text = LocalizationService.IsEnglish
            ? "Checking for latest releases from GitHub..."
            : "Đang kiểm tra phiên bản mới nhất từ Git (GitHub)...";
        TxtProgressPercent.Text = "";
        ProgressBar.IsIndeterminate = true;

        await _checker.LoadAsync();
        RefreshUI();
        ApplyLanguage();

        ProgressBar.IsIndeterminate = false;
        ProgressPanel.Visibility = Visibility.Collapsed;
        SetBusy(false);

        if (_checker.UpdateAvailable)
        {
            string msg = LocalizationService.IsEnglish
                ? $"New version found on GitHub: v{_checker.LatestManifest!.Version}!\n" +
                  $"Release date: {_checker.LatestManifest.ReleaseDate}\n\n" +
                  "Click 'Install / Update' to automatically download and install now."
                : $"Đã tìm thấy phiên bản mới trên Git: v{_checker.LatestManifest!.Version}!\n" +
                  $"Ngày phát hành: {_checker.LatestManifest.ReleaseDate}\n\n" +
                  "Bạn có thể nhấn nút 'Cài đặt / Cập nhật' để tự động tải và cài đặt ngay.";

            string title = LocalizationService.IsEnglish ? "Update Available" : "Có bản cập nhật mới";
            MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else if (_checker.IsInstalled)
        {
            string msg = LocalizationService.IsEnglish
                ? $"You are using the latest version (v{_checker.InstalledManifest?.Version ?? "2.1.3"})!\n\n" +
                  "If you need to reinstall, click 'Install / Update'."
                : $"Bạn đang dùng phiên bản mới nhất (v{_checker.InstalledManifest?.Version ?? "2.1.3"}) từ Git!\n\n" +
                  "Nếu cần cài lại, bạn có thể nhấn nút 'Cài đặt / Cập nhật'.";

            string title = LocalizationService.IsEnglish ? "Up to Date" : "Đã là phiên bản mới nhất";
            MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            string msg = LocalizationService.IsEnglish
                ? $"Latest version on GitHub: v{_checker.LatestManifest?.Version ?? "2.1.3"}.\n" +
                  "Click 'Install' to install into Revit."
                : $"Phiên bản mới nhất trên Git: v{_checker.LatestManifest?.Version ?? "2.1.3"}.\n" +
                  "Nhấn 'Cài đặt / Cập nhật' để tiến hành cài đặt vào Revit.";

            string title = LocalizationService.IsEnglish ? "Version Info" : "Thông tin phiên bản";
            MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void BtnInstall_Click(object sender, RoutedEventArgs e)
    {
        var targetManifest = new VersionManifest
        {
            Version = _selectedRelease?.Version ?? _checker.LatestManifest?.Version ?? "2.1.3",
            DownloadUrl = _selectedRelease?.DownloadUrl ?? _checker.LatestManifest?.DownloadUrl ?? "",
            ReleaseDate = _checker.LatestManifest?.ReleaseDate ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Changelog = _selectedRelease?.Changelog ?? _checker.LatestManifest?.Changelog ?? new List<string>()
        };

        var selectedVersions = _revitVersions.Where(x => x.IsSelectedForInstall).ToList();
        if (selectedVersions.Count == 0)
        {
            MessageBox.Show(
                LocalizationService.MsgSelectRevitPrompt,
                LocalizationService.MsgSelectRevitTitle,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var url = targetManifest.DownloadUrl;
        if (string.IsNullOrWhiteSpace(url) || url.Contains("YOUR_USERNAME"))
        {
            string msg = LocalizationService.IsEnglish
                ? "No download link found for this release.\n\nLink: github.com/john-cpu25/JNNTool/releases"
                : "Chưa có link tải về trong manifest.\n\nLink: github.com/john-cpu25/JNNTool/releases";
            string title = LocalizationService.IsEnglish ? "No Release Found" : "Chưa có bản phát hành";
            MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _cts = new CancellationTokenSource();
        SetBusy(true);
        ProgressPanel.Visibility = Visibility.Visible;
        ProgressBar.Value = 0;
        ProgressBar.IsIndeterminate = false;
        TxtStatus.Text = LocalizationService.Installing;

        try
        {
            await _svc.InstallAsync(targetManifest, _cts.Token);
            await _checker.LoadAsync();
            RefreshUI();
            ApplyLanguage();
            TxtStatus.Text = LocalizationService.Completed;
            TxtProgressPercent.Text = "100%";
            ProgressBar.Value = 100;
            MessageBox.Show(
                LocalizationService.MsgInstallSuccess,
                LocalizationService.IsEnglish ? "Success" : "Thành công",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            TxtStatus.Text = LocalizationService.IsEnglish ? "Operation cancelled." : "Đã hủy thao tác.";
        }
        catch (System.Net.Http.HttpRequestException httpEx) when
            (httpEx.Message.Contains("404") || httpEx.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            string notFoundMsg = LocalizationService.IsEnglish
                ? $"Could not find MSI package for release v{targetManifest.Version} on GitHub (404).\n\nPlease check github.com/john-cpu25/JNNTool/releases"
                : $"Không tìm thấy file tải về cho bản v{targetManifest.Version} trên GitHub (404).\n\nHãy kiểm tra lại github.com/john-cpu25/JNNTool/releases";
            MessageBox.Show(notFoundMsg, LocalizationService.IsEnglish ? "Release Not Found" : "Không tìm thấy bản phát hành",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void BtnUninstall_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            LocalizationService.MsgConfirmUninstall,
            LocalizationService.MsgUninstallTitle,
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        SetBusy(true);
        ProgressPanel.Visibility = Visibility.Visible;
        ProgressBar.IsIndeterminate = true;
        TxtStatus.Text = LocalizationService.IsEnglish ? "Uninstalling JNNTool..." : "Đang gỡ cài đặt JNNTool...";

        try
        {
            await _svc.UninstallAsync();
            await _checker.LoadAsync();
            RefreshUI();
            ApplyLanguage();
            ProgressPanel.Visibility = Visibility.Collapsed;
            ProgressBar.IsIndeterminate = false;
            MessageBox.Show(
                LocalizationService.MsgUninstallSuccess,
                LocalizationService.IsEnglish ? "Uninstall Completed" : "Gỡ cài đặt",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
        CmbReleaseSelect.IsEnabled = !busy;
        BtnLang.IsEnabled = !busy;
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
