param(
    [string[]] $ScenarioIds = @(),
    [switch] $PrepareOnly
)

$ErrorActionPreference = "Stop"

$modRoot = (Get-Item $PSScriptRoot).Parent.FullName
$manifestPath = Join-Path $modRoot "tests\visual-saves\manifest.json"
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json

if ($ScenarioIds.Count -eq 0) {
    $ScenarioIds = @($manifest.scenarios | ForEach-Object { $_.id })
}

Write-Host "VoogleRoute visual test session"
Write-Host "Character: $($manifest.characterId)"
Write-Host "Scenarios: $($ScenarioIds -join ', ')"
Write-Host ""
Write-Host "Prerequisites:"
Write-Host "  .\tools\install-visual-saves.ps1"
Write-Host "  .\tools\install-visual-test-assets.ps1"
Write-Host "  Deploy VoogleRoute mod build"
Write-Host ""

$index = 0
foreach ($id in $ScenarioIds) {
    $index++
    $scenario = @($manifest.scenarios | Where-Object { $_.id -eq $id }) | Select-Object -First 1
    if (-not $scenario) {
        Write-Warning "Skipping unknown scenario: $id"
        continue
    }

    Write-Host "=== [$index/$($ScenarioIds.Count)] $id ==="
    Write-Host "Save to load: $($scenario.saveName)"
    Write-Host "Description:  $($scenario.description)"
    Write-Host ""

    if ($PrepareOnly -or $index -eq 1) {
        & (Join-Path $PSScriptRoot "prepare-visual-request.ps1") -ScenarioId $id
        Write-Host ""
    }

    if ($PrepareOnly) {
        continue
    }

    Write-Host "Manual steps:"
    Write-Host "  1. If not the first scenario: Esc -> Main menu -> Load -> $($scenario.saveName)"
    Write-Host "  2. For the first scenario: Main menu -> Load -> $($scenario.saveName)"
    Write-Host "  3. Wait for harness capture (check ModsLocal\VoogleRoute\visual-test\last-result.json)"
    Write-Host "  4. .\tools\compare-visual.ps1 -ScenarioId $id"
    Write-Host "  5. Press Enter for next scenario (or Ctrl+C to stop)"
    [void][System.Console]::ReadLine()

    if ($index -lt $ScenarioIds.Count) {
        & (Join-Path $PSScriptRoot "prepare-visual-request.ps1") -ScenarioId $ScenarioIds[$index]
    }
}

Write-Host "Session complete."
