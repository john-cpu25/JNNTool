using System.Text.Json.Serialization;

namespace JNNToolInstaller.Models;

public class VersionManifest
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "0.0.0";

    [JsonPropertyName("releaseDate")]
    public string ReleaseDate { get; set; } = "";

    [JsonPropertyName("manifestUrl")]
    public string ManifestUrl { get; set; } = "";

    /// <summary>Link tải file ZIP trực tiếp từ GitHub Releases.</summary>
    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; set; } = "";

    [JsonPropertyName("changelog")]
    public List<string> Changelog { get; set; } = new();

    public Version AsVersion() =>
        System.Version.TryParse(Version, out var v) ? v : new Version(0, 0, 0);
}
