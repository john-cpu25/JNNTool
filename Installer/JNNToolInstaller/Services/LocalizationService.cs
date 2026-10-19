using System.IO;

namespace JNNToolInstaller.Services;

public enum AppLanguage
{
    VI,
    EN
}

public static class LocalizationService
{
    private static readonly string SettingsFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "JNNTool", "updater_lang.txt");

    public static AppLanguage CurrentLanguage { get; set; } = AppLanguage.VI;

    public static bool IsEnglish => CurrentLanguage == AppLanguage.EN;

    static LocalizationService()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var lang = File.ReadAllText(SettingsFile).Trim().ToUpperInvariant();
                if (lang == "EN") CurrentLanguage = AppLanguage.EN;
            }
        }
        catch { }
    }

    public static void ToggleLanguage()
    {
        CurrentLanguage = (CurrentLanguage == AppLanguage.VI) ? AppLanguage.EN : AppLanguage.VI;
        try
        {
            var dir = Path.GetDirectoryName(SettingsFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(SettingsFile, CurrentLanguage.ToString());
        }
        catch { }
    }

    // ─── UI Text Resources ───────────────────────────────────────────────────

    public static string LangBtnText => IsEnglish ? "🌐 EN" : "🌐 VI";
    public static string HeaderWarning => IsEnglish ? "Please CLOSE REVIT before install" : "Vui lòng TẮT REVIT trước khi cài";
    public static string OnlineLabel => "Online ";
    public static string MachineLabel => IsEnglish ? "PC: " : "Máy: ";
    public static string NotInstalled => IsEnglish ? "Not installed" : "Chưa cài";
    public static string Installed => IsEnglish ? "Installed" : "Đã cài đặt";

    public static string StatusReadyInstall => IsEnglish ? "Ready to install" : "Sẵn sàng cài đặt";
    public static string StatusReadyUpdate => IsEnglish ? "Update available" : "Sẵn sàng cập nhật";
    public static string StatusUpToDate => IsEnglish ? "Already up to date" : "Đã là phiên bản mới nhất";

    public static string OnlineVersionLabel => IsEnglish ? "Online Version" : "Phiên bản Online";
    public static string CurrentVersionLabel => IsEnglish ? "Current Version" : "Hiện tại";

    public static string HintText => IsEnglish
        ? "Select Revit versions below, then click \"Install\" to proceed."
        : "Chọn phiên bản Revit bên dưới rồi bấm \"Cài đặt\" để cập nhật.";

    public static string TabChangelog => IsEnglish ? "Changelog" : "Nhật ký phiên bản";
    public static string TabInfo => IsEnglish ? "Information" : "Thông tin";
    public static string ChangelogSubtitle => IsEnglish ? "JNNTool Release Notes" : "Nhật ký phiên bản JNNTool";

    public static string InfoHeader => "JNN-STRUCTURAL REVIT ADD-IN";
    public static string InfoSubHeader => IsEnglish ? "Release Version: v2.1.3 (Auto-Deploy Per-User MSI)" : "Phiên bản phát hành: v2.1.3 (Auto-Deploy Per-User MSI)";
    public static string InfoPlatform => IsEnglish ? "• Supported Platforms: Autodesk Revit 2022, 2023, 2024, 2025, 2026, 2027" : "• Nền tảng hỗ trợ: Autodesk Revit 2022, 2023, 2024, 2025, 2026, 2027";
    public static string InfoDeveloper => IsEnglish ? "• Developer: JNN Engineering Team" : "• Tác giả: JNN Engineering Team";
    public static string InfoRepo => IsEnglish ? "• Repository: github.com/john-cpu25/JNNTool" : "• Kho phân phối: github.com/john-cpu25/JNNTool";
    public static string InfoScope => IsEnglish ? "• Install Scope: %APPDATA% (No Admin Rights Required)" : "• Chuẩn cài đặt: %APPDATA% (Không cần quyền Administrator)";

    public static string RevitVersionsTitle => IsEnglish ? "Revit Versions" : "Phiên bản Revit";
    public static string RevitVersionsSubtitle => IsEnglish ? " (Row 1: Select | Row 2: Installed)" : " (Dòng 1: Chọn cài | Dòng 2: Trong máy)";
    public static string SelectAll => IsEnglish ? "Select All" : "Chọn tất cả";
    public static string DeselectAll => IsEnglish ? "Deselect All" : "Bỏ chọn";

    public static string UninstallBtn => IsEnglish ? "🗑 Uninstall" : "🗑 Gỡ cài đặt";
    public static string CheckFileBtn => IsEnglish ? "🔍 Check Updates" : "🔍 Kiểm tra File";
    public static string InstallBtn => IsEnglish ? "Install / Update" : "Cài đặt / Cập nhật";
    public static string DowngradeBtn => IsEnglish ? "Install / Rollback" : "Cài đặt / Hạ cấp";
    public static string Installing => IsEnglish ? "Installing..." : "Đang cài đặt...";
    public static string Completed => IsEnglish ? "Completed!" : "Hoàn tất!";
    public static string LatestReleaseText => IsEnglish ? "Latest version" : "Bản mới nhất";

    // Dialog messages
    public static string MsgConfirmUninstall => IsEnglish
        ? "Are you sure you want to uninstall JNNTool Plugin?\nThe plugin will be removed from all Revit versions."
        : "Bạn có chắc muốn gỡ cài đặt JNNTool Plugin không?\nPlugin sẽ bị xóa khỏi tất cả các phiên bản Revit.";

    public static string MsgUninstallTitle => IsEnglish ? "Confirm Uninstall" : "Xác nhận gỡ cài đặt";
    public static string MsgUninstallSuccess => IsEnglish ? "JNNTool uninstallation completed." : "Đã gỡ cài đặt JNNTool hoàn tất.";
    public static string MsgInstallSuccess => IsEnglish ? "JNNTool installation / update succeeded!\nPlease restart Revit to apply." : "Cài đặt / Cập nhật JNNTool thành công!\nVui lòng khởi động lại Revit để sử dụng.";
    public static string MsgSelectRevitPrompt => IsEnglish ? "Please select at least one Revit version to install." : "Vui lòng chọn ít nhất một phiên bản Revit để cài đặt.";
    public static string MsgSelectRevitTitle => IsEnglish ? "No Revit Version Selected" : "Chưa chọn phiên bản Revit";
}
