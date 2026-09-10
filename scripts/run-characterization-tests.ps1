[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$EvidencePath
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $projectRoot "tests\MediaForge.Tests\MediaForge.Tests.csproj"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET 10 SDK is required. Install it from Microsoft, then rerun this script."
}
if (-not (Test-Path -LiteralPath $testProject -PathType Leaf)) {
    throw "Characterisation test project was not found: $testProject"
}

$output = [System.Collections.Generic.List[string]]::new()
function Invoke-DotnetStep {
    param([string[]]$Arguments)
    $lines = & dotnet @Arguments 2>&1
    foreach ($line in $lines) {
        $text = [string]$line
        $output.Add($text)
        Write-Host $text
    }
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

$startedUtc = [DateTime]::UtcNow
Invoke-DotnetStep -Arguments @("restore", $testProject)
Invoke-DotnetStep -Arguments @("build", $testProject, "-c", $Configuration, "--no-restore")
Invoke-DotnetStep -Arguments @("run", "--project", $testProject, "-c", $Configuration, "--no-build", "--")

if (-not [string]::IsNullOrWhiteSpace($EvidencePath)) {
    $parent = Split-Path -Parent $EvidencePath
    if ($parent) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
    [ordered]@{
        schema = 1
        report = "MediaForge PH-08 architecture characterisation tests"
        startedUtc = $startedUtc.ToString("o")
        completedUtc = [DateTime]::UtcNow.ToString("o")
        configuration = $Configuration
        project = $testProject
        passed = $true
        output = $output
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $EvidencePath -Encoding UTF8
}
