$ErrorActionPreference = 'Stop'

$releaseExe = Join-Path $PSScriptRoot 'ShiguangPoster.exe'
$releaseSaver = Join-Path $PSScriptRoot 'ShiguangPoster.scr'
$dataRoot = Join-Path $PSScriptRoot 'data'
$ownedPaths = @($releaseExe, $releaseSaver)

Get-Process -Name 'ShiguangPoster' -ErrorAction SilentlyContinue | ForEach-Object {
    $processPath = $null
    try { $processPath = $_.Path } catch { try { $processPath = $_.MainModule.FileName } catch { } }
    if ($processPath) {
        $fullPath = [IO.Path]::GetFullPath($processPath)
        $owned = $ownedPaths | Where-Object { [String]::Equals($fullPath, [IO.Path]::GetFullPath($_), [StringComparison]::OrdinalIgnoreCase) }
        if ($owned) { Stop-Process -Id $_.Id -Force }
    }
}

if (-not (Test-Path -LiteralPath $releaseExe)) {
    Write-Host '找不到 ShiguangPoster.exe，未执行卸载。'
    exit 1
}

& $releaseExe --uninstall
if ($LASTEXITCODE -ne 0) {
    if ($LASTEXITCODE -eq 2) {
        Write-Host '为保护现有 Windows 屏保设置，未删除拾光画报的数据文件夹。请从软件的“屏保设置”中停用屏保后再运行此脚本。'
    }
    exit $LASTEXITCODE
}

$desktopKey = 'HKCU:\Control Panel\Desktop'
$currentSaver = (Get-ItemProperty -Path $desktopKey -Name 'SCRNSAVE.EXE' -ErrorAction SilentlyContinue).'SCRNSAVE.EXE'
if ($currentSaver -and [String]::Equals(([string]$currentSaver).Trim('"'), $releaseSaver, [StringComparison]::OrdinalIgnoreCase)) {
    Write-Host 'Windows 仍在使用拾光画报屏保，数据已保留以避免影响系统设置。'
    exit 2
}

if (Test-Path -LiteralPath $dataRoot) { Remove-Item -LiteralPath $dataRoot -Recurse -Force }
Write-Host '已卸载拾光画报：已清理本程序的登录启动项、屏保注册和本地数据。原始照片不会被删除。'
