param(
    [string]$UnityVersion = "2022.3.40f1",
    [int]$TimeoutMinutes = 30
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "process-utils.ps1")

if ($TimeoutMinutes -lt 1) {
    throw "TimeoutMinutes must be >= 1."
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$resolveScript = Join-Path $PSScriptRoot "resolve-unity-path.ps1"
$unityPath = & $resolveScript -UnityVersion $UnityVersion
$resultsPath = Join-Path $repoRoot "Builds\TestResults\editmode-results.xml"
$logPath = Join-Path $repoRoot "Builds\Logs\test-editmode.log"
$resultsDir = Split-Path -Parent $resultsPath
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $logPath) | Out-Null
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null
if (Test-Path $resultsPath) {
    Remove-Item -Force $resultsPath
}

function Invoke-EditModeRun {
    param(
        [switch]$WithQuit
    )

    $arguments = @(
        "-projectPath", $repoRoot,
        "-batchmode",
        "-nographics"
    )

    if ($WithQuit) {
        $arguments += "-quit"
    }

    $arguments += @(
        "-logFile", $logPath,
        "-runTests",
        "-testPlatform", "EditMode",
        "-assemblyNames", "RoboterLego.EditModeTests",
        "-testResults", $resultsPath
    )

    return Invoke-ProcessWithTimeout `
        -FilePath $unityPath `
        -ArgumentList $arguments `
        -TimeoutSeconds $timeoutSeconds `
        -NoNewWindow `
        -OperationName "Unity EditMode tests"
}

Write-Host "Running Unity EditMode tests (timeout: $TimeoutMinutes min)..." -ForegroundColor Cyan
$timeoutSeconds = $TimeoutMinutes * 60

try {
    $exitCode = Invoke-EditModeRun -WithQuit

    if ($exitCode -eq 0 -and -not (Test-Path $resultsPath)) {
        Write-Warning "Unity exited successfully but did not emit EditMode results. Retrying once without -quit."
        $exitCode = Invoke-EditModeRun
    }
}
catch {
    if (Test-Path $logPath) {
        Get-Content -Tail 200 $logPath | Out-Host
    }

    throw "$($_.Exception.Message) Log: $logPath"
}

if ($exitCode -ne 0) {
    $logContent = ""
    if (Test-Path $logPath) {
        $logContent = Get-Content -Raw $logPath
        Get-Content -Tail 200 $logPath | Out-Host
    }

    if ($logContent -match "No valid Unity Editor license found|Token not found in cache|com.unity.editor.headless") {
        throw "Unity license is not activated for batchmode. Open Unity Hub, sign in, and activate a Personal/Pro license, then rerun scripts/test-editmode.ps1."
    }

    throw "EditMode tests failed. Results: $resultsPath"
}

if (-not (Test-Path $resultsPath)) {
    if (Test-Path $logPath) {
        Get-Content -Tail 200 $logPath | Out-Host
    }

    throw "EditMode tests finished but did not produce results at '$resultsPath'."
}

$resultsContent = Get-Content -Raw $resultsPath
if ([string]::IsNullOrWhiteSpace($resultsContent) -or $resultsContent -notmatch "<test-run") {
    Write-Warning "EditMode results file was empty/invalid. Retrying once without -quit."
    Remove-Item -Force $resultsPath -ErrorAction SilentlyContinue

    try {
        $exitCode = Invoke-EditModeRun
    }
    catch {
        if (Test-Path $logPath) {
            Get-Content -Tail 200 $logPath | Out-Host
        }

        throw "$($_.Exception.Message) Log: $logPath"
    }

    if ($exitCode -ne 0 -or -not (Test-Path $resultsPath)) {
        throw "EditMode retry failed to produce valid results: $resultsPath"
    }

    $resultsContent = Get-Content -Raw $resultsPath
    if ([string]::IsNullOrWhiteSpace($resultsContent) -or $resultsContent -notmatch "<test-run") {
        throw "EditMode results file is empty or invalid after retry: $resultsPath"
    }
}

Write-Host "EditMode tests passed. Results: $resultsPath" -ForegroundColor Green
