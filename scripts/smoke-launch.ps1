[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Executable,
    [string]$EvidencePath,
    [int]$StartupSeconds = 4,
    [int]$CloseTimeoutSeconds = 6
)

$ErrorActionPreference = "Stop"
$resolvedExecutable = (Resolve-Path -LiteralPath $Executable).Path
if (-not $EvidencePath) {
    $EvidencePath = Join-Path (Split-Path -Parent $resolvedExecutable) "launch-smoke.json"
}

function Stop-ProcessTree {
    param([System.Diagnostics.Process]$Process)

    if ($Process.HasExited) { return }
    try {
        $Process.Kill($true)
        $Process.WaitForExit(5000) | Out-Null
        return
    }
    catch {
        & taskkill.exe /PID $Process.Id /T /F 2>&1 | Out-Null
    }
}

function Read-StartupDiagnostics {
    param([string]$LocalAppData)

    $logDirectory = Join-Path $LocalAppData "MediaForge\Logs"
    if (-not (Test-Path -LiteralPath $logDirectory -PathType Container)) { return @() }
    return @(Get-ChildItem -LiteralPath $logDirectory -File -Filter "*.log" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTimeUtc |
        ForEach-Object {
            [ordered]@{
                name = $_.Name
                content = Get-Content -LiteralPath $_.FullName -Raw -ErrorAction SilentlyContinue
            }
        })
}

$cases = @(
    @{ Name = "absent-settings"; Content = $null; BlockPresetDirectory = $false },
    @{ Name = "valid-settings"; Content = '{"ParallelJobs":1,"CollisionPolicy":"Rename"}'; BlockPresetDirectory = $false },
    @{ Name = "malformed-settings"; Content = '{ malformed json'; BlockPresetDirectory = $false },
    @{ Name = "blocked-preset-storage"; Content = $null; BlockPresetDirectory = $true }
)
$results = @()
$root = Join-Path ([System.IO.Path]::GetTempPath()) ("MediaForge-LaunchSmoke-" + [Guid]::NewGuid().ToString("N"))

try {
    foreach ($case in $cases) {
        $caseRoot = Join-Path $root $case.Name
        $appData = Join-Path $caseRoot "AppData\Roaming"
        $localAppData = Join-Path $caseRoot "AppData\Local"
        $settingsDirectory = Join-Path $appData "MediaForge"
        New-Item -ItemType Directory -Path $settingsDirectory, $localAppData -Force | Out-Null

        $settingsPath = Join-Path $settingsDirectory "settings.json"
        if ($null -ne $case.Content) {
            Set-Content -LiteralPath $settingsPath -Value $case.Content -Encoding UTF8
        }
        if ([bool]$case.BlockPresetDirectory) {
            Set-Content -LiteralPath (Join-Path $settingsDirectory "Presets") -Value "blocked by launch smoke fixture" -Encoding ASCII
        }

        $startedUtc = [DateTime]::UtcNow
        $process = $null
        $status = "Failed"
        $detail = ""
        $mainWindowTitle = ""
        $mainWindowHandle = 0
        try {
            $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
            $startInfo.FileName = $resolvedExecutable
            $startInfo.WorkingDirectory = Split-Path -Parent $resolvedExecutable
            $startInfo.UseShellExecute = $false
            $startInfo.EnvironmentVariables["APPDATA"] = $appData
            $startInfo.EnvironmentVariables["LOCALAPPDATA"] = $localAppData
            $startInfo.EnvironmentVariables["MEDIAFORGE_APPDATA_ROOT"] = $appData
            $startInfo.EnvironmentVariables["MEDIAFORGE_LOCALAPPDATA_ROOT"] = $localAppData
            $startInfo.EnvironmentVariables["MEDIAFORGE_LAUNCH_SMOKE"] = "1"

            $process = [System.Diagnostics.Process]::new()
            $process.StartInfo = $startInfo
            if (-not $process.Start()) {
                throw "Process.Start returned false."
            }

            try { $process.WaitForInputIdle([Math]::Max(1000, $StartupSeconds * 1000)) | Out-Null } catch { }
            $startupDeadline = [DateTime]::UtcNow.AddSeconds($StartupSeconds)
            while ([DateTime]::UtcNow -lt $startupDeadline) {
                if ($process.HasExited) {
                    throw "Process exited during startup with code $($process.ExitCode)."
                }
                $process.Refresh()
                if ($process.MainWindowHandle -ne 0) {
                    $mainWindowHandle = $process.MainWindowHandle.ToInt64()
                    $mainWindowTitle = $process.MainWindowTitle
                }
                Start-Sleep -Milliseconds 100
            }

            if ($process.HasExited) {
                throw "Process exited during startup with code $($process.ExitCode)."
            }
            $process.Refresh()
            if ($process.MainWindowHandle -ne 0) {
                $mainWindowHandle = $process.MainWindowHandle.ToInt64()
                $mainWindowTitle = $process.MainWindowTitle
            }
            if ($mainWindowHandle -eq 0) {
                throw "No closeable top-level window appeared within $StartupSeconds seconds."
            }

            $closeRequested = $process.CloseMainWindow()
            if ($closeRequested) {
                if (-not $process.WaitForExit($CloseTimeoutSeconds * 1000)) {
                    Stop-ProcessTree -Process $process
                    throw "Main window did not close within $CloseTimeoutSeconds seconds."
                }
                if ($process.ExitCode -ne 0) {
                    throw "Process closed with exit code $($process.ExitCode)."
                }
                $status = "Passed"
                $detail = "Application presented '$mainWindowTitle', remained alive for $StartupSeconds seconds and closed through CloseMainWindow()."
            }
            else {
                Stop-ProcessTree -Process $process
                throw "CloseMainWindow could not close handle $mainWindowHandle ('$mainWindowTitle')."
            }
        }
        catch {
            $detail = $_.Exception.Message
            if ($null -ne $process) {
                Stop-ProcessTree -Process $process
            }
        }

        $results += [ordered]@{
            case = $case.Name
            status = $status
            detail = $detail
            startedUtc = $startedUtc.ToString("o")
            completedUtc = [DateTime]::UtcNow.ToString("o")
            isolatedAppData = $appData
            isolatedLocalAppData = $localAppData
            mainWindowHandle = $mainWindowHandle
            mainWindowTitle = $mainWindowTitle
            startupDiagnostics = @(Read-StartupDiagnostics -LocalAppData $localAppData)
        }
    }
}
finally {
    Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
}

$document = [ordered]@{
    schema = 2
    executable = $resolvedExecutable
    executableSha256 = (Get-FileHash -LiteralPath $resolvedExecutable -Algorithm SHA256).Hash.ToLowerInvariant()
    generatedUtc = [DateTime]::UtcNow.ToString("o")
    passed = -not ($results.status -contains "Failed")
    cases = $results
    isolation = "Each case uses MediaForge-specific roaming/local roots. Real user settings, presets, recent projects, recovery snapshots and startup logs are excluded."
    limitation = "This is a launch/close smoke test only; it does not prove interaction, conversion, preview, cancellation, output safety, codec support or accessibility."
}

$evidenceDirectory = Split-Path -Parent $EvidencePath
if ($evidenceDirectory) {
    New-Item -ItemType Directory -Path $evidenceDirectory -Force | Out-Null
}
$document | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $EvidencePath -Encoding UTF8
Write-Host "Launch smoke evidence: $EvidencePath"

if (-not $document.passed) {
    $failed = @($results | Where-Object status -eq "Failed" | ForEach-Object { "$($_.case): $($_.detail)" }) -join "; "
    throw "One or more launch smoke cases failed. $failed Evidence: $EvidencePath"
}
