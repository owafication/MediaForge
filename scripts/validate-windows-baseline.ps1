[CmdletBinding()]
param(
    [ValidateSet("x64")]
    [string]$Architecture = "x64",
    [string]$SourceArchive,
    [string]$FfmpegPath,
    [string]$FfprobePath,
    [switch]$SkipLaunchSmoke,
    [switch]$BuildInstaller,
    [string]$InnoCompiler
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$timestamp = [DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss")
$evidenceRoot = Join-Path $projectRoot "artifacts\ph07-baseline-$timestamp"
New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
$transcriptPath = Join-Path $evidenceRoot "transcript.txt"
$steps = [System.Collections.Generic.List[object]]::new()

function Add-StepResult {
    param(
        [string]$Name,
        [string]$Validation,
        [string]$Status,
        [string]$Detail,
        [bool]$Mandatory,
        [DateTime]$StartedUtc
    )
    $steps.Add([ordered]@{
        name = $Name
        validation = $Validation
        status = $Status
        mandatory = $Mandatory
        detail = $Detail
        startedUtc = $StartedUtc.ToString("o")
        completedUtc = [DateTime]::UtcNow.ToString("o")
    })
}

function Invoke-EvidenceStep {
    param(
        [string]$Name,
        [string]$Validation,
        [bool]$Mandatory = $true,
        [scriptblock]$Action
    )

    $started = [DateTime]::UtcNow
    Write-Host "`n=== $Name ($Validation) ==="
    try {
        & $Action
        Add-StepResult -Name $Name -Validation $Validation -Status "Passed" -Detail "Completed without error." -Mandatory $Mandatory -StartedUtc $started
    }
    catch {
        $detail = $_.Exception.Message
        Add-StepResult -Name $Name -Validation $Validation -Status "Failed" -Detail $detail -Mandatory $Mandatory -StartedUtc $started
        Write-Warning "$Name failed: $detail"
    }
}

function Resolve-Python {
    $py = Get-Command "py.exe" -ErrorAction SilentlyContinue
    if ($py) { return @{ Exe = $py.Source; Prefix = @("-3") } }
    $python = Get-Command "python.exe" -ErrorAction SilentlyContinue
    if ($python) { return @{ Exe = $python.Source; Prefix = @() } }
    $python3 = Get-Command "python3.exe" -ErrorAction SilentlyContinue
    if ($python3) { return @{ Exe = $python3.Source; Prefix = @() } }
    throw "Python 3 is required for deterministic static checks and fixture generation."
}


function Invoke-PythonCommand {
    param([string[]]$Arguments)
    if (-not $script:python) { throw "Python was not resolved." }
    $commandArguments = @($script:python.Prefix) + @($Arguments)
    & $($script:python.Exe) @commandArguments
    return $LASTEXITCODE
}

function Resolve-ToolPath {
    param([string]$Requested, [string]$CommandName)
    if (-not [string]::IsNullOrWhiteSpace($Requested)) {
        return (Resolve-Path -LiteralPath $Requested).Path
    }
    $command = Get-Command $CommandName -ErrorAction SilentlyContinue
    if (-not $command) { throw "$CommandName was not found. Supply its path or add it to PATH." }
    return $command.Source
}

function Write-ManualChecklist {
    param([string]$Path)
    @"
# PH-07 manual Windows safety checklist

This checklist is mandatory evidence still requiring user-visible MediaForge interaction. Record exact inputs, outputs, hashes, exit states and observations in this evidence directory.

1. Queue one generated image, video and audio fixture together; verify job state, progress, output and log accuracy (`VAL-005`).
2. Hash every source before and after success, failure, skip, overwrite-target and cancellation (`VAL-031`, `VAL-063`).
3. Exercise Rename, Skip and Overwrite with an existing target and with two jobs resolving to the same destination (`VAL-007`).
4. Test beside-source, explicit output root and preserved folder tree; confirm no output escapes the selected root (`VAL-008`).
5. Cancel during probe/conversion and close during work; inspect child processes, temporary files and final output ambiguity (`VAL-006`).
6. Test invalid ratios, dimensions, quality, CRF, FPS and bitrate; confirm visible rejection or correction (`VAL-009`).
7. Test absent, valid and malformed settings plus unwritable AppData (`VAL-004`, `VAL-011`).
8. Test Unicode, dot-prefixed, long, locked and unwritable paths plus a reparse/junction tree (`VAL-019`).
9. Confirm stream-copy/extract-audio incompatibilities are blocked when edits require processing (`VAL-010`).
10. Record limitations, skipped combinations and rollback artefact before any release claim.

Generated fixtures: `ph07-fixtures/fixture-manifest.json`.
"@ | Set-Content -LiteralPath $Path -Encoding UTF8
}

Start-Transcript -LiteralPath $transcriptPath -Force | Out-Null
try {
    [xml]$project = Get-Content -LiteralPath (Join-Path $projectRoot "MediaForge.csproj")
    $version = [string]$project.Project.PropertyGroup.Version
    $runtime = "win-$Architecture"
    $zipPath = Join-Path $projectRoot "artifacts\MediaForge-$version-$runtime.zip"
    $publishDirectory = Join-Path $projectRoot "artifacts\publish-$runtime"

    $environment = [ordered]@{
        schema = 1
        generatedUtc = [DateTime]::UtcNow.ToString("o")
        machineName = $env:COMPUTERNAME
        osVersion = [Environment]::OSVersion.VersionString
        is64BitOperatingSystem = [Environment]::Is64BitOperatingSystem
        is64BitProcess = [Environment]::Is64BitProcess
        powershellVersion = $PSVersionTable.PSVersion.ToString()
        powershellEdition = $PSVersionTable.PSEdition
        projectRoot = $projectRoot
        productVersion = $version
        runtime = $runtime
    }
    if ($SourceArchive) {
        $resolvedArchive = (Resolve-Path -LiteralPath $SourceArchive).Path
        $environment.sourceArchive = $resolvedArchive
        $environment.sourceArchiveSha256 = (Get-FileHash -LiteralPath $resolvedArchive -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    $environment | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $evidenceRoot "environment.json") -Encoding UTF8

    Invoke-EvidenceStep -Name "Repository state capture" -Validation "VAL-017/VAL-065" -Action {
        $git = Get-Command git.exe -ErrorAction SilentlyContinue
        if (-not $git) { throw "Git was not found." }
        Push-Location $projectRoot
        try {
            & $git.Source status --short --branch 2>&1 | Set-Content -LiteralPath (Join-Path $evidenceRoot "git-status.txt") -Encoding UTF8
            & $git.Source branch --show-current 2>&1 | Set-Content -LiteralPath (Join-Path $evidenceRoot "git-branch.txt") -Encoding UTF8
            & $git.Source remote -v 2>&1 | Set-Content -LiteralPath (Join-Path $evidenceRoot "git-remotes.txt") -Encoding UTF8
            & $git.Source check-ignore -v bin obj artifacts .vs 2>&1 | Set-Content -LiteralPath (Join-Path $evidenceRoot "git-ignore.txt") -Encoding UTF8
        }
        finally { Pop-Location }
    }

    $python = $null
    Invoke-EvidenceStep -Name "Resolve Python" -Validation "VAL-058" -Action {
        $script:python = Resolve-Python
        $exitCode = Invoke-PythonCommand -Arguments @("--version")
        if ($exitCode -ne 0) { throw "Python version check failed with exit code $exitCode." }
    }

    Invoke-EvidenceStep -Name "Static source verification" -Validation "VAL-001/VAL-002/VAL-017/VAL-018/VAL-065" -Action {
        if (-not $script:python) { throw "Python was not resolved." }
        $exitCode = Invoke-PythonCommand -Arguments @((Join-Path $PSScriptRoot "verify-source.py"), "--root", $projectRoot, "--json", (Join-Path $evidenceRoot "source-audit.json"))
        if ($exitCode -ne 0) { throw "Static source verification failed with exit code $exitCode." }
    }

    $ffmpeg = $null
    $ffprobe = $null
    Invoke-EvidenceStep -Name "FFmpeg provenance" -Validation "VAL-012" -Action {
        $script:ffmpeg = Resolve-ToolPath -Requested $FfmpegPath -CommandName "ffmpeg.exe"
        $script:ffprobe = Resolve-ToolPath -Requested $FfprobePath -CommandName "ffprobe.exe"
        $ffmpegVersion = & $script:ffmpeg -version 2>&1
        if ($LASTEXITCODE -ne 0) { throw "FFmpeg version check failed with exit code $LASTEXITCODE." }
        $ffprobeVersion = & $script:ffprobe -version 2>&1
        if ($LASTEXITCODE -ne 0) { throw "FFprobe version check failed with exit code $LASTEXITCODE." }
        [ordered]@{
            ffmpegPath = $script:ffmpeg
            ffmpegSha256 = (Get-FileHash -LiteralPath $script:ffmpeg -Algorithm SHA256).Hash.ToLowerInvariant()
            ffmpegVersion = @($ffmpegVersion | Select-Object -First 4)
            ffprobePath = $script:ffprobe
            ffprobeSha256 = (Get-FileHash -LiteralPath $script:ffprobe -Algorithm SHA256).Hash.ToLowerInvariant()
            ffprobeVersion = @($ffprobeVersion | Select-Object -First 4)
        } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $evidenceRoot "tool-provenance.json") -Encoding UTF8
    }

    Invoke-EvidenceStep -Name "Generate non-sensitive fixtures" -Validation "VAL-058" -Action {
        if (-not $script:python) { throw "Python was not resolved." }
        if (-not $script:ffmpeg -or -not $script:ffprobe) { throw "FFmpeg tools were not resolved." }
        $exitCode = Invoke-PythonCommand -Arguments @((Join-Path $PSScriptRoot "generate-test-fixtures.py"), "--output", (Join-Path $evidenceRoot "ph07-fixtures"), "--ffmpeg", $script:ffmpeg, "--ffprobe", $script:ffprobe)
        if ($exitCode -ne 0) { throw "Fixture generation failed with exit code $exitCode." }
    }

    Invoke-EvidenceStep -Name ".NET environment" -Validation "VAL-003/VAL-030" -Action {
        $dotnet = Get-Command dotnet.exe -ErrorAction SilentlyContinue
        if (-not $dotnet) { throw ".NET SDK was not found." }
        & $dotnet.Source --info 2>&1 | Tee-Object -FilePath (Join-Path $evidenceRoot "dotnet-info.txt")
        if ($LASTEXITCODE -ne 0) { throw "dotnet --info failed with exit code $LASTEXITCODE." }
    }

    Invoke-EvidenceStep -Name "PH-08 architecture characterisation tests" -Validation "VAL-032/VAL-058/VAL-063" -Action {
        & (Join-Path $PSScriptRoot "run-characterization-tests.ps1") -Configuration Release -EvidencePath (Join-Path $evidenceRoot "characterisation-tests.json")
    }

    Invoke-EvidenceStep -Name "Restore, build and publish x64" -Validation "VAL-003/VAL-013/VAL-030" -Action {
        & (Join-Path $PSScriptRoot "build-release.ps1") -Architecture $Architecture
        if (-not (Test-Path -LiteralPath $zipPath -PathType Leaf)) { throw "Expected release ZIP was not created: $zipPath" }
    }

    Invoke-EvidenceStep -Name "Release package audit" -Validation "VAL-013/VAL-018/VAL-059" -Action {
        if (-not (Test-Path -LiteralPath $zipPath -PathType Leaf)) { throw "Release ZIP is unavailable because publish did not complete." }
        & (Join-Path $PSScriptRoot "verify-release-package.ps1") -ZipPath $zipPath -ProjectRoot $projectRoot -EvidencePath (Join-Path $evidenceRoot "release-package-audit.json")
    }

    if ($SkipLaunchSmoke) {
        Add-StepResult -Name "Launch/settings smoke" -Validation "VAL-004/VAL-030" -Status "Skipped" -Detail "Skipped by -SkipLaunchSmoke." -Mandatory $true -StartedUtc ([DateTime]::UtcNow)
    }
    else {
        Invoke-EvidenceStep -Name "Launch/settings smoke" -Validation "VAL-004/VAL-030" -Action {
            $executable = Join-Path $publishDirectory "MediaForge.exe"
            if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) { throw "Published executable is unavailable: $executable" }
            & (Join-Path $PSScriptRoot "smoke-launch.ps1") -Executable $executable -EvidencePath (Join-Path $evidenceRoot "launch-smoke.json")
        }
    }

    if ($BuildInstaller) {
        Invoke-EvidenceStep -Name "Installer build" -Validation "VAL-015/VAL-059" -Mandatory $false -Action {
            & (Join-Path $PSScriptRoot "build-installer.ps1") -InnoCompiler $InnoCompiler
        }
    }
    else {
        Add-StepResult -Name "Installer build" -Validation "VAL-015/VAL-059" -Status "Skipped" -Detail "Installer was not requested." -Mandatory $false -StartedUtc ([DateTime]::UtcNow)
    }

    Write-ManualChecklist -Path (Join-Path $evidenceRoot "MANUAL-SAFETY-CHECKLIST.md")

    $mandatoryFailures = @($steps | Where-Object { $_.mandatory -and $_.status -eq "Failed" })
    $mandatorySkipped = @($steps | Where-Object { $_.mandatory -and $_.status -eq "Skipped" })
    $summary = [ordered]@{
        schema = 1
        report = "PH-07 native baseline evidence"
        generatedUtc = [DateTime]::UtcNow.ToString("o")
        projectRoot = $projectRoot
        evidenceRoot = $evidenceRoot
        version = $version
        passed = ($mandatoryFailures.Count -eq 0 -and $mandatorySkipped.Count -eq 0)
        steps = $steps
        unproven = @(
            "Manual source immutability, collision, destination containment, cancellation and temp-cleanup fixtures remain pending until MANUAL-SAFETY-CHECKLIST.md is completed.",
            "A successful launch smoke does not prove editor interaction, conversion correctness, codec support, accessibility or installer behaviour.",
            "ARM64 and installer are not claimed unless explicitly run and evidenced."
        )
    }
    $summary | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $evidenceRoot "summary.json") -Encoding UTF8

    $stepLines = $steps | ForEach-Object { "| $($_.validation) | $($_.name) | $($_.status) | $($_.detail.Replace('|', '\|')) |" }
    @"
# PH-07 native baseline evidence

- Generated UTC: $($summary.generatedUtc)
- Product version: $version
- Runtime: $runtime
- Overall automated status: $(if ($summary.passed) { 'Passed' } else { 'Not passed' })
- Evidence root: `$evidenceRoot`

| Validation | Step | Status | Detail |
|---|---|---|---|
$($stepLines -join "`n")

## Unproven

- Manual source/output/collision/cancellation/filesystem fixtures remain pending; complete `MANUAL-SAFETY-CHECKLIST.md`.
- This harness does not promote static or launch-smoke results into a full application safety pass.
- ARM64 and installer remain unclaimed unless separately executed and recorded.
"@ | Set-Content -LiteralPath (Join-Path $evidenceRoot "SUMMARY.md") -Encoding UTF8

    Write-Host "`nEvidence root: $evidenceRoot"
    if (-not $summary.passed) {
        throw "PH-07 automated baseline did not pass. Inspect SUMMARY.md and transcript.txt."
    }
}
finally {
    Stop-Transcript | Out-Null
}
