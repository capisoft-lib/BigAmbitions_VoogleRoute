param(
    [string] $ProjectRoot,
    [string] $ModsLocalRoot,
    [string] $BackupRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$modId = "VoogleRoute"
$repoRoot = (Get-Item -LiteralPath $PSScriptRoot).Parent.FullName

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "..\..\.."))
}
if ([string]::IsNullOrWhiteSpace($ModsLocalRoot)) {
    $ModsLocalRoot = Join-Path $env:USERPROFILE "AppData\LocalLow\Hovgaard Games\Big Ambitions\ModsLocal"
}
if ([string]::IsNullOrWhiteSpace($BackupRoot)) {
    $BackupRoot = Join-Path $env:LOCALAPPDATA "Capisoft\BigAmbitions\WorkshopBackups"
}

$projectRootFull = [System.IO.Path]::GetFullPath($ProjectRoot)
$modsLocalRootFull = [System.IO.Path]::GetFullPath($ModsLocalRoot)
$backupRootFull = [System.IO.Path]::GetFullPath($BackupRoot)
$outputRootFull = [System.IO.Path]::GetFullPath((Join-Path $projectRootFull "Output"))
$sourceFull = [System.IO.Path]::GetFullPath((Join-Path $outputRootFull $modId))
$targetFull = [System.IO.Path]::GetFullPath((Join-Path $modsLocalRootFull $modId))

function Assert-ChildPath {
    param(
        [Parameter(Mandatory = $true)][string] $Path,
        [Parameter(Mandatory = $true)][string] $Parent,
        [Parameter(Mandatory = $true)][string] $Label
    )

    $parentPrefix = $Parent.TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $Path.StartsWith($parentPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label must remain inside '$Parent'; resolved '$Path'."
    }
}

function Get-PackageManifest {
    param([Parameter(Mandatory = $true)][string] $Root)

    $rootPrefix = $Root.TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    return @(
        Get-ChildItem -LiteralPath $Root -File -Recurse |
            Sort-Object FullName |
            ForEach-Object {
                [pscustomobject]@{
                    RelativePath = $_.FullName.Substring($rootPrefix.Length).Replace('\', '/')
                    Length = [long]$_.Length
                    Sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
                }
            }
    )
}

function Get-ManifestFingerprint {
    param([Parameter(Mandatory = $true)][object[]] $Manifest)

    $body = ($Manifest | ForEach-Object {
        "$($_.RelativePath)|$($_.Length)|$($_.Sha256)"
    }) -join "`n"
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($body)
        return [System.BitConverter]::ToString($sha.ComputeHash($bytes)).Replace("-", "")
    }
    finally {
        $sha.Dispose()
    }
}

Assert-ChildPath -Path $sourceFull -Parent $outputRootFull -Label "Workshop source"
Assert-ChildPath -Path $targetFull -Parent $modsLocalRootFull -Label "Workshop target"
if ((Split-Path -Leaf $targetFull) -ne $modId) {
    throw "Unexpected Workshop target '$targetFull'."
}
if (-not (Test-Path -LiteralPath $sourceFull -PathType Container)) {
    throw "Official VoogleRoute output is missing: '$sourceFull'."
}

$runningGame = @(Get-Process -Name "Big Ambitions" -ErrorAction SilentlyContinue)
if ($runningGame.Count -gt 0) {
    throw "Close Big Ambitions before preparing the Workshop folder."
}

$requiredFiles = @(
    "VoogleRoute.dll",
    "Data\big_ambitions_enhanced_routes.csv",
    "Data\hamptons_house_navigation_maps.json",
    "Data\subway_stations.csv",
    "Dependencies\VoogleRoute.Pathfinding.dll",
    "Locales\en.json",
    "Thumbnail.png"
)
$missingFiles = @($requiredFiles | Where-Object {
    -not (Test-Path -LiteralPath (Join-Path $sourceFull $_) -PathType Leaf)
})
if ($missingFiles.Count -gt 0) {
    throw "Official output is incomplete. Missing: $($missingFiles -join ', ')."
}

$subwayValidator = Join-Path $repoRoot "tools\validate-subway-stations.ps1"
$subwayValidation = & $subwayValidator -Path (Join-Path $sourceFull "Data\subway_stations.csv")
Write-Host "[verify] Packaged subway fallback: $($subwayValidation.StationCount) stations, SHA-256 $($subwayValidation.Sha256)"

$releaseDllPath = Join-Path $sourceFull "VoogleRoute.dll"
$releaseDllBytes = [System.IO.File]::ReadAllBytes($releaseDllPath)
$releaseDllAscii = [System.Text.Encoding]::ASCII.GetString($releaseDllBytes)
$releaseDllUnicode = [System.Text.Encoding]::Unicode.GetString($releaseDllBytes)
$forbiddenExporterMarkers = @(
    "VoogleRouteSubwayExporter",
    "VoogleRouteDevExports",
    "Exported subway stations"
)
$foundExporterMarkers = @($forbiddenExporterMarkers | Where-Object {
    $releaseDllAscii.Contains($_) -or $releaseDllUnicode.Contains($_)
})
if ($foundExporterMarkers.Count -gt 0) {
    throw "Release DLL contains developer subway exporter markers: $($foundExporterMarkers -join ', ')."
}

$sourceManifest = Get-PackageManifest -Root $sourceFull
$runtimeArtifacts = @($sourceManifest | Where-Object {
    $_.RelativePath -in @("config.json", "bookmarks.json", "line_color.txt") -or
    $_.RelativePath -like "Logs/*" -or
    $_.RelativePath -like "WaypointDumps/*" -or
    $_.RelativePath -like "*.log" -or
    $_.RelativePath -like "*waypoints_all_*.csv" -or
    $_.RelativePath -like "*_dump.csv"
})
if ($runtimeArtifacts.Count -gt 0) {
    throw "Official output contains runtime artifacts: $($runtimeArtifacts.RelativePath -join ', ')."
}

$forbiddenDependencies = @($sourceManifest | Where-Object {
    $_.RelativePath -match '^Dependencies/(LIB_BaUnifiedUI|LIB_BaPlayerLocation).*\.dll$' -or
    $_.RelativePath -match '^Dependencies/(Microsoft\.Bcl\.AsyncInterfaces|System\.(Buffers|Memory|Numerics\.Vectors|Runtime\.CompilerServices\.Unsafe|Text\.Encodings\.Web|Text\.Json|Threading\.Tasks\.Extensions))\.dll$'
})
if ($forbiddenDependencies.Count -gt 0) {
    throw "Official output contains forbidden dependencies: $($forbiddenDependencies.RelativePath -join ', ')."
}

$dependencyFiles = @($sourceManifest | Where-Object { $_.RelativePath -like "Dependencies/*.dll" })
if ($dependencyFiles.Count -ne 1 -or $dependencyFiles[0].RelativePath -ne "Dependencies/VoogleRoute.Pathfinding.dll") {
    throw "VoogleRoute must package exactly one dependency: Dependencies/VoogleRoute.Pathfinding.dll."
}

$backupModFull = $null
$targetMoved = $false
try {
    if (Test-Path -LiteralPath $targetFull) {
        New-Item -ItemType Directory -Path $backupRootFull -Force | Out-Null
        $backupDir = Join-Path $backupRootFull ("VoogleRoute-" + (Get-Date -Format "yyyyMMdd-HHmmss-fff"))
        New-Item -ItemType Directory -Path $backupDir | Out-Null
        $backupModFull = Join-Path $backupDir $modId
        Move-Item -LiteralPath $targetFull -Destination $backupModFull
        $targetMoved = $true
    }

    Copy-Item -LiteralPath $sourceFull -Destination $targetFull -Recurse

    $targetManifest = Get-PackageManifest -Root $targetFull
    $sourceJson = $sourceManifest | ConvertTo-Json -Depth 4 -Compress
    $targetJson = $targetManifest | ConvertTo-Json -Depth 4 -Compress
    if ($sourceJson -ne $targetJson) {
        throw "Prepared Workshop folder differs from official output."
    }
}
catch {
    if (Test-Path -LiteralPath $targetFull) {
        Remove-Item -LiteralPath $targetFull -Recurse -Force
    }
    if ($targetMoved -and $backupModFull -and (Test-Path -LiteralPath $backupModFull)) {
        Move-Item -LiteralPath $backupModFull -Destination $targetFull
    }
    throw
}

$contentBytes = [long](($sourceManifest | Measure-Object Length -Sum).Sum)
$previewBytes = [long](($sourceManifest | Where-Object RelativePath -eq "Thumbnail.png" | Measure-Object Length -Sum).Sum)
$dllPath = Join-Path $targetFull "VoogleRoute.dll"
$result = [pscustomobject]@{
    ModId = $modId
    Source = $sourceFull
    PreparedFolder = $targetFull
    RuntimeBackup = if ($backupModFull) { $backupModFull } else { "" }
    FileCount = $sourceManifest.Count
    PackageBytes = $contentBytes
    SteamContentBytesWithoutPreview = $contentBytes - $previewBytes
    DllSha256 = (Get-FileHash -LiteralPath $dllPath -Algorithm SHA256).Hash
    SubwayCsvSha256 = $subwayValidation.Sha256
    SubwayStationCount = $subwayValidation.StationCount
    PackageFingerprint = Get-ManifestFingerprint -Manifest $sourceManifest
    ReadyForWorkshop = $true
}

$result | ConvertTo-Json -Depth 4
