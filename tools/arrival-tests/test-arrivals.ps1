param([switch]$Baseline, [string]$BaselineRef = 'HEAD')
$ErrorActionPreference = 'Stop'
$modRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testRoot = Join-Path $env:TEMP ('VoogleRoute-arrivals-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot | Out-Null
$sources = @('NavigationArrivalService', 'NavigationDestinationClear', 'NavigationTargetTracker', 'DestinationResolver', 'JobDestinationSync', 'CompletedNavigationTarget', 'AutoWalkService')
foreach ($name in $sources) {
    $relative = "Scripts/Navigation/$name.cs"
    $destination = Join-Path $testRoot "$name.cs"
    if ($Baseline -and $name -ne 'CompletedNavigationTarget') {
        $original = & git -C $modRoot show "${BaselineRef}:$relative"
        if ($LASTEXITCODE -ne 0) { throw "Cannot read baseline $relative" }
        [IO.File]::WriteAllLines($destination, $original)
    } else {
        Copy-Item -LiteralPath (Join-Path $modRoot $relative) -Destination $destination
    }
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Program.cs.txt') -Destination (Join-Path $testRoot 'Program.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Stubs.cs.txt') -Destination (Join-Path $testRoot 'Stubs.cs')
@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <EnableNETAnalyzers>false</EnableNETAnalyzers>
  </PropertyGroup>
</Project>
'@ | Set-Content -LiteralPath (Join-Path $testRoot 'Arrival.Tests.csproj') -Encoding utf8
Write-Host "Testing production navigation sources in $testRoot"
$runArgs = @('run', '--project', (Join-Path $testRoot 'Arrival.Tests.csproj'), '--configuration', 'Release')
if ($Baseline) { $runArgs += @('--', 'loop-only') }
& dotnet @runArgs
if ($LASTEXITCODE -ne 0) { throw 'Arrival regression harness failed.' }
