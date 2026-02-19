Set-StrictMode -Version Latest

function Join-ProcessArguments {
    param(
        [string[]]$ArgumentList
    )

    if ($null -eq $ArgumentList -or $ArgumentList.Count -eq 0) {
        return ""
    }

    $parts = foreach ($argument in $ArgumentList) {
        if ($null -eq $argument) {
            '""'
            continue
        }

        if ($argument.Length -eq 0) {
            '""'
            continue
        }

        if ($argument -notmatch '[\s"]') {
            $argument
            continue
        }

        $escaped = $argument -replace '(\\*)"', '$1$1\"'
        $escaped = $escaped -replace '(\\+)$', '$1$1'
        '"' + $escaped + '"'
    }

    return ($parts -join " ")
}

function Stop-ProcessTreeSafe {
    param(
        [Parameter(Mandatory = $true)]
        [int]$ProcessId
    )

    if ($ProcessId -le 0) {
        return
    }

    $taskkillPath = Join-Path $env:WINDIR "System32\taskkill.exe"

    try {
        if (Test-Path $taskkillPath) {
            & $taskkillPath /PID $ProcessId /T /F | Out-Null
        }
        else {
            Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
        }
    }
    catch {
        Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
    }
}

function Invoke-ProcessWithTimeout {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [string[]]$ArgumentList = @(),

        [Parameter(Mandatory = $true)]
        [int]$TimeoutSeconds,

        [string]$WorkingDirectory,

        [switch]$NoNewWindow,

        [string]$OperationName
    )

    if ($TimeoutSeconds -lt 1) {
        throw "TimeoutSeconds must be >= 1."
    }

    if ([string]::IsNullOrWhiteSpace($OperationName)) {
        $OperationName = $FilePath
    }

    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $FilePath
    $startInfo.Arguments = Join-ProcessArguments -ArgumentList $ArgumentList
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $NoNewWindow.IsPresent

    if (-not [string]::IsNullOrWhiteSpace($WorkingDirectory)) {
        $startInfo.WorkingDirectory = $WorkingDirectory
    }

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    [void]$process.Start()
    $timeoutMilliseconds = $TimeoutSeconds * 1000

    if (-not $process.WaitForExit($timeoutMilliseconds)) {
        Stop-ProcessTreeSafe -ProcessId $process.Id
        throw "$OperationName timed out after $TimeoutSeconds seconds and was terminated."
    }

    $exitCode = $process.ExitCode
    $process.Close()
    return $exitCode
}
