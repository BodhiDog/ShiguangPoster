param([switch]$Verify, [switch]$Package, [switch]$NoLoginStartup, [string]$Version)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$versionFile = Join-Path $projectRoot 'VERSION'
if ([String]::IsNullOrWhiteSpace($Version)) {
    if (-not (Test-Path -LiteralPath $versionFile)) { throw 'VERSION was not found.' }
    $Version = (Get-Content -LiteralPath $versionFile -Raw -Encoding UTF8).Trim()
}
if ($Version -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') { throw 'Version must use major.minor.patch or major.minor.patch.build.' }
$assemblyVersion = if ($Version.Split('.').Count -eq 3) { $Version + '.0' } else { $Version }
$programPath = Join-Path $projectRoot 'src\Program.cs'
$programSource = [IO.File]::ReadAllText($programPath, [Text.Encoding]::UTF8)
if ($programSource -notmatch '\[assembly: AssemblyVersion\("[^"]+"\)\]') { throw 'Assembly version attributes were not found.' }
$versionedSource = $programSource -replace '(?m)(\[assembly: AssemblyVersion\(")[^"]+("\)\])', ('${1}' + $assemblyVersion + '${2}')
$versionedSource = $versionedSource -replace '(?m)(\[assembly: AssemblyFileVersion\(")[^"]+("\)\])', ('${1}' + $assemblyVersion + '${2}')
if ($versionedSource -ne $programSource) {
    $utf8 = New-Object System.Text.UTF8Encoding($false)
    [IO.File]::WriteAllText($programPath, $versionedSource, $utf8)
}
$frameworkRoot = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
if (-not (Test-Path -LiteralPath (Join-Path $frameworkRoot 'csc.exe'))) {
    $frameworkRoot = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319'
}
$compilerPath = Join-Path $frameworkRoot 'csc.exe'
if (-not (Test-Path -LiteralPath $compilerPath)) {
    throw '.NET Framework 4.8 compiler was not found. No software has been installed.'
}
$outputRoot = Join-Path $projectRoot 'dist\ShiguangPoster'
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
$references = @('System.dll','System.Core.dll','System.Xaml.dll','System.Web.Extensions.dll','System.Net.Http.dll','System.Drawing.dll','System.Windows.Forms.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll')
$compilerArgs = @('/nologo','/target:winexe','/platform:anycpu','/optimize+','/warn:4','/codepage:65001','/utf8output','/main:Shiguang.Program',('/out:' + (Join-Path $outputRoot 'ShiguangPoster.exe')),('/win32manifest:' + (Join-Path $projectRoot 'src\app.manifest')))
foreach ($reference in $references) { $compilerArgs += '/reference:' + (Join-Path $frameworkRoot $reference) }
foreach ($resourceFile in (Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.xaml')) {
    $compilerArgs += '/resource:' + $resourceFile.FullName + ',Shiguang.' + $resourceFile.Name
}
foreach ($assetFile in (Get-ChildItem -LiteralPath (Join-Path $projectRoot 'assets') -Filter '*.png')) {
    $compilerArgs += '/resource:' + $assetFile.FullName + ',Shiguang.Assets.' + $assetFile.Name
}
$compilerArgs += @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' | ForEach-Object { $_.FullName })
& $compilerPath @compilerArgs
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Copy-Item -LiteralPath (Join-Path $outputRoot 'ShiguangPoster.exe') -Destination (Join-Path $outputRoot 'ShiguangPoster.scr')
foreach ($name in @('ShiguangPoster.exe.config','ShiguangPoster.scr.config')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot 'src\App.config') -Destination (Join-Path $outputRoot $name)
}
foreach ($name in @('uninstall.ps1','uninstall.cmd')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot $name) -Destination (Join-Path $outputRoot $name)
}
if (Test-Path -LiteralPath (Join-Path $projectRoot 'README.md')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination (Join-Path $outputRoot 'README.md')
}
$docsPath = Join-Path $projectRoot 'docs'
if (Test-Path -LiteralPath $docsPath) {
    $outputDocs = Join-Path $outputRoot 'docs'
    New-Item -ItemType Directory -Force -Path $outputDocs | Out-Null
    foreach ($docFile in (Get-ChildItem -LiteralPath $docsPath -File)) {
        Copy-Item -LiteralPath $docFile.FullName -Destination (Join-Path $outputDocs $docFile.Name)
    }
}
$appPath = Join-Path $outputRoot 'ShiguangPoster.exe'
if (-not $NoLoginStartup) {
    $startupProcess = Start-Process -FilePath $appPath -ArgumentList '--enable-login-startup' -Wait -PassThru
    if ($startupProcess.ExitCode -ne 0) { throw 'Could not register login startup.' }
    Write-Output 'Registered: current-user login startup'
}
if ($Verify) {
    $verifyRoot = Join-Path $projectRoot 'artifacts\verification'
    New-Item -ItemType Directory -Force -Path $verifyRoot | Out-Null
    & $appPath --verify --data-dir $verifyRoot --output-dir $verifyRoot | Out-Null
    if ($LASTEXITCODE -ne 0) { throw ('Verification failed. See ' + $verifyRoot) }
    Get-Content -LiteralPath (Join-Path $verifyRoot 'verification.txt') -Encoding UTF8
}
Write-Output ('Built: ' + (Join-Path $outputRoot 'ShiguangPoster.exe'))
if ($Package) {
    $archivePath = Join-Path $projectRoot ('dist\ShiguangPoster-' + $Version + '-win.zip')
    Compress-Archive -LiteralPath $outputRoot -DestinationPath $archivePath -CompressionLevel Optimal -Force
    Write-Output ('Packaged: ' + $archivePath)
}
