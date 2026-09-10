[CmdletBinding()]
param(
    [string]$InnoCompiler
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot

function Resolve-InnoCompiler {
    param([string]$RequestedPath)

    if (-not [string]::IsNullOrWhiteSpace($RequestedPath)) {
        $expanded = [Environment]::ExpandEnvironmentVariables($RequestedPath)
        if (Test-Path -LiteralPath $expanded -PathType Leaf) {
            return (Resolve-Path -LiteralPath $expanded).Path
        }
        throw "Inno Setup 6 compiler was not found at the supplied path: $expanded"
    }

    $pathCommand = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
    if ($null -ne $pathCommand) {
        return $pathCommand.Source
    }

    $candidates = @()
    if (-not [string]::IsNullOrWhiteSpace(${env:ProgramFiles(x86)})) {
        $candidates += (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe")
    }
    if (-not [string]::IsNullOrWhiteSpace($env:ProgramFiles)) {
        $candidates += (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
    }

    foreach ($candidate in $candidates | Select-Object -Unique) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    $searched = if ($candidates.Count -gt 0) { $candidates -join "`n - " } else { "standard Program Files locations" }
    throw "Inno Setup 6 is required to build the installer. Install it or rerun with -InnoCompiler <path-to-ISCC.exe>. Searched:`n - $searched"
}

$resolvedInnoCompiler = Resolve-InnoCompiler -RequestedPath $InnoCompiler

# Do not spend time publishing unless the installer compiler is available.
& (Join-Path $PSScriptRoot "build-release.ps1") -Architecture x64

$publishedExecutable = Join-Path $projectRoot "artifacts\publish-win-x64\MediaForge.exe"
if (-not (Test-Path -LiteralPath $publishedExecutable -PathType Leaf)) {
    throw "The release build did not produce the expected executable: $publishedExecutable"
}

& $resolvedInnoCompiler (Join-Path $projectRoot "installer\MediaForge.iss")
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compilation failed with exit code $LASTEXITCODE."
}

Write-Host "Installer created under artifacts."
