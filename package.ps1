$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$versionFile = Join-Path $projectRoot 'VERSION'

if (-not (Test-Path -LiteralPath $versionFile)) { throw 'VERSION was not found.' }
$version = (Get-Content -LiteralPath $versionFile -Raw -Encoding UTF8).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') {
    throw 'VERSION must use major.minor.patch or major.minor.patch.build.'
}

& (Join-Path $projectRoot 'build.ps1') -Package -NoLoginStartup -Version $version
if ($LASTEXITCODE -ne 0) { throw 'Packaging failed.' }
