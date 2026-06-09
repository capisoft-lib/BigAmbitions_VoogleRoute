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

dotnet build $csproj -c Release --nologo -v q

$dll = Join-Path $pfRoot "bin\Release\netstandard2.1\VoogleRoute.Pathfinding.dll"
if (-not (Test-Path $dll)) {
    throw "Build succeeded but DLL missing: $dll"
}

$dllTarget = Join-Path $modRoot "Dependencies\VoogleRoute.Pathfinding.dll"
Copy-Item $dll -Destination $dllTarget -Force
Write-Host "Copied -> $dllTarget"

& (Join-Path $pfRoot "tools\sync-route-data.ps1") -ModRoot $modRoot

Write-Host "PathFinding artifacts updated. Rebuild VoogleRoute in Unity Mod Builder."
