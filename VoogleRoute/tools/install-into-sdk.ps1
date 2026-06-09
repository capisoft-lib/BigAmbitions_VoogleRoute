param(
    [Parameter(Mandatory = $true)]
    [string] $SdkPath,

    [switch] $Force
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
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
  .\VoogleRoute\tools\build-pathfinding.ps1
"@
}

$exclude = @(".git", "tools", "Output", "bin", "obj")
Get-ChildItem $repoRoot -Force | Where-Object { $exclude -notcontains $_.Name } | ForEach-Object {
    Copy-Item $_.FullName -Destination $dest -Recurse -Force
}

Write-Host "Installed VoogleRoute -> $dest"
Write-Host "Also install LIB_BaPlayerLocation and enable both mods in-game."
Write-Host "Run tools\build-pathfinding.ps1 if Dependencies\VoogleRoute.Pathfinding.dll is missing."
Write-Host "Next: open SDK in Unity 2022.3.62f2, then Mod Builder -> Build & Install."
