param(
    [string]$UnityVersion = "2022.3.40f1",
    [int]$MaxArtifactAgeMinutes = 120
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($MaxArtifactAgeMinutes -lt 1) {
    throw "MaxArtifactAgeMinutes must be >= 1."
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$now = Get-Date

$requiredFiles = @{
    EditModeLog = Join-Path $repoRoot "Builds\Logs\test-editmode.log"
    PlayModeLog = Join-Path $repoRoot "Builds\Logs\test-playmode.log"
    BuildLog = Join-Path $repoRoot "Builds\Logs\build-android.log"
    EditModeResults = Join-Path $repoRoot "Builds\TestResults\editmode-results.xml"
    PlayModeResults = Join-Path $repoRoot "Builds\TestResults\playmode-results.xml"
    Apk = Join-Path $repoRoot "Builds\Android\RoboterLego.apk"
}

foreach ($entry in $requiredFiles.GetEnumerator()) {
    if (-not (Test-Path $entry.Value)) {
        throw "Quality gate failed: missing required artifact '$($entry.Key)' at '$($entry.Value)'."
    }
}

function Assert-Recent {
    param(
        [string]$Path,
        [string]$Label
    )

    $item = Get-Item $Path
    $ageMinutes = ($now - $item.LastWriteTime).TotalMinutes
    if ($ageMinutes -gt $MaxArtifactAgeMinutes) {
        throw "Quality gate failed: '$Label' is stale ($([Math]::Round($ageMinutes, 1)) minutes old): $Path"
    }
}

Assert-Recent -Path $requiredFiles.EditModeResults -Label "EditMode results"
Assert-Recent -Path $requiredFiles.PlayModeResults -Label "PlayMode results"
Assert-Recent -Path $requiredFiles.Apk -Label "Android APK"

function Assert-TestRunPassed {
    param(
        [string]$XmlPath,
        [string]$Label
    )

    [xml]$xml = Get-Content -Raw $XmlPath
    $testRun = $xml.SelectSingleNode("/test-run")
    if ($null -eq $testRun) {
        throw "Quality gate failed: $Label XML missing <test-run> root: $XmlPath"
    }

    $result = [string]$testRun.result
    if ($result -notlike "Passed*") {
        throw "Quality gate failed: $Label result is '$result' (expected Passed): $XmlPath"
    }

    $failed = [int]$testRun.failed
    if ($failed -ne 0) {
        throw "Quality gate failed: $Label reports failed=${failed}: $XmlPath"
    }
}

Assert-TestRunPassed -XmlPath $requiredFiles.EditModeResults -Label "EditMode"
Assert-TestRunPassed -XmlPath $requiredFiles.PlayModeResults -Label "PlayMode"

function Assert-NoCompilerDiagnosticsInLog {
    param(
        [string]$LogPath,
        [string]$Label
    )

    $diagnostics = Select-String -Path $LogPath -Pattern "warning CS\d+|error CS\d+" -AllMatches
    if ($diagnostics) {
        $sample = ($diagnostics | Select-Object -First 20 | ForEach-Object { $_.Line }) -join [Environment]::NewLine
        throw "Quality gate failed: compiler diagnostics found in $Label log ($LogPath):`n$sample"
    }
}

Assert-NoCompilerDiagnosticsInLog -LogPath $requiredFiles.EditModeLog -Label "EditMode"
Assert-NoCompilerDiagnosticsInLog -LogPath $requiredFiles.PlayModeLog -Label "PlayMode"
Assert-NoCompilerDiagnosticsInLog -LogPath $requiredFiles.BuildLog -Label "Build"

$lookupViolations = Select-String -Path (Get-ChildItem -Path (Join-Path $repoRoot "Assets") -Recurse -Include *.cs | ForEach-Object { $_.FullName }) `
    -Pattern "FindObjectOfType\s*\(|FindObjectsOfType\s*\(" `
    | Where-Object { $_.Path -notlike "*UnityObjectLookup.cs" }

if ($lookupViolations) {
    $sample = ($lookupViolations | Select-Object -First 20 | ForEach-Object { "$($_.Path):$($_.LineNumber): $($_.Line.Trim())" }) -join [Environment]::NewLine
    throw "Quality gate failed: direct obsolete object lookup usage detected:`n$sample"
}

Write-Host "Quality gate passed (Unity target: $UnityVersion)." -ForegroundColor Green
