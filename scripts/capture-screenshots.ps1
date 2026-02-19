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
$logPath = Join-Path $repoRoot "Builds\Logs\capture-screenshots.log"
$outputDir = Join-Path $repoRoot "Builds\Screenshots"
$manifestPath = Join-Path $outputDir "index.md"
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $logPath) | Out-Null
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

$expectedShots = @(
    "01_create_default_factory.png",
    "02_create_core_swap_moon.png",
    "03_create_leftarm_swap_neon.png",
    "04_create_rightarm_swap_desert.png",
    "05_create_accessory_swap_arctic.png",
    "06_create_face_ears_swap_factory.png",
    "07_create_color_cycle_1.png",
    "08_create_color_cycle_2.png",
    "09_play_default_ui.png",
    "10_play_dance_ui.png",
    "11_play_move_ui.png",
    "12_play_new_robot_regenerated_ui.png"
)

foreach ($name in $expectedShots) {
    $path = Join-Path $outputDir $name
    if (Test-Path $path) {
        Remove-Item -Force $path
    }
}

$markerPath = Join-Path $outputDir "_capture_complete.txt"
if (Test-Path $markerPath) {
    Remove-Item -Force $markerPath
}

$arguments = @(
    "-projectPath", $repoRoot,
    "-batchmode",
    "-logFile", $logPath,
    "-executeMethod", "RoboterLego.Editor.Capture.RobotScreenshotBatch.CaptureTwelveWithUi",
    "-screenshotOutput", $outputDir
)

Write-Host "Capturing robot screenshots (timeout: $TimeoutMinutes min)..." -ForegroundColor Cyan
$timeoutSeconds = $TimeoutMinutes * 60

try {
    $exitCode = Invoke-ProcessWithTimeout `
        -FilePath $unityPath `
        -ArgumentList $arguments `
        -TimeoutSeconds $timeoutSeconds `
        -NoNewWindow `
        -OperationName "Unity screenshot capture"
}
catch {
    if (Test-Path $logPath) {
        Get-Content -Tail 200 $logPath | Out-Host
    }

    throw "$($_.Exception.Message) Log: $logPath"
}

if ($exitCode -ne 0) {
    if (Test-Path $logPath) {
        Get-Content -Tail 200 $logPath | Out-Host
    }

    throw "Screenshot capture failed."
}

if (-not (Test-Path $markerPath)) {
    if (Test-Path $logPath) {
        Get-Content -Tail 200 $logPath | Out-Host
    }

    throw "Screenshot capture completed without marker file: $markerPath"
}

$markerContent = Get-Content -Raw $markerPath
if ($markerContent -notmatch "^success") {
    throw "Screenshot capture reported failure:`n$markerContent"
}

function Test-PngSignature {
    param([string]$Path)
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -lt 8) {
        return $false
    }

    return $bytes[0] -eq 0x89 `
        -and $bytes[1] -eq 0x50 `
        -and $bytes[2] -eq 0x4E `
        -and $bytes[3] -eq 0x47 `
        -and $bytes[4] -eq 0x0D `
        -and $bytes[5] -eq 0x0A `
        -and $bytes[6] -eq 0x1A `
        -and $bytes[7] -eq 0x0A
}

$manifestLines = @("# Screenshot Manifest", "", "Captured via scripts/capture-screenshots.ps1", "")
foreach ($name in $expectedShots) {
    $path = Join-Path $outputDir $name
    if (-not (Test-Path $path)) {
        throw "Missing expected screenshot: $path"
    }

    $item = Get-Item $path
    if ($item.Length -le 0) {
        throw "Screenshot is empty: $path"
    }

    if (-not (Test-PngSignature -Path $path)) {
        throw "Screenshot is not a valid PNG signature: $path"
    }

    $manifestLines += "- $name ($($item.Length) bytes)"
}

Set-Content -Path $manifestPath -Value $manifestLines -Encoding UTF8
Write-Host "Captured $($expectedShots.Count) screenshots: $outputDir" -ForegroundColor Green
