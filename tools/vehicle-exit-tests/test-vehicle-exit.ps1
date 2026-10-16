param([switch]$Baseline, [string]$BaselineRef = 'HEAD')
$ErrorActionPreference = 'Stop'
$modRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$tempBase = [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) 'BigAmbitions'))
$testRoot = Join-Path $tempBase ('VoogleRoute-vehicle-exit-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
try {
    foreach ($name in @('PlayerLocationSession', 'MovementModeDetector', 'PlayerNavigationRelease', 'VehicleExitNavigationRelease')) {
        $relative = "Scripts/Navigation/$name.cs"
        $destination = Join-Path $testRoot "$name.cs"
        if ($Baseline -and $name -eq 'PlayerLocationSession') {
            $original = & git -C $modRoot show "${BaselineRef}:$relative"
            if ($LASTEXITCODE -ne 0) { throw "Cannot read baseline $relative" }
            [IO.File]::WriteAllLines($destination, $original)
        } else {
            Copy-Item -LiteralPath (Join-Path $modRoot $relative) -Destination $destination
        }
    }
    Copy-Item -LiteralPath (Join-Path $modRoot 'Scripts/Live/PlayerLocationSnapshotMapper.cs') -Destination $testRoot
    $libraryRoot = [IO.Path]::GetFullPath((Join-Path $modRoot '../LIB_BaPlayerLocation/Scripts/Subscriber'))
    foreach ($name in @('PlayerLocationSnapshot.cs', 'MovementKind.cs')) {
        Copy-Item -LiteralPath (Join-Path $libraryRoot $name) -Destination $testRoot
    }
    foreach ($name in @('Program', 'Stubs')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot "$name.cs.txt") -Destination (Join-Path $testRoot "$name.cs")
    }
    @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <EnableNETAnalyzers>false</EnableNETAnalyzers>
  </PropertyGroup>
</Project>
'@ | Set-Content -LiteralPath (Join-Path $testRoot 'VehicleExit.Tests.csproj') -Encoding utf8
    & dotnet run --project (Join-Path $testRoot 'VehicleExit.Tests.csproj') --configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'Vehicle-exit regression harness failed.' }
} finally {
    $resolved = [IO.Path]::GetFullPath($testRoot)
    if (-not $resolved.StartsWith($tempBase.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean a test path outside $tempBase"
    }
    if (Test-Path -LiteralPath $resolved) {
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
