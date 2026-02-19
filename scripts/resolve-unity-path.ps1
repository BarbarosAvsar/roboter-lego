param(
    [string]$UnityVersion = "2022.3.40f1",
    [switch]$AllowAnyInstalled
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Find-UnityUnderBase {
    param([string]$BasePath)

    if (-not (Test-Path $BasePath)) {
        return @()
    }

    $results = @()

    $direct = Join-Path $BasePath "Editor\Unity.exe"
    if (Test-Path $direct) {
        $results += $direct
    }

    $dirs = Get-ChildItem $BasePath -Directory -ErrorAction SilentlyContinue
    foreach ($dir in $dirs) {
        $oneLevel = Join-Path $dir.FullName "Editor\Unity.exe"
        if (Test-Path $oneLevel) {
            $results += $oneLevel
        }

        $subDirs = Get-ChildItem $dir.FullName -Directory -ErrorAction SilentlyContinue
        foreach ($subDir in $subDirs) {
            $twoLevel = Join-Path $subDir.FullName "Editor\Unity.exe"
            if (Test-Path $twoLevel) {
                $results += $twoLevel
            }
        }
    }

    return $results | Select-Object -Unique
}

function Get-InstalledUnityExecutables {
    $allFound = @()

    $hubRoots = @(
        "C:\Program Files\Unity\Hub\Editor",
        "C:\Program Files (x86)\Unity\Hub\Editor"
    )

    foreach ($hubRoot in $hubRoots) {
        if (-not (Test-Path $hubRoot)) {
            continue
        }

        $editorDirs = Get-ChildItem $hubRoot -Directory -ErrorAction SilentlyContinue | Sort-Object Name -Descending
        foreach ($dir in $editorDirs) {
            $path = Join-Path $dir.FullName "Editor\Unity.exe"
            if (Test-Path $path) {
                $allFound += $path
            }
        }
    }

    $legacyPaths = @(
        "C:\Program Files\Unity\Editor\Unity.exe",
        "C:\Program Files (x86)\Unity\Editor\Unity.exe"
    )

    foreach ($path in $legacyPaths) {
        if (Test-Path $path) {
            $allFound += $path
        }
    }

    $scanBases = @(
        "C:\Program Files\Unity",
        "C:\Program Files (x86)\Unity",
        "C:\Program Files",
        "C:\Program Files (x86)"
    )

    foreach ($base in $scanBases) {
        $baseFound = Find-UnityUnderBase -BasePath $base
        foreach ($path in $baseFound) {
            $allFound += $path
        }
    }

    return $allFound | Select-Object -Unique
}

function Resolve-UnityPath {
    param(
        [string]$Version,
        [switch]$AllowFallbackToAnyInstalled
    )

    $exactCandidates = @(
        "C:\Program Files\Unity\Hub\Editor\$Version\Editor\Unity.exe",
        "C:\Program Files (x86)\Unity\Hub\Editor\$Version\Editor\Unity.exe",
        "C:\Program Files\Unity $Version\Editor\Unity.exe",
        "C:\Program Files (x86)\Unity $Version\Editor\Unity.exe",
        "C:\Program Files\Unity\$Version\Editor\Unity.exe",
        "C:\Program Files (x86)\Unity\$Version\Editor\Unity.exe"
    )

    foreach ($candidate in $exactCandidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    if (-not $AllowFallbackToAnyInstalled) {
        return $null
    }

    $installed = Get-InstalledUnityExecutables
    if ($installed.Count -gt 0) {
        return ($installed | Select-Object -First 1)
    }

    return $null
}

$unityPath = Resolve-UnityPath -Version $UnityVersion -AllowFallbackToAnyInstalled:$AllowAnyInstalled
if (-not $unityPath) {
    $installed = Get-InstalledUnityExecutables
    if ($installed.Count -gt 0) {
        $versions = $installed | ForEach-Object {
            $editorDir = Split-Path -Parent $_
            $versionDir = Split-Path -Parent $editorDir
            Split-Path -Leaf $versionDir
        } | Select-Object -Unique
        $installedText = ($versions -join ", ")
        Write-Error "Unity editor version '$UnityVersion' was not found. Installed versions detected: $installedText. Install the required version with scripts/install-toolchain.ps1 -UnityVersion $UnityVersion."
    }
    else {
        Write-Error "Unity editor not found. Expected version '$UnityVersion'. Run scripts/install-toolchain.ps1 first."
    }
}

Write-Output $unityPath
