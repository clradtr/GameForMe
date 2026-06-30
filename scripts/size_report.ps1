param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [int]$TopFiles = 50,
    [int]$TopFolders = 30
)

$ErrorActionPreference = "SilentlyContinue"

function Format-Size {
    param([Nullable[Int64]]$Bytes)

    if ($null -eq $Bytes) {
        return "0 B"
    }

    if ($Bytes -ge 1GB) {
        return "{0:N2} GiB" -f ($Bytes / 1GB)
    }

    if ($Bytes -ge 1MB) {
        return "{0:N2} MiB" -f ($Bytes / 1MB)
    }

    if ($Bytes -ge 1KB) {
        return "{0:N2} KiB" -f ($Bytes / 1KB)
    }

    return "$Bytes B"
}

function Get-FolderBytes {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return 0
    }

    $sum = (Get-ChildItem -LiteralPath $Path -Recurse -Force -File -ErrorAction SilentlyContinue |
        Measure-Object Length -Sum).Sum
    if ($null -eq $sum) {
        return 0
    }

    return [Int64]$sum
}

$Root = (Resolve-Path $Root).Path
$files = Get-ChildItem -LiteralPath $Root -Recurse -Force -File -ErrorAction SilentlyContinue
$totalBytes = ($files | Measure-Object Length -Sum).Sum
if ($null -eq $totalBytes) {
    $totalBytes = 0
}

Write-Host "# Size Report"
Write-Host ""
Write-Host "Root: $Root"
Write-Host "Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host "Total: $(Format-Size $totalBytes) ($totalBytes bytes)"
Write-Host "Files: $($files.Count)"
Write-Host ""

$selectedFolders = @(
    "Assets",
    "Assets/Art",
    "Assets/Art/Generated",
    "Assets/Art/Generated/_Source",
    "Packages",
    "ProjectSettings",
    "Library",
    "Builds",
    "Logs",
    "UserSettings"
)

Write-Host "## Selected Folders"
Write-Host ""
Write-Host "| Path | Size |"
Write-Host "|---|---:|"
foreach ($folder in $selectedFolders) {
    $path = Join-Path $Root $folder
    $bytes = Get-FolderBytes $path
    Write-Host "| ``$folder`` | $(Format-Size $bytes) |"
}

Write-Host ""
Write-Host "## Top $TopFiles Files"
Write-Host ""
Write-Host "| Path | Size |"
Write-Host "|---|---:|"
$files |
    Sort-Object Length -Descending |
    Select-Object -First $TopFiles |
    ForEach-Object {
        $relative = $_.FullName.Substring($Root.Length + 1)
        Write-Host "| ``$relative`` | $(Format-Size $_.Length) |"
    }

Write-Host ""
Write-Host "## Top $TopFolders Folders"
Write-Host ""
Write-Host "| Path | Size | Files |"
Write-Host "|---|---:|---:|"
$sizeMap = @{}
$countMap = @{}
foreach ($file in $files) {
    $dir = $file.DirectoryName
    while ($dir -and $dir.StartsWith($Root)) {
        if (-not $sizeMap.ContainsKey($dir)) {
            $sizeMap[$dir] = [Int64]0
            $countMap[$dir] = 0
        }

        $sizeMap[$dir] = [Int64]$sizeMap[$dir] + [Int64]$file.Length
        $countMap[$dir] = [int]$countMap[$dir] + 1

        if ($dir -eq $Root) {
            break
        }

        $dir = Split-Path -Parent $dir
    }
}

$sizeMap.GetEnumerator() |
    Where-Object { $_.Key -ne $Root } |
    Sort-Object Value -Descending |
    Select-Object -First $TopFolders |
    ForEach-Object {
        $relative = $_.Key.Substring($Root.Length + 1)
        Write-Host "| ``$relative`` | $(Format-Size $_.Value) | $($countMap[$_.Key]) |"
    }

