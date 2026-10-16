param([switch] $Install)
$ErrorActionPreference = 'Stop'
$modRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectRoot = (Resolve-Path (Join-Path $modRoot '../../..')).Path
. (Join-Path $projectRoot 'scripts/_project.ps1')
$scratch = Join-Path ([IO.Path]::GetTempPath()) ('BigAmbitions/VoogleRoute-physical-' + [Guid]::NewGuid().ToString('N'))
$output = Join-Path $projectRoot 'Output/VoogleRoute'
$local = Join-Path $ModsLocalRoot 'VoogleRoute'
New-Item -ItemType Directory -Path $scratch | Out-Null
try {
    $baseLines = Get-Content (Ensure-BeeRsp)
    $sources = @(Get-ChildItem -LiteralPath $modRoot -Filter '*.cs' -File -Recurse |
        Where-Object { $_.FullName -notmatch '\\(PathFinding|tools|Editor)\\' } |
        ForEach-Object { '"' + $_.FullName.Replace('\', '/') + '"' })
    $references = @(Get-PlayerRuntimeReferenceLines)
    foreach ($dependency in @(
        (Join-Path $projectRoot 'Output/LIB_BaPlayerLocation/LIB_BaPlayerLocation.dll'),
        (Join-Path $projectRoot 'Output/LIB_BaUnifiedUI/LIB_BaUnifiedUI.dll'),
        (Join-Path $modRoot 'Dependencies/VoogleRoute.Pathfinding.dll'))) {
        if (-not (Test-Path -LiteralPath $dependency)) { throw "Missing compile dependency: $dependency" }
        $references += '-r:"' + $dependency.Replace('\', '/') + '"'
    }
    $flags = @(Ensure-RoslynParallelFlag @($baseLines | Where-Object {
        $_ -like '-target:*' -or $_ -like '/optimize*' -or $_ -like '-langversion:*'
    }))
    $dll = Join-Path $scratch 'VoogleRoute.dll'
    $rsp = Join-Path $scratch 'compile.rsp'
    $rspLines = $flags + @('/debug-', ('-out:"' + $dll.Replace('\', '/') + '"')) +
        @(Get-PlayerRuntimeDefineLines -RspLines $baseLines) + $references + $sources
    Set-Content -LiteralPath $rsp -Value $rspLines -Encoding UTF8
    Push-Location $projectRoot
    try {
        & $Dotnet exec $Csc /noconfig /nostdlib "@$rsp"
        if ($LASTEXITCODE -ne 0) { throw 'VoogleRoute compilation failed.' }
    } finally { Pop-Location }
    Assert-PlayerRuntimeAssembly -DllPath $dll
    Assert-PlayerRuntimeAssembly -DllPath (Join-Path $modRoot 'Dependencies/VoogleRoute.Pathfinding.dll')

    # Build an explicit package. User configuration and unrelated local files are never selected.
    $payload = [ordered]@{ 'VoogleRoute.dll' = $dll }
    foreach ($name in @('ModManifest.asset', 'Thumbnail.png', 'config.json.example', 'REQUIRED_MODS.txt')) {
        $payload[$name] = Join-Path $modRoot $name
    }
    foreach ($folder in @('Data', 'Locales')) {
        $source = Join-Path $modRoot $folder
        foreach ($file in Get-ChildItem -LiteralPath $source -File -Recurse | Where-Object Extension -ne '.meta') {
            $payload[$folder + '/' + $file.FullName.Substring($source.Length).TrimStart('\', '/')] = $file.FullName
        }
    }
    $payload['Dependencies/VoogleRoute.Pathfinding.dll'] = Join-Path $modRoot 'Dependencies/VoogleRoute.Pathfinding.dll'
    foreach ($relative in $payload.Keys) {
        $target = Join-Path $output $relative
        New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
        Copy-Item -LiteralPath $payload[$relative] -Destination $target -Force
    }
    $hash = (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash
    Write-Host "Built $($sources.Count) sources; $($payload.Count) package files; SHA256 $hash"
    if ($Install) {
        if (Get-Process -Name 'Big Ambitions' -ErrorAction SilentlyContinue) {
            throw 'Close Big Ambitions before replacing the local mod. Build is available in Output/VoogleRoute.'
        }
        $preserved = @{}
        if (Test-Path -LiteralPath $local) {
            foreach ($file in Get-ChildItem -LiteralPath $local -File -Recurse) {
                $relative = $file.FullName.Substring($local.Length).TrimStart('\', '/').Replace('\', '/')
                if (-not $payload.Contains($relative)) { $preserved[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
            }
        }
        foreach ($relative in $payload.Keys) {
            $target = Join-Path $local $relative
            New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
            Copy-Item -LiteralPath (Join-Path $output $relative) -Destination $target -Force
        }
        foreach ($file in $preserved.Keys) {
            if ((Get-FileHash -LiteralPath $file).Hash -ne $preserved[$file]) { throw "User data changed: $file" }
        }
        foreach ($relative in $payload.Keys) {
            if ((Get-FileHash -LiteralPath (Join-Path $local $relative)).Hash -ne
                (Get-FileHash -LiteralPath (Join-Path $output $relative)).Hash) { throw "Installed file differs: $relative" }
        }
        Write-Host "Installed in $local; $($preserved.Count) user files preserved."
    }
} finally {
    $resolvedScratch = [IO.Path]::GetFullPath($scratch)
    $expectedParent = [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) 'BigAmbitions')) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedScratch.StartsWith($expectedParent, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected scratch directory.' }
    Remove-Item -LiteralPath $resolvedScratch -Recurse -ErrorAction Stop
}
