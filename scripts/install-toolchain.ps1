param(
    [string]$UnityVersion = "2022.3.40f1",
    [string]$UnityChangeset = "cbdda657d2f0",
    [int]$WingetUpdateTimeoutMinutes = 10,
    [int]$WingetInstallTimeoutMinutes = 90,
    [int]$HubInstallTimeoutMinutes = 90
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "process-utils.ps1")

function Assert-PositiveMinutes {
    param(
        [string]$Name,
        [int]$Value
    )

    if ($Value -lt 1) {
        throw "$Name must be >= 1."
    }
}

function Assert-Command {
    param([string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found."
    }
}

function Invoke-CommandWithTimeout {
    param(
        [string]$ExecutablePath,
        [string[]]$Arguments,
        [int]$TimeoutSeconds,
        [string]$OperationName
    )

    return Invoke-ProcessWithTimeout `
        -FilePath $ExecutablePath `
        -ArgumentList $Arguments `
        -TimeoutSeconds $TimeoutSeconds `
        -NoNewWindow `
        -OperationName $OperationName
}

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Test-Internet {
    try {
        $null = Invoke-WebRequest -Uri "https://public-cdn.cloud.unity3d.com" -Method Head -TimeoutSec 10
        return $true
    }
    catch {
        return $false
    }
}

function Get-UnityHubPath {
    $candidates = @(
        "C:\Program Files\Unity Hub\Unity Hub.exe",
        "C:\Program Files\Unity Hub\UnityHub.exe",
        "C:\Program Files (x86)\Unity Hub\Unity Hub.exe",
        "C:\Program Files (x86)\Unity Hub\UnityHub.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    return $null
}

function Get-UnityWingetPackageId {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Version
    )

    if ($Version -match "^2022\.") {
        return "Unity.Unity.2022"
    }

    if ($Version -match "^2023\.") {
        return "Unity.Unity.2023"
    }

    if ($Version -match "^6000\.") {
        return "Unity.Unity.6000"
    }

    throw "No known winget Unity package id mapping for version '$Version'."
}

function Install-UnityEditorFallback {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Version
    )

    $packageId = Get-UnityWingetPackageId -Version $Version
    Write-Warning "Unity Hub CLI install is unavailable on this system. Falling back to winget package '$packageId' (version $Version)."

    $exitCode = Invoke-CommandWithTimeout `
        -ExecutablePath $script:WingetPath `
        -Arguments @("install", "-e", "--id", $packageId, "--version", $Version, "--force", "--source", "winget", "--accept-package-agreements", "--accept-source-agreements") `
        -TimeoutSeconds $script:WingetInstallTimeoutSeconds `
        -OperationName "winget install $packageId $Version"

    if ($exitCode -ne 0) {
        $listOutput = & $script:WingetPath list -e --id $packageId --source winget --accept-source-agreements 2>&1 | Out-String
        if ($listOutput -notmatch [Regex]::Escape($Version)) {
            throw "Fallback Unity editor install failed for version $Version."
        }
    }

    Write-Warning "Android build modules may not be included by fallback package. Install Android Build Support via Unity Hub if Android build fails."
}

function Invoke-HubInstall {
    param(
        [string]$HubPath,
        [string]$Version,
        [string]$Changeset,
        [int]$TimeoutSeconds
    )

    $moduleArgs = @("android", "android-sdk-ndk-tools", "android-open-jdk")
    $moduleLongArgs = @()
    $moduleShortArgs = @()
    foreach ($module in $moduleArgs) {
        $moduleLongArgs += @("--module", $module)
        $moduleShortArgs += @("-m", $module)
    }

    $attempts = @()
    $attempts += ,([object[]](@("--", "--headless", "install", "--version", $Version, "--changeset", $Changeset) + $moduleLongArgs))
    $attempts += ,([object[]](@("--", "--headless", "install", "--version", $Version, "--changeset", $Changeset) + $moduleShortArgs))
    $attempts += ,([object[]](@("--headless", "install", "--version", $Version, "--changeset", $Changeset) + $moduleLongArgs))
    $attempts += ,([object[]](@("--headless", "install", "--version", $Version, "--changeset", $Changeset) + $moduleShortArgs))

    foreach ($args in $attempts) {
        if ($args -isnot [System.Array]) {
            $args = @($args)
        }

        Write-Host "Trying Unity Hub CLI install: $($args -join ' ')" -ForegroundColor Cyan
        try {
            $exitCode = Invoke-CommandWithTimeout `
                -ExecutablePath $HubPath `
                -Arguments $args `
                -TimeoutSeconds $TimeoutSeconds `
                -OperationName "Unity Hub CLI install"

            if ($exitCode -eq 0) {
                return $true
            }

            Write-Warning "Unity Hub CLI attempt exited with code $exitCode."
        }
        catch {
            Write-Warning $_.Exception.Message
            if ($_.Exception.Message -like "*timed out*") {
                return $false
            }
        }
    }

    return $false
}

Assert-Command -Name winget
Assert-PositiveMinutes -Name "WingetUpdateTimeoutMinutes" -Value $WingetUpdateTimeoutMinutes
Assert-PositiveMinutes -Name "WingetInstallTimeoutMinutes" -Value $WingetInstallTimeoutMinutes
Assert-PositiveMinutes -Name "HubInstallTimeoutMinutes" -Value $HubInstallTimeoutMinutes

$script:WingetPath = (Get-Command winget -ErrorAction Stop).Source
$script:WingetInstallTimeoutSeconds = $WingetInstallTimeoutMinutes * 60
$hubInstallTimeoutSeconds = $HubInstallTimeoutMinutes * 60

if (-not (Test-Internet)) {
    Write-Warning "Unity CDN connectivity check failed. Continuing anyway; install may still work depending on network policy."
}

if (-not (Test-IsAdministrator)) {
    Write-Warning "You are not running as Administrator. Installation may fail depending on machine policy."
}

Write-Host "Updating winget sources..." -ForegroundColor Cyan
try {
    $sourceExitCode = Invoke-CommandWithTimeout `
        -ExecutablePath $script:WingetPath `
        -Arguments @("source", "update", "--disable-interactivity") `
        -TimeoutSeconds ($WingetUpdateTimeoutMinutes * 60) `
        -OperationName "winget source update"
}
catch {
    Write-Warning $_.Exception.Message
    $sourceExitCode = -1
}

if ($sourceExitCode -ne 0) {
    Write-Warning "winget source update returned a non-zero code. Continuing with installation."
}

Write-Host "Installing Unity Hub..." -ForegroundColor Cyan
try {
    $hubInstallExitCode = Invoke-CommandWithTimeout `
        -ExecutablePath $script:WingetPath `
        -Arguments @("install", "-e", "--id", "Unity.UnityHub", "--source", "winget", "--accept-package-agreements", "--accept-source-agreements") `
        -TimeoutSeconds $script:WingetInstallTimeoutSeconds `
        -OperationName "winget install Unity.UnityHub"

    if ($hubInstallExitCode -ne 0) {
        Write-Warning "Unity Hub install command exited with code $hubInstallExitCode."
    }
}
catch {
    Write-Warning $_.Exception.Message
}

$hubPath = Get-UnityHubPath
if (-not $hubPath) {
    throw "Unity Hub executable not found after installation."
}

Write-Host "Installing Unity editor $UnityVersion and Android modules..." -ForegroundColor Cyan
$installed = Invoke-HubInstall -HubPath $hubPath -Version $UnityVersion -Changeset $UnityChangeset -TimeoutSeconds $hubInstallTimeoutSeconds
if (-not $installed) {
    Install-UnityEditorFallback -Version $UnityVersion
}

$resolveScript = Join-Path $PSScriptRoot "resolve-unity-path.ps1"
$unityPath = & $resolveScript -UnityVersion $UnityVersion
Write-Host "Unity ready at: $unityPath" -ForegroundColor Green
