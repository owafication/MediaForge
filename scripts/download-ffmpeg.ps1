[CmdletBinding()]
param(
    [string]$InstallDirectory = (Join-Path $env:LOCALAPPDATA "MediaForge\tools"),
    [string]$ArchiveUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip",
    [string]$ChecksumUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip.sha256"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("MediaForge-FFmpeg-" + [Guid]::NewGuid().ToString("N"))
$archivePath = Join-Path $tempRoot "ffmpeg.zip"
$checksumPath = Join-Path $tempRoot "ffmpeg.zip.sha256"
$extractPath = Join-Path $tempRoot "extract"

try {
    New-Item -ItemType Directory -Path $tempRoot, $extractPath -Force | Out-Null
    Write-Host "Downloading FFmpeg archive..."
    Invoke-WebRequest -Uri $ArchiveUrl -OutFile $archivePath -UseBasicParsing
    Invoke-WebRequest -Uri $ChecksumUrl -OutFile $checksumPath -UseBasicParsing

    $checksumText = (Get-Content $checksumPath -Raw).Trim()
    $expectedHash = ([regex]::Match($checksumText, '[A-Fa-f0-9]{64}')).Value.ToUpperInvariant()
    if ([string]::IsNullOrWhiteSpace($expectedHash)) {
        throw "The published SHA-256 checksum could not be parsed."
    }

    $actualHash = (Get-FileHash -Path $archivePath -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($actualHash -ne $expectedHash) {
        throw "FFmpeg archive checksum mismatch. Expected $expectedHash but received $actualHash."
    }

    Write-Host "Checksum verified. Extracting..."
    Expand-Archive -Path $archivePath -DestinationPath $extractPath -Force

    $ffmpeg = Get-ChildItem -Path $extractPath -Filter ffmpeg.exe -Recurse -File | Select-Object -First 1
    $ffprobe = Get-ChildItem -Path $extractPath -Filter ffprobe.exe -Recurse -File | Select-Object -First 1
    if (-not $ffmpeg -or -not $ffprobe) {
        throw "The archive did not contain both ffmpeg.exe and ffprobe.exe."
    }

    New-Item -ItemType Directory -Path $InstallDirectory -Force | Out-Null
    Copy-Item $ffmpeg.FullName (Join-Path $InstallDirectory "ffmpeg.exe") -Force
    Copy-Item $ffprobe.FullName (Join-Path $InstallDirectory "ffprobe.exe") -Force

    $license = Get-ChildItem -Path $extractPath -File -Recurse |
        Where-Object { $_.Name -match '^(LICENSE|COPYING)(\..*)?$' } |
        Select-Object -First 1
    if ($license) {
        Copy-Item $license.FullName (Join-Path $InstallDirectory "FFmpeg-LICENSE.txt") -Force
    }

    $testOutput = & (Join-Path $InstallDirectory "ffmpeg.exe") -version 2>&1 | Select-Object -First 1
    if ($LASTEXITCODE -ne 0) {
        throw "The downloaded ffmpeg.exe did not pass its version check."
    }

    Write-Host "Installed FFmpeg tools to: $InstallDirectory"
    Write-Host $testOutput
}
finally {
    Remove-Item -Path $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
