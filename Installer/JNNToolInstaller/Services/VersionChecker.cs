using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using JNNToolInstaller.Models;

namespace JNNToolInstaller.Services;

public class VersionChecker
{
    public const string DefaultGitHubManifestUrl =
        "https://raw.githubusercontent.com/john-cpu25/JNNTool/main/version.json";

    // Nơi plugin được cài
    private static readonly string InstallRoot =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                     "Autodesk", "ApplicationPlugins", "JNNTool.bundle");

    private static readonly string InstalledVersionFile =
        Path.Combine(InstallRoot, "version.json");

    // version.json cạnh installer exe
    private static readonly string ManifestFile =
        Path.Combine(AppContext.BaseDirectory, "version.json");

    public VersionManifest? InstalledManifest { get; private set; }
    public VersionManifest? LatestManifest    { get; private set; }
    public bool IsFetchedFromGit              { get; private set; }

    public bool IsInstalled => Directory.Exists(InstallRoot);

    public bool UpdateAvailable
    {
        get
        {
            if (!IsInstalled || LatestManifest == null) return false;
            if (InstalledManifest == null) return true;
            return LatestManifest.AsVersion() > InstalledManifest.AsVersion();
        }
    }

    public async Task LoadAsync()
    {
        IsFetchedFromGit = false;

        // 1. Luôn ưu tiên lấy bản mới nhất trực tiếp từ GitHub Git
        try
        {
            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(8);
            http.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
            http.DefaultRequestHeaders.Add("User-Agent", "JNNToolInstaller");

            // Cache buster tránh bị GitHub CDN lưu đệm
            var url = $"{DefaultGitHubManifestUrl}?t={DateTime.UtcNow.Ticks}";
            var json = await http.GetStringAsync(url);

            var online = JsonSerializer.Deserialize<VersionManifest>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (online != null && !string.IsNullOrWhiteSpace(online.Version))
            {
                // Đảm bảo có downloadUrl hợp lệ
                if (string.IsNullOrWhiteSpace(online.DownloadUrl))
                {
                    online.DownloadUrl =
                        $"https://github.com/john-cpu25/JNNTool/releases/download/v{online.Version}/JNNToolSetup_v{online.Version}.msi";
                }
                LatestManifest = online;
                IsFetchedFromGit = true;
            }
        }
        catch
        {
            // Offline hoặc lỗi mạng - sẽ đọc local manifest bên dưới
        }

        // 2. Nếu chưa lấy được từ Git (offline), đọc từ local manifest cạnh exe
        if (LatestManifest == null)
        {
            LatestManifest = await ReadManifestAsync(ManifestFile);
        }

        // 3. Nếu vẫn null, tạo manifest mặc định v1.0.0
        if (LatestManifest == null)
        {
            LatestManifest = new VersionManifest
            {
                Version = "1.0.0",
                ReleaseDate = "2026-09-13",
                ManifestUrl = DefaultGitHubManifestUrl,
                DownloadUrl = "https://github.com/john-cpu25/JNNTool/releases/download/v1.0.0/JNNToolSetup_v1.0.0.msi",
                Changelog = new List<string>
                {
                    "Hỗ trợ Revit 2022 - 2026",
                    "Cập nhật logo JN mới",
                    "Giao diện Light theme không cuộn"
                }
            };
        }

        // 4. Đọc phiên bản đã cài trên máy
        InstalledManifest = File.Exists(InstalledVersionFile)
            ? await ReadManifestAsync(InstalledVersionFile)
            : null;

        // Nếu thư mục cài đặt tồn tại nhưng chưa có version.json, giả định v1.0.0
        if (InstalledManifest == null && Directory.Exists(InstallRoot))
        {
            InstalledManifest = new VersionManifest
            {
                Version = "1.0.0",
                ReleaseDate = "2026-09-13"
            };
        }
    }

    private static async Task<VersionManifest?> ReadManifestAsync(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<VersionManifest>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }
}
