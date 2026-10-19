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

    // Nơi plugin được cài (Per-User %APPDATA% ưu tiên, fallback ProgramData cho bản cũ)
    private static readonly string PerUserInstallRoot =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "Autodesk", "ApplicationPlugins", "JNNTool.bundle");

    private static readonly string LegacyMachineInstallRoot =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                     "Autodesk", "ApplicationPlugins", "JNNTool.bundle");

    private static string ActiveInstallRoot =>
        Directory.Exists(PerUserInstallRoot) ? PerUserInstallRoot :
        Directory.Exists(LegacyMachineInstallRoot) ? LegacyMachineInstallRoot :
        PerUserInstallRoot;

    // version.json cạnh installer exe
    private static readonly string ManifestFile =
        Path.Combine(AppContext.BaseDirectory, "version.json");

    public const string GitHubReleasesApiUrl =
        "https://api.github.com/repos/john-cpu25/JNNTool/releases";

    public VersionManifest? InstalledManifest { get; private set; }
    public VersionManifest? LatestManifest    { get; private set; }
    public List<GitHubReleaseItem> AvailableReleases { get; private set; } = new();
    public bool IsFetchedFromGit              { get; private set; }

    public bool IsInstalled => Directory.Exists(PerUserInstallRoot) || Directory.Exists(LegacyMachineInstallRoot);

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
        AvailableReleases.Clear();

        // 1. Luôn ưu tiên lấy bản mới nhất trực tiếp từ GitHub Git (version.json)
        try
        {
            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(8);
            http.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
            http.DefaultRequestHeaders.Add("User-Agent", "JNNToolUpdater");

            // Cache buster tránh bị GitHub CDN lưu đệm
            var url = $"{DefaultGitHubManifestUrl}?t={DateTime.UtcNow.Ticks}";
            var json = await http.GetStringAsync(url);

            var online = JsonSerializer.Deserialize<VersionManifest>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (online != null && !string.IsNullOrWhiteSpace(online.Version))
            {
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

        // 1.2 Lấy danh sách toàn bộ releases từ GitHub API để cho phép chọn bản (Rollback/Downgrade)
        try
        {
            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(8);
            http.DefaultRequestHeaders.Add("User-Agent", "JNNToolUpdater");

            var relJson = await http.GetStringAsync(GitHubReleasesApiUrl);
            var dtoList = JsonSerializer.Deserialize<List<GitHubReleaseDto>>(relJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (dtoList != null && dtoList.Count > 0)
            {
                bool first = true;
                foreach (var dto in dtoList)
                {
                    if (string.IsNullOrWhiteSpace(dto.TagName)) continue;

                    var item = new GitHubReleaseItem
                    {
                        TagName = dto.TagName,
                        Name = string.IsNullOrWhiteSpace(dto.Name) ? dto.TagName : dto.Name,
                        Body = dto.Body ?? "",
                        IsLatest = first
                    };

                    var msiAsset = dto.Assets?.FirstOrDefault(a =>
                        a.Name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase));
                    if (msiAsset != null && !string.IsNullOrWhiteSpace(msiAsset.BrowserDownloadUrl))
                    {
                        item.DownloadUrl = msiAsset.BrowserDownloadUrl;
                    }
                    else
                    {
                        item.DownloadUrl =
                            $"https://github.com/john-cpu25/JNNTool/releases/download/{dto.TagName}/JNNToolSetup_{dto.TagName}.msi";
                    }

                    if (!string.IsNullOrWhiteSpace(dto.Body))
                    {
                        var lines = dto.Body.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            var trimmed = line.Trim();
                            if (string.IsNullOrWhiteSpace(trimmed)) continue;
                            if (trimmed.StartsWith("#") || trimmed.StartsWith("---")) continue;
                            if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
                            {
                                item.Changelog.Add(trimmed.Substring(2).Trim());
                            }
                            else
                            {
                                item.Changelog.Add(trimmed);
                            }
                        }
                    }

                    if (item.Changelog.Count == 0)
                    {
                        item.Changelog.Add($"Bản phát hành {item.TagName}");
                    }

                    AvailableReleases.Add(item);
                    first = false;
                }
            }
        }
        catch
        {
            // Offline hoặc rate limit API
        }

        // 2. Nếu chưa lấy được từ Git (offline), đọc từ local manifest cạnh exe
        if (LatestManifest == null)
        {
            LatestManifest = await ReadManifestAsync(ManifestFile);
        }

        // 3. Nếu vẫn null, tạo manifest mặc định
        if (LatestManifest == null)
        {
            LatestManifest = new VersionManifest
            {
                Version = "2.1.3",
                ReleaseDate = "2026-10-19",
                ManifestUrl = DefaultGitHubManifestUrl,
                DownloadUrl = "https://github.com/john-cpu25/JNNTool/releases/download/v2.1.3/JNNToolSetup_v2.1.3.msi",
                Changelog = new List<string>
                {
                    "Hỗ trợ Revit 2022 - 2027",
                    "Cài đặt Per-User (%APPDATA%) không cần quyền Admin"
                }
            };
        }

        // Nếu AvailableReleases chưa có, tạo ít nhất 1 item từ LatestManifest
        if (AvailableReleases.Count == 0)
        {
            AvailableReleases.Add(new GitHubReleaseItem
            {
                TagName = $"v{LatestManifest.Version}",
                Name = $"JNNTool v{LatestManifest.Version}",
                Body = string.Join("\n", LatestManifest.Changelog),
                DownloadUrl = LatestManifest.DownloadUrl,
                IsLatest = true,
                Changelog = new List<string>(LatestManifest.Changelog)
            });
        }

        // 4. Đọc phiên bản đã cài trên máy
        var installedVersionFile = Path.Combine(ActiveInstallRoot, "version.json");
        InstalledManifest = File.Exists(installedVersionFile)
            ? await ReadManifestAsync(installedVersionFile)
            : null;

        // Nếu thư mục cài đặt tồn tại nhưng chưa có version.json, giả định v1.0.0
        if (InstalledManifest == null && IsInstalled)
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
