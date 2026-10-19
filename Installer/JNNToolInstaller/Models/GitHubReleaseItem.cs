using System.Text.Json.Serialization;

namespace JNNToolInstaller.Models;

public class GitHubReleaseItem
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = "";

    public string Version => TagName.TrimStart('v', 'V');

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("body")]
    public string Body { get; set; } = "";

    public string DownloadUrl { get; set; } = "";
    public bool IsLatest { get; set; }
    public List<string> Changelog { get; set; } = new();

    public string DisplayText => GetDisplayText(JNNToolInstaller.Services.LocalizationService.IsEnglish);

    public string GetDisplayText(bool isEnglish)
    {
        if (IsLatest)
        {
            return isEnglish ? $"v{Version} (Latest)" : $"v{Version} (Mới nhất)";
        }
        return $"v{Version}";
    }

    public Version AsVersion() =>
        System.Version.TryParse(Version, out var v) ? v : new Version(0, 0, 0);

    public override string ToString() => DisplayText;
}

public class GitHubAssetItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }
}

public class GitHubReleaseDto
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("body")]
    public string Body { get; set; } = "";

    [JsonPropertyName("assets")]
    public List<GitHubAssetItem> Assets { get; set; } = new();
}
