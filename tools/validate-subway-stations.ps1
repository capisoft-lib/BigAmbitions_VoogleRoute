param(
    [Parameter(Mandatory = $true)]
    [string] $Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$expectedHeader = "station_name,neighborhood,x,y,z,nav_x,nav_y,nav_z"
$expectedNames = @(
    "GarmentDistrictEastStation",
    "GarmentDistrictHarborStation",
    "GarmentDistrictWestStation",
    "HellsKitchenEastStation",
    "HellsKitchenNorthStation",
    "HellsKitchenSouthStation",
    "HellsKitchenWestStation",
    "IndustryCityCenterStation",
    "IndustryCityHarborStation",
    "IndustryCitySunsetParkStation",
    "LowerManhattanCenterStation",
    "LowerManhattanNorthStation",
    "MidtownMadisonParkStation",
    "MidtownNorthWestStation",
    "MidtownStPatrickStation",
    "MidtownTimesSquareStation",
    "MurrayHillEastStation",
    "MurrayHillNorthStation",
    "MurrayHillSouthStation",
    "TheHamptonsStation"
)

$fullPath = [System.IO.Path]::GetFullPath($Path)
if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
    throw "Subway fallback is missing: '$fullPath'."
}

$lines = @(Get-Content -LiteralPath $fullPath)
if ($lines.Count -eq 0 -or $lines[0] -cne $expectedHeader) {
    throw "Unexpected subway CSV header in '$fullPath'. Expected '$expectedHeader'."
}

$dataLines = @($lines | Select-Object -Skip 1 | Where-Object {
    -not [string]::IsNullOrWhiteSpace($_) -and -not $_.StartsWith("#", [System.StringComparison]::Ordinal)
})
if ($dataLines.Count -ne $expectedNames.Count) {
    throw "Subway fallback must contain exactly $($expectedNames.Count) stations; found $($dataLines.Count) in '$fullPath'."
}

$names = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$culture = [System.Globalization.CultureInfo]::InvariantCulture
$numberStyle = [System.Globalization.NumberStyles]::Float
for ($lineIndex = 0; $lineIndex -lt $dataLines.Count; $lineIndex++) {
    $lineNumber = $lineIndex + 2
    $parts = @($dataLines[$lineIndex].Split(','))
    if ($parts.Count -ne 8) {
        throw "Subway CSV line $lineNumber must contain exactly 8 columns; found $($parts.Count)."
    }

    $stationName = $parts[0].Trim()
    if ([string]::IsNullOrWhiteSpace($stationName)) {
        throw "Subway CSV line $lineNumber has an empty station name."
    }
    if (-not $names.Add($stationName)) {
        throw "Duplicate subway station '$stationName' on line $lineNumber."
    }

    $coordinates = [double[]]::new(6)
    for ($coordinateIndex = 0; $coordinateIndex -lt 6; $coordinateIndex++) {
        $parsed = 0.0
        $text = $parts[$coordinateIndex + 2].Trim()
        if (-not [double]::TryParse($text, $numberStyle, $culture, [ref] $parsed) -or
            [double]::IsNaN($parsed) -or [double]::IsInfinity($parsed)) {
            throw "Invalid subway coordinate '$text' on line $lineNumber."
        }
        $coordinates[$coordinateIndex] = $parsed
    }

    if ($coordinates[0] -eq 0 -and $coordinates[1] -eq 0 -and $coordinates[2] -eq 0) {
        throw "Subway station '$stationName' has an empty world position."
    }
    if ($coordinates[3] -eq 0 -and $coordinates[4] -eq 0 -and $coordinates[5] -eq 0) {
        throw "Subway station '$stationName' has an empty navigation position."
    }
}

$missingNames = @($expectedNames | Where-Object { -not $names.Contains($_) })
$unexpectedNames = @($names | Where-Object { $_ -notin $expectedNames })
if ($missingNames.Count -gt 0 -or $unexpectedNames.Count -gt 0) {
    throw "Unexpected subway station set. Missing: $($missingNames -join ', '); unexpected: $($unexpectedNames -join ', ')."
}

[pscustomobject]@{
    Path = $fullPath
    StationCount = $dataLines.Count
    Sha256 = (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash
    Valid = $true
}
