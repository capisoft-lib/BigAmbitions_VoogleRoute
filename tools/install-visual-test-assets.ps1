param(
    [string] $ModsLocalRoot = ""
)

$ErrorActionPreference = "Stop"

$modRoot = (Get-Item $PSScriptRoot).Parent.FullName
$manifestSource = Join-Path $modRoot "tests\visual-saves\manifest.json"

if (-not $ModsLocalRoot) {
    $ModsLocalRoot = Join-Path $env:USERPROFILE "AppData\LocalLow\Hovgaard Games\Big Ambitions\ModsLocal\VoogleRoute"
}

$visualTestRoot = Join-Path $ModsLocalRoot "visual-test"
New-Item -ItemType Directory -Force -Path $visualTestRoot | Out-Null

if (-not (Test-Path $manifestSource)) {
    throw "Missing manifest: $manifestSource"
}

Copy-Item $manifestSource (Join-Path $visualTestRoot "manifest.json") -Force

$exampleRequest = @{
    scenarioId         = "route-action-370"
    captureDelaySeconds = 2.0
    captureMode        = "panel"
    marginPixels       = 4
    outputPath         = ""
} | ConvertTo-Json -Depth 4

$examplePath = Join-Path $visualTestRoot "request.example.json"
Set-Content -Path $examplePath -Value $exampleRequest -Encoding UTF8

Write-Host "Installed visual-test assets -> $visualTestRoot"
Write-Host "  manifest.json"
Write-Host "  request.example.json"
Write-Host ""
Write-Host "Use prepare-visual-request.ps1 before loading a test save."
