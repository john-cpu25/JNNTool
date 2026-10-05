# ==============================================================================
# JNNTool - Automated Release & Deploy to GitHub Script
# Cach dung:
#   .\Installer\ReleaseToGitHub.ps1 -Version "2.0.3" -Changelog "Noi dung cap nhat..."
# ==============================================================================
param(
    [Parameter(Mandatory=$true)]
    [string]$Version,

    [Parameter(Mandatory=$false)]
    [string]$Changelog = "Cap nhat va toi uu tinh nang moi cho JNNTool"
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$InstallerDir = Join-Path $ProjectRoot "Installer"
$BundleDir = Join-Path $ProjectRoot "JNNTool.bundle"
$MsiName = "JNNToolSetup_v$Version.msi"
$MsiPath = Join-Path $InstallerDir $MsiName
$Today = Get-Date -Format "yyyy-MM-dd"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   BAT DAU TU DONG DONG GOI VA RELEASE JNNTOOL v$Version   " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Cap nhat 4 file version.json
Write-Host "`n1. Cap nhat cac file version.json..." -ForegroundColor Yellow
$jsonObj = [ordered]@{
    version     = $Version
    releaseDate = $Today
    manifestUrl = "https://raw.githubusercontent.com/john-cpu25/JNNTool/main/version.json"
    downloadUrl = "https://github.com/john-cpu25/JNNTool/releases/download/v$Version/$MsiName"
    changelog   = @($Changelog -split "\r?\n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}
$jsonText = $jsonObj | ConvertTo-Json -Depth 5

$filesToUpdate = @(
    (Join-Path $ProjectRoot "version.json"),
    (Join-Path $BundleDir "version.json"),
    (Join-Path $InstallerDir "JNNToolInstaller\version.json"),
    (Join-Path $InstallerDir "publish\version.json")
)

foreach ($f in $filesToUpdate) {
    if (Test-Path (Split-Path $f -Parent)) {
        [System.IO.File]::WriteAllText($f, $jsonText, (New-Object System.Text.UTF8Encoding($false)))
        Write-Host "   -> Da cap nhat: $f" -ForegroundColor Gray
    }
}

# 2. Publish JNNToolInstaller
Write-Host "`n2. Publish JNNToolInstaller (JNN Updater)..." -ForegroundColor Yellow
dotnet publish "$InstallerDir\JNNToolInstaller\JNNToolInstaller.csproj" -c Release -o "$InstallerDir\publish"
if ($LASTEXITCODE -ne 0) { Write-Error "Publish JNNToolInstaller that bai!"; exit 1 }

# 3. Build Plugin & WiX MSI Installer
Write-Host "`n3. Build Plugin va tao file MSI qua BuildInstaller.ps1..." -ForegroundColor Yellow
& "$InstallerDir\BuildInstaller.ps1" -Version $Version
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $MsiPath)) {
    Write-Error "Build MSI that bai hoac khong tim thay file $MsiPath"
    exit 1
}

# 4. Git Commit, Tag & Push
Write-Host "`n4. Git Commit, Tag va Push len GitHub..." -ForegroundColor Yellow
Set-Location $ProjectRoot
git add -A
$status = git status --porcelain
if ($status) {
    git commit -m "Release v${Version}: $Changelog"
}
# Tao hoac cap nhat Tag
if (& git tag -l "v$Version") {
    Write-Host "   Tag v$Version da ton tai, cap nhat tag..." -ForegroundColor Gray
    git tag -d "v$Version"
}
git tag -a "v$Version" -m "Release v${Version}: $Changelog"
git push origin main
git push origin "v$Version" --force
if ($LASTEXITCODE -ne 0) { Write-Error "Git push that bai!"; exit 1 }

# 5. Lay Token tu Windows Credential Manager
Write-Host "`n5. Lay GitHub Token tu Windows Credential Vault..." -ForegroundColor Yellow
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Text;

public class CredReader {
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credentialPtr);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern void CredFree(IntPtr credentialPtr);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL {
        public int Flags;
        public int Type;
        public string TargetName;
        public string Comment;
        public long LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public int Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }

    public static string GetPassword(string target) {
        IntPtr credPtr;
        if (CredRead(target, 1, 0, out credPtr)) {
            var cred = (CREDENTIAL)Marshal.PtrToStructure(credPtr, typeof(CREDENTIAL));
            byte[] bytes = new byte[cred.CredentialBlobSize];
            Marshal.Copy(cred.CredentialBlob, bytes, 0, cred.CredentialBlobSize);
            CredFree(credPtr);
            string s = Encoding.UTF8.GetString(bytes);
            if (s.StartsWith("gho_") || s.StartsWith("ghp_") || s.StartsWith("github_pat_")) return s;
            return Encoding.Unicode.GetString(bytes);
        }
        return null;
    }
}
'@

$token = [CredReader]::GetPassword('git:https://github.com')
if ([string]::IsNullOrWhiteSpace($token)) {
    Write-Warning "Khong tim thay token trong Windows Credential Manager. Vui long tao Release va upload thu cong."
    exit 0
}

# 6. Tao GitHub Release & Upload MSI
Write-Host "`n6. Tao GitHub Release v$Version va Upload $MsiName..." -ForegroundColor Yellow
$headers = @{
    "Authorization" = "Bearer $token"
    "User-Agent"    = "JNNToolAutoDeploy"
    "Accept"        = "application/vnd.github+json"
}

# Kiem tra release
$release = $null
try {
    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/john-cpu25/JNNTool/releases/tags/v$Version" -Headers $headers -Method Get
    Write-Host "   Release v$Version da ton tai tren GitHub." -ForegroundColor Gray
} catch {
    Write-Host "   Tao moi Release v$Version tren GitHub..." -ForegroundColor Gray
    $releaseBody = @{
        tag_name         = "v$Version"
        target_commitish = "main"
        name             = "Release v${Version}: $Changelog"
        body             = "## JNNTool v$Version ($Today)`n`n### Changelog:`n$Changelog"
        draft            = $false
        prerelease       = $false
    }
    $bodyBytes = [System.Text.Encoding]::UTF8.GetBytes(($releaseBody | ConvertTo-Json))
    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/john-cpu25/JNNTool/releases" -Headers $headers -Method Post -Body $bodyBytes -ContentType "application/json"
}

# Upload Asset MSI
Write-Host "   Upload asset $MsiName len Release..." -ForegroundColor Gray
if ($release.assets) {
    $existing = $release.assets | Where-Object { $_.name -eq $MsiName }
    if ($existing) {
        Invoke-RestMethod -Uri "https://api.github.com/repos/john-cpu25/JNNTool/releases/assets/$($existing.id)" -Headers $headers -Method Delete
    }
}

$uploadUrl = $release.upload_url -replace '\{\?name,label\}', "?name=$MsiName"
$uploadHeaders = @{
    "Authorization" = "Bearer $token"
    "User-Agent"    = "JNNToolAutoDeploy"
    "Content-Type"  = "application/octet-stream"
}
$msiBytes = [System.IO.File]::ReadAllBytes((Resolve-Path $MsiPath))
$uploadResult = Invoke-RestMethod -Uri $uploadUrl -Headers $uploadHeaders -Method Post -Body $msiBytes

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "   RELEASE THANH CONG v$Version LEN GITHUB!" -ForegroundColor Green
Write-Host "   MSI Download: $($uploadResult.browser_download_url)" -ForegroundColor Green
Write-Host "   JNN Updater da co the tu dong cap nhat ban moi." -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
