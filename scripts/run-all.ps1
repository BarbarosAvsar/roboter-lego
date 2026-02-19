param(
    [string]$UnityVersion = "2022.3.40f1",
    [int]$BootstrapTimeoutMinutes = 20,
    [int]$EditModeTimeoutMinutes = 30,
    [int]$PlayModeTimeoutMinutes = 35,
    [int]$BuildTimeoutMinutes = 60
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($BootstrapTimeoutMinutes -lt 1 -or $EditModeTimeoutMinutes -lt 1 -or $PlayModeTimeoutMinutes -lt 1 -or $BuildTimeoutMinutes -lt 1) {
    throw "All timeout values must be >= 1 minute."
}

$bootstrapScript = Join-Path $PSScriptRoot "bootstrap-project.ps1"
$editTestsScript = Join-Path $PSScriptRoot "test-editmode.ps1"
$playTestsScript = Join-Path $PSScriptRoot "test-playmode.ps1"
$buildScript = Join-Path $PSScriptRoot "build-android.ps1"

& $bootstrapScript -UnityVersion $UnityVersion -TimeoutMinutes $BootstrapTimeoutMinutes
& $editTestsScript -UnityVersion $UnityVersion -TimeoutMinutes $EditModeTimeoutMinutes
& $playTestsScript -UnityVersion $UnityVersion -TimeoutMinutes $PlayModeTimeoutMinutes -SkipBootstrap
& $buildScript -UnityVersion $UnityVersion -TimeoutMinutes $BuildTimeoutMinutes -SkipBootstrap

Write-Host "Full pipeline completed successfully." -ForegroundColor Green
