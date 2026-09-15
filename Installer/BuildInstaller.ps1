# Script build plugin va tao MSI installer cho JNNTool
# Cach dung: cd Installer\ ; .\BuildInstaller.ps1 [-Version "1.2.0"]
param(
    [string]$Version = ""   # Neu de trong se doc tu version.json
)

$ProjectRoot   = Resolve-Path ".."
$InstallerDir  = Get-Location
$BundleDir     = Join-Path $ProjectRoot "JNNTool.bundle"
$VersionJson   = Join-Path $BundleDir "version.json"

Write-Host "--- Bat dau dong goi JNNTool MSI ---" -ForegroundColor Cyan

# 0. Doc version tu version.json neu khong truyen tham so
if ([string]::IsNullOrWhiteSpace($Version)) {
    $versionData = Get-Content $VersionJson | ConvertFrom-Json
    $Version     = $versionData.version
}
Write-Host "   Phien ban: v$Version" -ForegroundColor White

# Canh bao neu chua dien manifestUrl
$manifestCheck = Get-Content $VersionJson | ConvertFrom-Json
if ($manifestCheck.manifestUrl -like "*YOUR_USERNAME*") {
    Write-Warning "version.json van con YOUR_USERNAME. Cap nhat manifestUrl truoc khi release!"
}

# 1. Build plugin C# (net48 + net8)
Write-Host ""
Write-Host "1. Build plugin C#..." -ForegroundColor Yellow
dotnet build "$ProjectRoot\JNNTool.csproj" -c Release
if ($LASTEXITCODE -ne 0) { Write-Error "Build plugin that bai!"; exit 1 }

# 2. Kiem tra bundle
if (-not (Test-Path $BundleDir)) {
    Write-Error "Khong tim thay JNNTool.bundle tai $BundleDir"
    exit 1
}

# 3. Cap nhat version trong Package.wxs
Write-Host ""
Write-Host "2. Cap nhat version trong Package.wxs..." -ForegroundColor Yellow
$wxsPath = Join-Path $InstallerDir "Package.wxs"
$wxsContent = Get-Content $wxsPath -Raw
$wxsContent = $wxsContent -replace 'Version="[\d\.]+"', "Version=""$Version"""
Set-Content $wxsPath $wxsContent -Encoding UTF8
Write-Host "   Version da cap nhat thanh $Version" -ForegroundColor Gray

# 4. Build MSI voi WiX
Write-Host ""
Write-Host "3. Build MSI voi WiX..." -ForegroundColor Yellow
$MsiName = "JNNToolSetup_v$Version.msi"
$MsiPath = Join-Path $InstallerDir $MsiName

# Xoa file cu neu co
if (Test-Path $MsiPath) { Remove-Item $MsiPath }

wix build "$wxsPath" -ext WixToolset.UI.wixext/4.0.5 -o "$MsiPath"
if ($LASTEXITCODE -ne 0) { Write-Error "Build MSI that bai!"; exit 1 }

# 5. Ket qua
$msiSize = [math]::Round((Get-Item $MsiPath).Length / 1MB, 2)

Write-Host ""
Write-Host "=== THANH CONG ===" -ForegroundColor Green
Write-Host "File MSI     : $MsiPath" -ForegroundColor Green
Write-Host "Kich thuoc   : $msiSize MB" -ForegroundColor Green
Write-Host ""
Write-Host "Buoc tiep theo de release len GitHub:" -ForegroundColor Cyan
Write-Host "  1. git add . && git commit -m 'Release v$Version'"
Write-Host "  2. git tag v$Version && git push && git push --tags"
Write-Host "  3. Tao GitHub Release moi voi tag v$Version"
Write-Host "  4. Upload $MsiName len Release assets"
Write-Host "  5. Cap nhat downloadUrl trong Installer\JNNToolInstaller\version.json:"
Write-Host "     https://github.com/john-cpu25/JNNTool/releases/download/v$Version/$MsiName"
Write-Host "  6. git add . && git commit -m 'Update manifest v$Version' && git push"
