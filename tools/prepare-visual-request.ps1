param(
    [Parameter(Mandatory = $true)]
    [string] $ScenarioId,

    [double] $CaptureDelaySeconds = -1,
    [ValidateSet("panel", "fullScreen")]
    [string] $CaptureMode = "",
    [int] $MarginPixels = -1,
    [string] $OutputPath = "",
    [string] $ModsLocalRoot = ""
)

$ErrorActionPreference = "Stop"

$modRoot = (Get-Item $PSScriptRoot).Parent.FullName
$repoManifest = Join-Path $modRoot "tests\visual-saves\manifest.json"

if (-not $ModsLocalRoot) {
    $ModsLocalRoot = Join-Path $env:USERPROFILE "AppData\LocalLow\Hovgaard Games\Big Ambitions\ModsLocal\VoogleRoute"
}

$visualTestRoot = Join-Path $ModsLocalRoot "visual-test"
$manifestPath = Join-Path $visualTestRoot "manifest.json"
if (-not (Test-Path $manifestPath)) {
    if (Test-Path $repoManifest) {
        New-Item -ItemType Directory -Force -Path $visualTestRoot | Out-Null
        Copy-Item $repoManifest $manifestPath -Force
    }
    else {
        throw "Run install-visual-test-assets.ps1 first."
    }
}

$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$scenario = @($manifest.scenarios | Where-Object { $_.id -eq $ScenarioId }) | Select-Object -First 1
if (-not $scenario) {
    $ids = ($manifest.scenarios | ForEach-Object { $_.id }) -join ", "
    throw "Unknown scenario '$ScenarioId'. Known: $ids"
}

$defaults = $manifest.captureDefaults
if ($CaptureDelaySeconds -lt 0) {
    $CaptureDelaySeconds = [double]$defaults.delaySeconds
}
if (-not $CaptureMode) {
    $CaptureMode = [string]$defaults.mode
}
if ($MarginPixels -lt 0) {
    $MarginPixels = [int]$defaults.marginPixels
}

if (-not $OutputPath) {
    $actualDir = Join-Path $modRoot "tests\visual\actual"
    New-Item -ItemType Directory -Force -Path $actualDir | Out-Null
    $OutputPath = Join-Path $actualDir "$ScenarioId.png"
}

$request = [ordered]@{
    scenarioId          = $ScenarioId
    saveName            = [string]$scenario.saveName
    captureDelaySeconds = $CaptureDelaySeconds
    captureMode         = $CaptureMode
    marginPixels        = $MarginPixels
    outputPath          = $OutputPath
    requestedAtUtc      = (Get-Date).ToUniversalTime().ToString("o")
}

$requestPath = Join-Path $visualTestRoot "request.json"
$request | ConvertTo-Json -Depth 4 | Set-Content -Path $requestPath -Encoding UTF8

Write-Host "Visual test request ready"
Write-Host "  scenario:  $ScenarioId"
Write-Host "  save:      $($scenario.saveName)"
Write-Host "  request:   $requestPath"
Write-Host "  output:    $OutputPath"
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Build/deploy VoogleRoute if code changed"
Write-Host "  2. Main menu -> Load -> $($scenario.saveName)"
Write-Host "  3. Wait for capture; read visual-test\last-result.json"
