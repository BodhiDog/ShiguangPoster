$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$versionFile = Join-Path $projectRoot 'VERSION'
$distRoot = Join-Path $projectRoot 'dist'
$temporaryOutput = Join-Path $projectRoot 'artifacts\package\ShiguangPoster'

if (-not (Test-Path -LiteralPath $versionFile)) { throw 'VERSION was not found.' }
$version = (Get-Content -LiteralPath $versionFile -Raw -Encoding UTF8).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') {
    throw 'VERSION must use major.minor.patch or major.minor.patch.build.'
}

if (Test-Path -LiteralPath $temporaryOutput) { Remove-Item -LiteralPath $temporaryOutput -Recurse -Force }
& (Join-Path $projectRoot 'build.ps1') -Package -NoLoginStartup -Version $version -OutputDirectory $temporaryOutput
if ($LASTEXITCODE -ne 0) { throw 'Packaging failed.' }

if (Test-Path -LiteralPath $temporaryOutput) { Remove-Item -LiteralPath $temporaryOutput -Recurse -Force }
Get-ChildItem -LiteralPath $distRoot -Force | Where-Object { $_.PSIsContainer -or $_.Extension -ne '.zip' } | Remove-Item -Recurse -Force
