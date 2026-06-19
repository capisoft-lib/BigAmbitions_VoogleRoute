param(
    [Parameter(Mandatory = $true)]
    [string] $SdkPath,

    [switch] $Force
)

$ErrorActionPreference = "Stop"

$repoRoot = (Get-Item $PSScriptRoot).Parent.FullName
$dest = Join-Path $SdkPath "Assets\Mods\VoogleRoute"

if (-not (Test-Path (Join-Path $SdkPath "Assets\Mods"))) {
    throw "Not a Big Ambitions SDK project: '$SdkPath' (missing Assets\Mods)."
}

if ((Test-Path $dest) -and -not $Force) {
    throw "Destination already exists: $dest. Use -Force to replace."
}

if (Test-Path $dest) {
    Remove-Item $dest -Recurse -Force
}

if (-not (Test-Path (Join-Path $repoRoot "PathFinding\VoogleRoute.Pathfinding.csproj"))) {
    throw @"
PathFinding submodule not initialized in this clone.
From the mod repo root run:
  git submodule update --init --recursive
  .\tools\build-pathfinding.ps1
"@
}

$modItems = @(
    "Scripts", "Locales", "Data", "Dependencies", "Plugins", "PathFinding", "tools", "Editor",
    "ModManifest.asset", "ModManifest.asset.meta",
    "VoogleRoute.asmdef", "VoogleRoute.asmdef.meta",
    "Thumbnail.png", "Thumbnail.png.meta",
    "config.json.example", "config.json.example.meta",
    "Scripts.meta", "Locales.meta", "Data.meta", "Dependencies.meta", "Plugins.meta", "PathFinding.meta"
)

$folderMetaSource = Join-Path (Split-Path $repoRoot -Parent) "VoogleRoute.meta"

New-Item -ItemType Directory -Force -Path $dest | Out-Null
foreach ($item in $modItems) {
    $source = Join-Path $repoRoot $item
    if (-not (Test-Path $source)) { continue }
    Copy-Item $source -Destination (Join-Path $dest $item) -Recurse -Force
}

$modsFolderMetaDest = Join-Path $SdkPath "Assets\Mods\VoogleRoute.meta"
if (Test-Path $folderMetaSource) {
    Copy-Item $folderMetaSource -Destination $modsFolderMetaDest -Force
}

Write-Host "Installed VoogleRoute -> $dest"
Write-Host "Also install LIB_BaPlayerLocation and enable both mods in-game."
Write-Host "Run tools\build-pathfinding.ps1 (syncs PathFinding + LIB_BaUnifiedUI into Dependencies)."
Write-Host "Next: open SDK in Unity 2022.3.62f2, then Mod Builder -> Build & Install."
