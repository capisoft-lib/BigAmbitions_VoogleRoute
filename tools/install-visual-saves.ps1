param(
    [string] $GameVersionFolder = "",
    [string] $SourceVersionFolder = "EA-0.11.5",
    [switch] $ListVersions,
    [switch] $WhatIf
)

$ErrorActionPreference = "Stop"

$modRoot = (Get-Item $PSScriptRoot).Parent.FullName
$manifestPath = Join-Path $modRoot "tests\visual-saves\manifest.json"
$sourceRoot = Join-Path $modRoot "tests\visual-saves\$SourceVersionFolder"

$gameDataRoot = Join-Path $env:USERPROFILE "AppData\LocalLow\Hovgaard Games\Big Ambitions"
$saveGamesRoot = Join-Path $gameDataRoot "SaveGames"

if (-not (Test-Path $saveGamesRoot)) {
    throw "SaveGames folder not found: $saveGamesRoot (install Big Ambitions first)."
}

$versionFolders = @(Get-ChildItem -Path $saveGamesRoot -Directory | Sort-Object Name)

if ($ListVersions) {
    Write-Host "SaveGames version folders:"
    foreach ($folder in $versionFolders) {
        Write-Host "  $($folder.Name)"
    }
    if (Test-Path $manifestPath) {
        $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
        Write-Host ""
        Write-Host "manifest.json hints:"
        Write-Host "  gameVersion: $($manifest.gameVersion)"
        Write-Host "  saveGameFolderHint: $($manifest.saveGameFolderHint)"
        Write-Host "  characterId: $($manifest.characterId)"
    }
    return
}

if (-not $GameVersionFolder) {
    if ($versionFolders.Count -eq 1) {
        $GameVersionFolder = $versionFolders[0].Name
        Write-Host "Using only SaveGames version folder: $GameVersionFolder"
    }
    elseif (Test-Path $manifestPath) {
        $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
        $hint = [string]$manifest.saveGameFolderHint
        $match = $versionFolders | Where-Object {
            $_.Name -eq $hint -or $_.Name -like "*$($manifest.gameVersion -replace '\.', '_')*"
        } | Select-Object -First 1
        if ($match) {
            $GameVersionFolder = $match.Name
            Write-Host "Matched manifest hint -> $GameVersionFolder"
        }
    }
}

if (-not $GameVersionFolder) {
    throw @"
Could not pick a SaveGames version folder. Use -ListVersions, then:
  .\tools\install-visual-saves.ps1 -GameVersionFolder <folder-name>
"@
}

$destVersionRoot = Join-Path $saveGamesRoot $GameVersionFolder
$characterId = "_VOOGLE_VIS_"
if (Test-Path $manifestPath) {
    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    if ($manifest.characterId) {
        $characterId = $manifest.characterId
    }
}

$sourceCharacter = Join-Path $sourceRoot $characterId
$destCharacter = Join-Path $destVersionRoot $characterId

if (-not (Test-Path $sourceCharacter)) {
    throw @"
Source fixture folder missing: $sourceCharacter
Create saves in-game first (see tests/visual-saves/README.md), then copy them here.
"@
}

$saves = @(Get-ChildItem -Path $sourceCharacter -File -Include *.hsg, *.json -Recurse)
if ($saves.Count -eq 0) {
    throw "No .hsg/.json saves found under $sourceCharacter — create fixtures before installing."
}

Write-Host "Installing visual test saves"
Write-Host "  from: $sourceCharacter"
Write-Host "  to:   $destCharacter"

if ($WhatIf) {
    Write-Host "[WhatIf] Would copy $($saves.Count) save file(s)."
    return
}

New-Item -ItemType Directory -Force -Path $destCharacter | Out-Null
Copy-Item -Path (Join-Path $sourceCharacter "*") -Destination $destCharacter -Recurse -Force

Write-Host "Done. Load saves from character '$characterId' in the main menu."
