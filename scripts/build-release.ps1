[CmdletBinding()]
param(
    [ValidateSet("x64", "arm64")]
    [string]$Architecture = "x64",
    [switch]$FrameworkDependent
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $projectRoot "MediaForge.csproj"
$runtime = "win-$Architecture"
$outputRoot = Join-Path $projectRoot "artifacts"
$publishDirectory = Join-Path $outputRoot "publish-$runtime"
$publishIntermediateRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("MediaForge-publish-$runtime-" + [Guid]::NewGuid().ToString("N"))
$maximumPublishAttempts = 3

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET 10 SDK is required. Install it from Microsoft, then rerun this script."
}

[xml]$project = Get-Content -LiteralPath $projectFile
$version = [string]$project.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "MediaForge.csproj does not declare a Version."
}

$zipPath = Join-Path $outputRoot "MediaForge-$version-$runtime.zip"
$zipHashPath = "$zipPath.sha256"
$launchSmokeEvidence = Join-Path $outputRoot "MediaForge-$version-$runtime-launch-smoke.json"

Remove-Item $publishDirectory -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $publishIntermediateRoot -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $zipPath, $zipHashPath, $launchSmokeEvidence -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

$selfContained = if ($FrameworkDependent) { "false" } else { "true" }

& dotnet restore $projectFile -r $runtime
if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed with exit code $LASTEXITCODE."
}

& dotnet build $projectFile -c Release -r $runtime --no-restore
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE."
}

$publishSucceeded = $false
$publishExitCode = -1
$successfulPublishDirectory = $null
for ($attempt = 1; $attempt -le $maximumPublishAttempts; $attempt++) {
    $attemptRoot = Join-Path $publishIntermediateRoot "attempt-$attempt"
    $attemptIntermediate = Join-Path $attemptRoot "obj"
    $attemptPublishDirectory = Join-Path $attemptRoot "publish"
    $intermediateOutputPath = $attemptIntermediate.TrimEnd([char]92, [char]47) + "/"

    Remove-Item $attemptRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $attemptIntermediate -Force | Out-Null
    New-Item -ItemType Directory -Path $attemptPublishDirectory -Force | Out-Null

    Write-Host "Publishing with isolated intermediates (attempt $attempt of $maximumPublishAttempts)..."
    & dotnet publish $projectFile -c Release -r $runtime --no-restore `
        --self-contained $selfContained `
        -p:PublishSingleFile=false `
        -p:IntermediateOutputPath=$intermediateOutputPath `
        -o $attemptPublishDirectory
    $publishExitCode = $LASTEXITCODE

    if ($publishExitCode -eq 0) {
        $publishSucceeded = $true
        $successfulPublishDirectory = $attemptPublishDirectory
        break
    }

    if ($attempt -lt $maximumPublishAttempts) {
        Write-Warning "dotnet publish attempt $attempt failed with exit code $publishExitCode. Retrying with a fresh isolated intermediate directory."
        Start-Sleep -Seconds 2
    }
}

if (-not $publishSucceeded -or [string]::IsNullOrWhiteSpace($successfulPublishDirectory)) {
    throw "dotnet publish failed after $maximumPublishAttempts attempt(s); last exit code $publishExitCode. Close any running MediaForge instance and retry."
}

if (Test-Path -LiteralPath $publishDirectory) {
    try {
        Remove-Item $publishDirectory -Recurse -Force -ErrorAction Stop
    }
    catch {
        throw "Publish succeeded, but the release staging directory could not be replaced: $publishDirectory. Close any running MediaForge instance and retry. $($_.Exception.Message)"
    }
}
New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null
Copy-Item -Path (Join-Path $successfulPublishDirectory "*") -Destination $publishDirectory -Recurse -Force

$publishedExecutable = Join-Path $publishDirectory "MediaForge.exe"
if (-not (Test-Path -LiteralPath $publishedExecutable -PathType Leaf)) {
    throw "Publish completed without producing the expected executable: $publishedExecutable"
}

Write-Host "Running isolated launch smoke test..."
& (Join-Path $PSScriptRoot "smoke-launch.ps1") `
    -Executable $publishedExecutable `
    -EvidencePath $launchSmokeEvidence
if ($LASTEXITCODE -ne 0) {
    throw "Launch smoke test failed with exit code $LASTEXITCODE. Evidence: $launchSmokeEvidence"
}

Copy-Item (Join-Path $projectRoot "README.md") $publishDirectory -Force
Copy-Item (Join-Path $projectRoot "LICENSE") $publishDirectory -Force
Copy-Item (Join-Path $projectRoot "THIRD_PARTY_NOTICES.md") $publishDirectory -Force

$payloadFiles = Get-ChildItem -LiteralPath $publishDirectory -File | Sort-Object Name
$payloadInventory = @($payloadFiles | ForEach-Object {
    [ordered]@{
        path = $_.Name
        size = $_.Length
        sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
})
$packageManifest = [ordered]@{
    schema = 1
    product = "MediaForge"
    version = $version
    runtime = $runtime
    selfContained = -not $FrameworkDependent.IsPresent
    singleFile = $false
    generatedUtc = [DateTime]::UtcNow.ToString("o")
    signingStatus = "unsigned"
    files = $payloadInventory
}
$packageManifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $publishDirectory "package-manifest.json") -Encoding UTF8

$hashLines = Get-ChildItem -LiteralPath $publishDirectory -File | Sort-Object Name | ForEach-Object {
    $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $($_.Name)"
}
$hashLines | Set-Content -LiteralPath (Join-Path $publishDirectory "SHA256SUMS.txt") -Encoding ASCII

Compress-Archive -Path (Join-Path $publishDirectory "*") -DestinationPath $zipPath -CompressionLevel Optimal
$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
"$zipHash  $(Split-Path -Leaf $zipPath)" | Set-Content -LiteralPath $zipHashPath -Encoding ASCII

Remove-Item $publishIntermediateRoot -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Release package: $zipPath"
Write-Host "Release SHA-256: $zipHash"
Write-Host "Launch smoke evidence: $launchSmokeEvidence"
