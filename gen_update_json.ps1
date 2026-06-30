param (
    [string]$targetPath,
    [string]$targetDir,
    [string]$configPath,
    [string]$url,
    [string]$changelog
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-ArchiveSourcePaths {
    param (
        [Parameter(Mandatory = $true)]
        [string]$RawTargetPath
    )

    $raw = $RawTargetPath.Trim()
    if ([string]::IsNullOrWhiteSpace($raw)) {
        throw "targetPath is empty."
    }

    if (Test-Path -LiteralPath $raw) {
        return ,(Resolve-Path -LiteralPath $raw).Path
    }

    $candidates = @()

    if ($raw.Contains(";")) {
        $candidates = $raw.Split(";") |
            ForEach-Object { $_.Trim() } |
            Where-Object { $_ }
    }
    else {
        $matches = [regex]::Matches($raw, '[A-Za-z]:\\.*?(?=(?:\s+[A-Za-z]:\\)|$)')
        if ($matches.Count -gt 0) {
            $candidates = $matches |
                ForEach-Object { $_.Value.Trim() } |
                Where-Object { $_ }
        }
    }

    if ($candidates.Count -eq 0) {
        throw "Unable to parse targetPath: $RawTargetPath"
    }

    $resolved = @()
    foreach ($candidate in $candidates) {
        if (-not (Test-Path -LiteralPath $candidate)) {
            throw "Archive source does not exist: $candidate"
        }

        $resolved += (Resolve-Path -LiteralPath $candidate).Path
    }

    return $resolved
}

function Get-RelativeArchivePath {
    param (
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(Mandatory = $true)]
        [string]$BaseDir
    )

    $normalizedBase = [IO.Path]::GetFullPath($BaseDir)
    if (-not $normalizedBase.EndsWith([IO.Path]::DirectorySeparatorChar)) {
        $normalizedBase += [IO.Path]::DirectorySeparatorChar
    }

    $normalizedFile = [IO.Path]::GetFullPath($FilePath)

    if ($normalizedFile.StartsWith($normalizedBase, [StringComparison]::OrdinalIgnoreCase)) {
        return $normalizedFile.Substring($normalizedBase.Length)
    }

    return [IO.Path]::GetFileName($normalizedFile)
}

$archiveSources = Get-ArchiveSourcePaths -RawTargetPath $targetPath
$resolvedTargetDir = (Resolve-Path -LiteralPath $targetDir).Path
$zipPath = Join-Path -Path $resolvedTargetDir -ChildPath "SglDesigner.zip"
$stageRoot = Join-Path -Path ([IO.Path]::GetTempPath()) -ChildPath ("sgldesigner_zip_" + [guid]::NewGuid().ToString("N"))

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

New-Item -ItemType Directory -Path $stageRoot | Out-Null

try {
    foreach ($source in $archiveSources) {
        $entryPath = Get-RelativeArchivePath -FilePath $source -BaseDir $resolvedTargetDir
        $stagePath = Join-Path -Path $stageRoot -ChildPath $entryPath
        $stageParent = Split-Path -Parent $stagePath

        if (-not (Test-Path -LiteralPath $stageParent)) {
            New-Item -ItemType Directory -Path $stageParent -Force | Out-Null
        }

        Copy-Item -LiteralPath $source -Destination $stagePath -Force
    }

    $archiveItems = Get-ChildItem -LiteralPath $stageRoot -Force
    if ($archiveItems.Count -eq 0) {
        throw "No files were staged for compression."
    }

    Write-Host "Compressing $($archiveSources -join '  ') to $zipPath..."
    Compress-Archive -Path ($archiveItems.FullName) -DestinationPath $zipPath
}
finally {
    if (Test-Path -LiteralPath $stageRoot) {
        Remove-Item -LiteralPath $stageRoot -Recurse -Force
    }
}

$versionSource = $archiveSources |
    Where-Object { $_.EndsWith(".exe", [StringComparison]::OrdinalIgnoreCase) } |
    Select-Object -First 1

if (-not $versionSource) {
    throw "No EXE file was found in targetPath, cannot determine file version."
}

$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($versionSource).FileVersion
$sha1 = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA1).Hash

$config = [ordered]@{
    version = $version
    url = $url
    changelog = $changelog
    mandatory = [ordered]@{
        value = $false
        minVersion = "1.0.0.0"
        mode = 1
    }
    checksum = [ordered]@{
        value = $sha1
        hashingAlgorithm = "SHA1"
    }
}

$config | ConvertTo-Json -Depth 5 | Out-File -FilePath $configPath -Encoding utf8
Write-Host "Success: ZIP created and JSON updated."
