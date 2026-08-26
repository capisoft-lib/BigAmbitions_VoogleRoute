param(
    [string]$BigAmbitionsManagedPath = $env:BA_GAME_MANAGED_PATH,
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"

$modRoot = Split-Path $PSScriptRoot -Parent
$pfRoot = Join-Path $modRoot "PathFinding"
$csproj = Join-Path $pfRoot "VoogleRoute.Pathfinding.csproj"

if (-not (Test-Path $csproj)) {
    throw @"
PathFinding submodule missing: $csproj
From the mod repo root run:
  git submodule update --init --recursive
"@
}

$buildScript = Join-Path $pfRoot "build-pathfinding.ps1"
$buildArgs = @{ SkipTests = $SkipTests }
if (-not [string]::IsNullOrWhiteSpace($BigAmbitionsManagedPath)) {
    $buildArgs.BigAmbitionsManagedPath = $BigAmbitionsManagedPath
}
& $buildScript @buildArgs
if ($LASTEXITCODE -ne 0) {
    throw "PathFinding player-runtime build failed."
}

$dll = Join-Path $modRoot "Dependencies\VoogleRoute.Pathfinding.dll"
if (-not (Test-Path $dll)) {
    throw "Build succeeded but authoritative dependency DLL is missing: $dll"
}

& (Join-Path $pfRoot "tools\sync-route-data.ps1") -ModRoot $modRoot

Write-Host "PathFinding artifacts updated. Rebuild VoogleRoute in Unity Mod Builder."
