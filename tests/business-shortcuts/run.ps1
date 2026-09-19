param([string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('voogleroute-business-tests-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Program.cs.txt') -Destination (Join-Path $testRoot 'Program.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../../Scripts/Navigation/PlayerBusinessBookmarkStore.cs') -Destination $testRoot
@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <EnableNETAnalyzers>false</EnableNETAnalyzers>
  </PropertyGroup>
</Project>
'@ | Set-Content -LiteralPath (Join-Path $testRoot 'BusinessShortcuts.csproj') -Encoding utf8
& $Dotnet run --project (Join-Path $testRoot 'BusinessShortcuts.csproj') --configuration Release
if ($LASTEXITCODE -ne 0) { throw 'Business shortcut regression checks failed.' }
Write-Host "Test workspace: $testRoot"
