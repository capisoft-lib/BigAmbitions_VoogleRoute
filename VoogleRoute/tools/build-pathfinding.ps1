$ErrorActionPreference = "Stop"

$modRoot = Split-Path $PSScriptRoot -Parent
$csproj = Join-Path $modRoot "PathFinding\VoogleRoute.Pathfinding.csproj"

if (-not (Test-Path $csproj)) {
    throw @"
PathFinding submodule missing: $csproj
Run from the mod repo root:
  git submodule update --init --recursive
"@
}

dotnet build $csproj -c Release --nologo -v q

$dll = Join-Path $modRoot "PathFinding\bin\Release\netstandard2.1\VoogleRoute.Pathfinding.dll"
if (-not (Test-Path $dll)) {
    throw "Build succeeded but DLL missing: $dll"
}

$target = Join-Path $modRoot "Dependencies\VoogleRoute.Pathfinding.dll"
Copy-Item $dll -Destination $target -Force
Write-Host "Copied -> $target"
Write-Host "Pathfinding DLL updated. Rebuild VoogleRoute in Unity Mod Builder."
