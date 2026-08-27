param(
    [ValidateSet("Install", "Remove", "ShowExport")]
    [string] $Action = "Install"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "..\..\..\..\scripts\_project.ps1")

$exporterId = "VoogleRouteSubwayExporter"
$templatePath = Join-Path $PSScriptRoot "SubwayStationExporterMod.cs.template"
$tempRoot = Join-Path $ProjectPath "Temp\VoogleRouteSubwayExporter"
$tempSource = Join-Path $tempRoot "SubwayStationExporterMod.cs"
$tempRsp = Join-Path $tempRoot "compile.rsp"
$tempDll = Join-Path $tempRoot "$exporterId.dll"
$installRoot = [System.IO.Path]::GetFullPath((Join-Path $ModsLocalRoot $exporterId))
$modsRootFull = [System.IO.Path]::GetFullPath($ModsLocalRoot).TrimEnd("\", "/")
$exportPath = Join-Path $env:USERPROFILE "AppData\LocalLow\Hovgaard Games\Big Ambitions\VoogleRouteDevExports\subway_stations.csv"

function Assert-ExporterInstallPath {
    $expectedPrefix = $modsRootFull + [System.IO.Path]::DirectorySeparatorChar
    if (-not $installRoot.StartsWith($expectedPrefix, [System.StringComparison]::OrdinalIgnoreCase) -or
        (Split-Path -Leaf $installRoot) -ne $exporterId) {
        throw "Refusing to manage unexpected exporter path '$installRoot'."
    }
}

Assert-ExporterInstallPath

if ($Action -eq "Remove") {
    if (Test-Path -LiteralPath $installRoot) {
        Remove-Item -LiteralPath $installRoot -Recurse -Force
        Write-Host "Removed developer-only exporter -> $installRoot"
    }
    else {
        Write-Host "Developer-only exporter is already absent -> $installRoot"
    }
    return
}

if ($Action -eq "ShowExport") {
    if (-not (Test-Path -LiteralPath $exportPath -PathType Leaf)) {
        throw "No runtime export found at '$exportPath'. Install the exporter and load a city once."
    }
    & (Join-Path $PSScriptRoot "validate-subway-stations.ps1") -Path $exportPath
    return
}

if (-not (Test-Path -LiteralPath $templatePath -PathType Leaf)) {
    throw "Exporter template is missing: '$templatePath'."
}

$rspPath = Ensure-BeeRsp
$rspLines = @(Get-Content -LiteralPath $rspPath)
$referenceLines = @(Get-PlayerRuntimeReferenceLines)
$defineLines = @(Get-PlayerRuntimeDefineLines -RspLines $rspLines)
$otherLines = @(Ensure-RoslynParallelFlag @($rspLines | Where-Object {
    $_ -like '-target:*' -or $_ -like '/optimize*' -or $_ -like '/debug:*' -or $_ -like '-langversion:*'
}))

New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
Copy-Item -LiteralPath $templatePath -Destination $tempSource -Force
$outputLine = '-out:"' + $tempDll.Replace('\', '/') + '"'
$sourceLine = '"' + $tempSource.Replace('\', '/') + '"'
$rspBody = @($otherLines | Where-Object { $_ -notlike '-out:*' }) + $outputLine + $defineLines + $referenceLines + $sourceLine
Set-Content -LiteralPath $tempRsp -Value $rspBody -Encoding UTF8

& $Dotnet exec $Csc /noconfig /nostdlib "@$tempRsp"
if ($LASTEXITCODE -ne 0) {
    throw "Developer subway exporter compilation failed."
}
Assert-PlayerRuntimeAssembly -DllPath $tempDll

if (Test-Path -LiteralPath $installRoot) {
    Remove-Item -LiteralPath $installRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $installRoot -Force | Out-Null
Copy-Item -LiteralPath $tempDll -Destination (Join-Path $installRoot "$exporterId.dll") -Force

Write-Host "Installed developer-only one-shot exporter -> $installRoot"
Write-Host "Load a city once, then validate with:"
Write-Host "  powershell -NoProfile -File `"$PSCommandPath`" -Action ShowExport"
Write-Host "Remove it before release with:"
Write-Host "  powershell -NoProfile -File `"$PSCommandPath`" -Action Remove"
