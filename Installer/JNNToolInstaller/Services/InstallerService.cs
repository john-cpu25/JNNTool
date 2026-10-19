using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using JNNToolInstaller.Models;

namespace JNNToolInstaller.Services;

public class InstallerService
{
    // Nơi plugin được cài (Per-User %APPDATA% ưu tiên, kèm đường dẫn ProgramData cũ để dọn dẹp nếu có)
    private static readonly string InstallRoot =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "Autodesk", "ApplicationPlugins", "JNNTool.bundle");

    private static readonly string LegacyMachineInstallRoot =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                     "Autodesk", "ApplicationPlugins", "JNNTool.bundle");

    public event Action<string>? StatusChanged;
    public event Action<int>?    ProgressChanged;

    // ─── Install / Update ────────────────────────────────────────────────────

    /// <summary>
    /// Download MSI moi tu GitHub roi chay msiexec de cai dat / nang cap.
    /// MSI tu xu ly MajorUpgrade (go ban cu, cai ban moi).
    /// </summary>
    public async Task InstallAsync(VersionManifest manifest, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(manifest.DownloadUrl))
            throw new InvalidOperationException(
                "Khong co downloadUrl trong manifest.\n" +
                "Vui long kiem tra ket noi mang hoac lien he tac gia.");

        // --- Download MSI ---
        StatusChanged?.Invoke("Dang ket noi GitHub...");
        ProgressChanged?.Invoke(0);

        var tempDir = Path.Combine(Path.GetTempPath(), $"JNNTool_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Ten file tu URL (vi du: JNNToolSetup_v1.2.0.msi)
            var fileName = Path.GetFileName(new Uri(manifest.DownloadUrl).LocalPath);
            if (string.IsNullOrWhiteSpace(fileName)) fileName = "JNNToolSetup.msi";
            var msiPath = Path.Combine(tempDir, fileName);

            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromMinutes(10);
            http.DefaultRequestHeaders.Add("User-Agent", "JNNToolUpdater");

            using var response = await http.GetAsync(
                manifest.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            long? length = response.Content.Headers.ContentLength;
            await using var fs  = new FileStream(msiPath, FileMode.Create, FileAccess.Write,
                                                  FileShare.None, 81920, true);
            await using var src = await response.Content.ReadAsStreamAsync(ct);

            var  buffer = new byte[81920];
            long read   = 0;
            int  n;
            while ((n = await src.ReadAsync(buffer, ct)) > 0)
            {
                await fs.WriteAsync(buffer.AsMemory(0, n), ct);
                read += n;
                if (length is > 0)
                {
                    int pct = (int)((double)read / length.Value * 90);
                    ProgressChanged?.Invoke(pct);
                    StatusChanged?.Invoke(
                        $"Dang tai... {read / 1024:N0} KB / {length.Value / 1024:N0} KB");
                }
            }
            await fs.DisposeAsync();

            // --- Chay msiexec (Per-User, khong can UAC runas) ---
            StatusChanged?.Invoke("Dang chay Windows Installer...");
            ProgressChanged?.Invoke(95);

            // /i = install, /qb = basic UI (progress bar), /norestart
            var psi = new ProcessStartInfo
            {
                FileName        = "msiexec.exe",
                Arguments       = $"/i \"{msiPath}\" /qb /norestart",
                UseShellExecute = true
            };

            var proc = Process.Start(psi)
                ?? throw new Exception("Khong the khoi dong Windows Installer.");

            await proc.WaitForExitAsync(ct);

            if (proc.ExitCode != 0 && proc.ExitCode != 1641 && proc.ExitCode != 3010)
                throw new Exception($"Cai dat that bai (exit code {proc.ExitCode}).");

            // Don dep thu muc plugin cu tai ProgramData de Revit khong nap nham ban cu
            if (Directory.Exists(LegacyMachineInstallRoot))
            {
                try
                {
                    StatusChanged?.Invoke("Don dep thu muc plugin cu (ProgramData)...");
                    Directory.Delete(LegacyMachineInstallRoot, recursive: true);
                }
                catch
                {
                    // Bo qua neu khong co quyen xoa truc tiep
                }
            }

            ProgressChanged?.Invoke(100);
            StatusChanged?.Invoke("Hoan tat! Vui long khoi dong lai Revit.");
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Ignore temp cleanup errors
            }
        }
    }

    // ─── Uninstall ───────────────────────────────────────────────────────────

    /// <summary>Go cai dat qua msiexec /x ProductCode hoac xoa thu muc bundle.</summary>
    public Task UninstallAsync(CancellationToken ct = default) => Task.Run(async () =>
    {
        StatusChanged?.Invoke("Dang go cai dat...");
        ProgressChanged?.Invoke(10);

        // Tim ProductCode tu registry (HKCU uu tien, HKLM fallback)
        var productCode = FindProductCode();
        if (productCode != null)
        {
            StatusChanged?.Invoke("Go cai dat qua Windows Installer...");
            var psi = new ProcessStartInfo
            {
                FileName        = "msiexec.exe",
                Arguments       = $"/x {productCode} /qb /norestart",
                UseShellExecute = true
            };
            var proc = Process.Start(psi);
            if (proc != null) await proc.WaitForExitAsync(ct);
        }

        if (Directory.Exists(InstallRoot))
        {
            StatusChanged?.Invoke("Xoa thu muc plugin (%APPDATA%)...");
            Directory.Delete(InstallRoot, recursive: true);
        }

        if (Directory.Exists(LegacyMachineInstallRoot))
        {
            try
            {
                StatusChanged?.Invoke("Xoa thu muc plugin cu (ProgramData)...");
                Directory.Delete(LegacyMachineInstallRoot, recursive: true);
            }
            catch
            {
                // Bo qua neu khong co quyen xoa ProgramData
            }
        }

        ProgressChanged?.Invoke(100);
        StatusChanged?.Invoke("Da go cai dat xong.");
    }, ct);

    // ─── Helpers ─────────────────────────────────────────────────────────────

    public bool IsInstalled => Directory.Exists(InstallRoot) || Directory.Exists(LegacyMachineInstallRoot);

    public List<string> GetInstalledRevitVersions()
    {
        var result = new List<string>();
        foreach (var year in new[] { "2022", "2023", "2024", "2025", "2026", "2027" })
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Autodesk", $"Revit {year}");
            if (Directory.Exists(path)) result.Add(year);
        }
        return result;
    }

    /// <summary>Tim ProductCode cua JNNTool trong registry (HKCU va HKLM) de go dung MSI.</summary>
    private static string? FindProductCode()
    {
        try
        {
            // 1. Kiem tra key HKCU\Software\JNN\JNNTool do Package.wxs ghi
            using (var jnnKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\JNN\JNNTool"))
            {
                var code = jnnKey?.GetValue("ProductCode") as string;
                if (!string.IsNullOrWhiteSpace(code))
                    return code;
            }

            // 2. Kiem tra HKCU Uninstall (Per-User MSI)
            using (var cuKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
            {
                if (cuKey != null)
                {
                    foreach (var sub in cuKey.GetSubKeyNames())
                    {
                        using var subKey = cuKey.OpenSubKey(sub);
                        var name = subKey?.GetValue("DisplayName") as string;
                        if (name != null && name.Contains("JNNTool", StringComparison.OrdinalIgnoreCase))
                            return sub;
                    }
                }
            }

            // 3. Fallback HKLM Uninstall (Per-Machine MSI cu)
            using (var lmKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
            {
                if (lmKey != null)
                {
                    foreach (var sub in lmKey.GetSubKeyNames())
                    {
                        using var subKey = lmKey.OpenSubKey(sub);
                        var name = subKey?.GetValue("DisplayName") as string;
                        if (name != null && name.Contains("JNNTool", StringComparison.OrdinalIgnoreCase))
                            return sub;
                    }
                }
            }
        }
        catch { }
        return null;
    }
}
