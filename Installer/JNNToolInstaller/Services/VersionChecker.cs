using System.IO;
using System.Text.Json;
using JNNToolInstaller.Models;

namespace JNNToolInstaller.Services;

public class VersionChecker
{
    // Nơi plugin được cài
    private static readonly string InstallRoot =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                     "Autodesk", "ApplicationPlugins", "JNNTool.bundle");

    private static readonly string InstalledVersionFile =
        Path.Combine(InstallRoot, "version.json");

    // version.json cạnh installer exe = phiên bản mới nhất được đóng gói
    private static readonly string ManifestFile =
        Path.Combine(AppContext.BaseDirectory, "version.json");

    public VersionManifest? InstalledManifest { get; private set; }
    public VersionManifest? LatestManifest    { get; private set; }

    public bool IsInstalled => Directory.Exists(InstallRoot) && InstalledManifest != null;
    public bool UpdateAvailable =>
        IsInstalled &&
        LatestManifest != null &&
        LatestManifest.AsVersion() > InstalledManifest!.AsVersion();

    public async Task LoadAsync()
    {
        LatestManifest = await ReadManifestAsync(ManifestFile);

        // Nếu có URL manifest (ví dụ GitHub), ưu tiên lấy online
        if (LatestManifest?.ManifestUrl is { Length: > 0 } url)
        {
            try
            {
                using var http = new System.Net.Http.HttpClient();
                http.Timeout = TimeSpan.FromSeconds(5);
                var json = await http.GetStringAsync(url);
                var online = JsonSerializer.Deserialize<VersionManifest>(json);
                if (online != null) LatestManifest = online;
            }
            catch { /* offline — dùng local manifest */ }
        }

        InstalledManifest = File.Exists(InstalledVersionFile)
            ? await ReadManifestAsync(InstalledVersionFile)
            : null;
    }

    private static async Task<VersionManifest?> ReadManifestAsync(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<VersionManifest>(json);
        }
        catch { return null; }
    }
}
