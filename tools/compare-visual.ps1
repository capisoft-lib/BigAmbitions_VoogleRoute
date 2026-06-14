param(
    [Parameter(Mandatory = $true)]
    [string] $ScenarioId,

    [string] $BaselinePath = "",
    [string] $ActualPath = "",
    [string] $DiffPath = "",
    [double] $MaxDiffPercent = 1.0,
    [switch] $UpdateBaseline
)

$ErrorActionPreference = "Stop"

$modRoot = (Get-Item $PSScriptRoot).Parent.FullName
$visualRoot = Join-Path $modRoot "tests\visual"
$baselineDir = Join-Path $visualRoot "baselines"
$actualDir = Join-Path $visualRoot "actual"
$diffDir = Join-Path $visualRoot "diff"

if (-not $BaselinePath) { $BaselinePath = Join-Path $baselineDir "$ScenarioId.png" }
if (-not $ActualPath) { $ActualPath = Join-Path $actualDir "$ScenarioId.png" }
if (-not $DiffPath) { $DiffPath = Join-Path $diffDir "$ScenarioId-diff.png" }

if ($UpdateBaseline) {
    if (-not (Test-Path $ActualPath)) {
        throw "Actual capture missing: $ActualPath"
    }
    New-Item -ItemType Directory -Force -Path $baselineDir | Out-Null
    Copy-Item $ActualPath $BaselinePath -Force
    Write-Host "Baseline updated: $BaselinePath"
    return
}

if (-not (Test-Path $BaselinePath)) {
    throw "Baseline missing: $BaselinePath (run with -UpdateBaseline after first good capture)"
}
if (-not (Test-Path $ActualPath)) {
    throw "Actual capture missing: $ActualPath"
}

Add-Type -AssemblyName System.Drawing

function Get-BitmapPixels([System.Drawing.Bitmap] $bitmap) {
    $rect = New-Object System.Drawing.Rectangle 0, 0, $bitmap.Width, $bitmap.Height
    $data = $bitmap.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, $bitmap.PixelFormat)
    try {
        $bytes = New-Object byte[] ($data.Stride * $bitmap.Height)
        [System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $bytes, 0, $bytes.Length)
        return @{
            Bytes  = $bytes
            Width  = $bitmap.Width
            Height = $bitmap.Height
            Stride = $data.Stride
        }
    }
    finally {
        $bitmap.UnlockBits($data)
    }
}

$baselineBmp = [System.Drawing.Bitmap]::FromFile((Resolve-Path $BaselinePath))
$actualBmp = [System.Drawing.Bitmap]::FromFile((Resolve-Path $ActualPath))
$diffBmp = $null

try {
    if ($baselineBmp.Width -ne $actualBmp.Width -or $baselineBmp.Height -ne $actualBmp.Height) {
        throw "Size mismatch: baseline $($baselineBmp.Width)x$($baselineBmp.Height) vs actual $($actualBmp.Width)x$($actualBmp.Height)"
    }

    $basePx = Get-BitmapPixels $baselineBmp
    $actPx = Get-BitmapPixels $actualBmp

    $width = $basePx.Width
    $height = $basePx.Height
    $diffBmp = New-Object System.Drawing.Bitmap $width, $height
    $mismatch = 0
    $tolerance = 8

    for ($y = 0; $y -lt $height; $y++) {
        for ($x = 0; $x -lt $width; $x++) {
            $offset = ($y * $basePx.Stride) + ($x * 4)
            $br = $basePx.Bytes[$offset]
            $bg = $basePx.Bytes[$offset + 1]
            $bb = $basePx.Bytes[$offset + 2]
            $ar = $actPx.Bytes[$offset]
            $ag = $actPx.Bytes[$offset + 1]
            $ab = $actPx.Bytes[$offset + 2]

            $delta = [Math]::Abs($br - $ar) + [Math]::Abs($bg - $ag) + [Math]::Abs($bb - $ab)
            if ($delta -gt $tolerance) {
                $mismatch++
                $diffBmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, 255, 0, 0))
            }
            else {
                $gray = [int](($br + $bg + $bb) / 3 * 0.35)
                $diffBmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $gray, $gray, $gray))
            }
        }
    }

    $total = $width * $height
    $percent = [Math]::Round(100.0 * $mismatch / $total, 4)

    New-Item -ItemType Directory -Force -Path $diffDir | Out-Null
    $diffBmp.Save($DiffPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $diffBmp.Dispose()
    $diffBmp = $null

    Write-Host "Scenario:   $ScenarioId"
    Write-Host "Baseline:   $BaselinePath"
    Write-Host "Actual:     $ActualPath"
    Write-Host "Diff:       $DiffPath"
    Write-Host "Mismatch:   $mismatch / $total pixels ($percent%)"
    Write-Host "Threshold:  $MaxDiffPercent%"

    if ($percent -gt $MaxDiffPercent) {
        throw "Visual diff exceeds threshold ($percent% > $MaxDiffPercent%)"
    }

    Write-Host "PASS"
}
finally {
    $baselineBmp.Dispose()
    $actualBmp.Dispose()
    if ($null -ne $diffBmp) { $diffBmp.Dispose() }
}
