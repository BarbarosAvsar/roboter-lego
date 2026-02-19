param(
    [string]$UnityVersion = "2022.3.40f1",
    [int]$TimeoutMinutes = 20
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
$logPath = Join-Path $repoRoot "Builds\Logs\bootstrap.log"
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $logPath) | Out-Null

$arguments = @(
    "-projectPath", $repoRoot,
    "-batchmode",
    "-nographics",
    "-quit",
    "-logFile", $logPath,
    "-executeMethod", "RoboterLego.Editor.Bootstrap.CreatePlayableScene.Execute"
)

Write-Host "Bootstrapping playable scene and Android settings (timeout: $TimeoutMinutes min)..." -ForegroundColor Cyan
$timeoutSeconds = $TimeoutMinutes * 60

try {
    $exitCode = Invoke-ProcessWithTimeout `
        -FilePath $unityPath `
        -ArgumentList $arguments `
        -TimeoutSeconds $timeoutSeconds `
        -NoNewWindow `
        -OperationName "Unity bootstrap"
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
        throw "Unity license is not activated for batchmode. Open Unity Hub, sign in, and activate a Personal/Pro license, then rerun scripts/bootstrap-project.ps1."
    }

    throw "Unity bootstrap failed."
}

Write-Host "Bootstrap completed." -ForegroundColor Green
