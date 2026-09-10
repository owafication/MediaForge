[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$outputRoot = Join-Path $projectRoot "artifacts"
[xml]$project = Get-Content -LiteralPath (Join-Path $projectRoot "MediaForge.csproj")
$version = [string]$project.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "MediaForge.csproj does not declare a Version."
}

$zipPath = Join-Path $outputRoot "MediaForge-$version-source.zip"
$hashPath = "$zipPath.sha256"

New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
Remove-Item $zipPath, $hashPath -Force -ErrorAction SilentlyContinue

$items = Get-ChildItem $projectRoot -Force | Where-Object {
    $_.Name -notin @("bin", "obj", "artifacts", ".git", ".vs")
}
Compress-Archive -Path $items.FullName -DestinationPath $zipPath -CompressionLevel Optimal
$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $(Split-Path -Leaf $zipPath)" | Set-Content -LiteralPath $hashPath -Encoding ASCII
Write-Host "Source package: $zipPath"
Write-Host "Source SHA-256: $hash"
