[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ZipPath,
    [string]$ProjectRoot,
    [string]$EvidencePath
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        throw "ProjectRoot was not supplied and PSScriptRoot is unavailable."
    }

    $ProjectRoot = Split-Path -Parent $PSScriptRoot
}
$resolvedZip = (Resolve-Path -LiteralPath $ZipPath).Path
$resolvedRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
[xml]$project = Get-Content -LiteralPath (Join-Path $resolvedRoot "MediaForge.csproj")
$version = [string]$project.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "MediaForge.csproj does not declare a Version."
}
if ((Split-Path -Leaf $resolvedZip) -notmatch [regex]::Escape($version)) {
    throw "Release ZIP name does not include project version ${version}: $resolvedZip"
}

if (-not $EvidencePath) {
    $EvidencePath = "$resolvedZip.inventory.json"
}


function Get-RelativePackagePath {
    param([string]$Root, [string]$FullName)
    return $FullName.Substring($Root.Length).TrimStart([char[]]@([char]92, [char]47)).Replace('\', '/')
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("MediaForge-PackageAudit-" + [Guid]::NewGuid().ToString("N"))
try {
    Expand-Archive -LiteralPath $resolvedZip -DestinationPath $tempRoot -Force
    $files = Get-ChildItem -LiteralPath $tempRoot -Recurse -File
    $relativeFiles = @($files | ForEach-Object { Get-RelativePackagePath -Root $tempRoot -FullName $_.FullName })
    $required = @(
        "MediaForge.exe",
        "MediaForge.dll",
        "MediaForge.deps.json",
        "MediaForge.runtimeconfig.json",
        "README.md",
        "LICENSE",
        "THIRD_PARTY_NOTICES.md",
        "package-manifest.json",
        "SHA256SUMS.txt"
    )
    $missing = @($required | Where-Object { $_ -notin $relativeFiles })
    if ($missing.Count -gt 0) {
        throw "Release ZIP is missing required entries: $($missing -join ', ')"
    }

    $forbidden = @($relativeFiles | Where-Object {
        $_ -match '(^|/)(bin|obj|artifacts|\.git|\.vs)(/|$)' -or
        $_ -match '\.(mp4|mkv|mov|avi|mp3|wav|flac|png|jpe?g|webp)$'
    })
    if ($forbidden.Count -gt 0) {
        throw "Release ZIP contains forbidden generated/source-media entries: $($forbidden -join ', ')"
    }

    $manifest = Get-Content -LiteralPath (Join-Path $tempRoot "package-manifest.json") -Raw | ConvertFrom-Json
    if ([string]$manifest.version -ne $version) {
        throw "Package manifest version $($manifest.version) does not match project version $version."
    }
    if ([bool]$manifest.singleFile) {
        throw "Package manifest reports singleFile=true; Win64 release publishing must remain multi-file."
    }

    $inventory = @($files | Sort-Object FullName | ForEach-Object {
        [ordered]@{
            path = Get-RelativePackagePath -Root $tempRoot -FullName $_.FullName
            size = $_.Length
            sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    })

    $evidence = [ordered]@{
        schema = 1
        generatedUtc = [DateTime]::UtcNow.ToString("o")
        zipPath = $resolvedZip
        zipSize = (Get-Item -LiteralPath $resolvedZip).Length
        zipSha256 = (Get-FileHash -LiteralPath $resolvedZip -Algorithm SHA256).Hash.ToLowerInvariant()
        version = $version
        fileCount = $inventory.Count
        requiredEntries = $required
        files = $inventory
        passed = $true
    }
    $evidence | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $EvidencePath -Encoding UTF8
    Write-Host "Release package verification passed: $resolvedZip"
    Write-Host "Evidence: $EvidencePath"
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
