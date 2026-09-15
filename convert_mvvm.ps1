# Script to convert [ObservableProperty] to manual properties
# and [RelayCommand] to manual ICommand in C# files

param(
    [string]$FilePath
)

$content = Get-Content $FilePath -Raw -Encoding utf8

# Remove 'partial' from class declaration (no longer needed)
$content = $content -replace 'public partial class (\w+)', 'public class $1'

# Convert [ObservableProperty] fields to manual properties
# Pattern: [ObservableProperty]\r\n        private TYPE _fieldName = DEFAULT;
# or:     [ObservableProperty]\r\n        private TYPE _fieldName;

$regex = [regex]'(?m)\s*\[ObservableProperty\]\s*\r?\n(\s+)private\s+(.+?)\s+_(\w+?)(\s*=\s*[^;]+)?;'

$content = $regex.Replace($content, {
    param($match)
    $indent = $match.Groups[1].Value
    $type = $match.Groups[2].Value
    $fieldName = $match.Groups[3].Value
    $default = $match.Groups[4].Value
    
    # Convert first char to uppercase for property name
    $propName = $fieldName.Substring(0,1).ToUpper() + $fieldName.Substring(1)
    
    $result = @"

${indent}private $type _$fieldName$default;
${indent}public $type $propName
${indent}{
${indent}    get => _$fieldName;
${indent}    set => SetProperty(ref _$fieldName, value);
${indent}}
"@
    return $result
})

# Convert [RelayCommand] void methods to ICommand with RelayCommand
$regexCmd = [regex]'(?m)\s*\[RelayCommand\]\s*\r?\n(\s+)private\s+void\s+(\w+)\((.*?)\)'
$content = $regexCmd.Replace($content, {
    param($match)
    $indent = $match.Groups[1].Value
    $methodName = $match.Groups[2].Value
    $params = $match.Groups[3].Value
    
    # Just remove the attribute, keep the method
    return "`n${indent}private void $methodName($params)"
})

Set-Content $FilePath $content -Encoding utf8 -NoNewline
Write-Output "Converted: $FilePath"
