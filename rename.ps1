# Script to rename RincoNhan and RincoModeling to JNNTool
$extensions = @(".cs", ".xaml", ".xml", ".wxs", ".ps1", ".md", ".addin")
$excludeFolders = @("obj", "bin", ".vs", ".git")

$files = Get-ChildItem -Path . -Recurse -File | Where-Object { 
    $ext = $_.Extension
    $path = $_.FullName

    $isValidExt = $extensions -contains $ext
    $isExcluded = $false

    foreach ($ex in $excludeFolders) {
        if ($path -match "\\$ex\\") {
            $isExcluded = $true
            break
        }
    }
    
    $isValidExt -and -not $isExcluded
}

foreach ($file in $files) {
    # Skip this script itself
    if ($file.Name -eq "rename.ps1") { continue }

    $content = Get-Content -Path $file.FullName -Raw
    $modified = $false

    if ($content -match "RincoNhan") {
        $content = $content -replace "RincoNhan", "JNNTool"
        $modified = $true
    }
    
    if ($content -match "RincoModeling") {
        $content = $content -replace "RincoModeling", "JNNTool"
        $modified = $true
    }

    if ($content -match "Rinco Modeling") {
        $content = $content -replace "Rinco Modeling", "JNNTool"
        $modified = $true
    }

    if ($modified) {
        Write-Host "Updating $($file.FullName)"
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8
    }
}

Write-Host "Rename completed!" -ForegroundColor Green
