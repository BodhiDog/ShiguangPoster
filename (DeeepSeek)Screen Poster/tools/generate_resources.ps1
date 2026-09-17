Add-Type -AssemblyName System.Drawing

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$res = Join-Path $root "resources"
$pics = Join-Path $root "pictures"

New-Item -ItemType Directory -Force -Path $res | Out-Null
New-Item -ItemType Directory -Force -Path $pics | Out-Null

function New-Canvas([int]$w, [int]$h) {
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    return @($bmp, $g)
}

function Add-HeartPath($path, [float]$cx, [float]$cy, [float]$size) {
    $p = { param([float]$x, [float]$y) [System.Drawing.PointF]::new(($cx + $x * $size), ($cy + $y * $size)) }
    $a = & $p 0.0 0.45
    $path.StartFigure()
    $path.AddBezier($a, (& $p -0.45 0.10), (& $p -0.50 -0.25), (& $p -0.28 -0.40))
    $path.AddBezier((& $p -0.28 -0.40), (& $p -0.10 -0.52), (& $p 0.0 -0.52), (& $p 0.0 -0.30))
    $path.AddBezier((& $p 0.0 -0.30), (& $p 0.0 -0.52), (& $p 0.10 -0.52), (& $p 0.28 -0.40))
    $path.AddBezier((& $p 0.28 -0.40), (& $p 0.50 -0.25), (& $p 0.45 0.10), (& $p 0.0 0.45))
    $path.CloseFigure()
}

# like1.png : heart outline (white)
$bmp, $g = New-Canvas 64 64
$g.Clear([System.Drawing.Color]::Transparent)
$hp = New-Object System.Drawing.Drawing2D.GraphicsPath
Add-HeartPath $hp 32 33 28
$pen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, 3)
$g.DrawPath($pen, $hp)
$bmp.Save((Join-Path $res "like1.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$pen.Dispose(); $hp.Dispose(); $g.Dispose(); $bmp.Dispose()

# like2.png : heart filled (red)
$bmp, $g = New-Canvas 64 64
$g.Clear([System.Drawing.Color]::Transparent)
$hp = New-Object System.Drawing.Drawing2D.GraphicsPath
Add-HeartPath $hp 32 33 28
$br = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 224, 60, 60))
$g.FillPath($br, $hp)
$bmp.Save((Join-Path $res "like2.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$br.Dispose(); $hp.Dispose(); $g.Dispose(); $bmp.Dispose()

# backstage.png : monitor icon 32x32
$bmp, $g = New-Canvas 32 32
$g.Clear([System.Drawing.Color]::Transparent)
$scr = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 70, 130, 200))
$g.FillRectangle($scr, 2, 3, 28, 20)
$inner = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 20, 40, 70))
$g.FillRectangle($inner, 4, 5, 24, 16)
$g.FillRectangle($scr, 13, 23, 6, 4)
$g.FillRectangle($scr, 9, 27, 14, 3)
$bmp.Save((Join-Path $res "backstage.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$scr.Dispose(); $inner.Dispose(); $g.Dispose(); $bmp.Dispose()

# sample pictures (gradients) so the gallery has content out of the box
function Save-Gradient([string]$name, [System.Drawing.Color]$c1, [System.Drawing.Color]$c2) {
    $bmp = New-Object System.Drawing.Bitmap(1280, 800)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $rect = New-Object System.Drawing.Rectangle(0, 0, 1280, 800)
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $c1, $c2, 45.0)
    $g.FillRectangle($brush, $rect)
    $bmp.Save((Join-Path $pics $name), [System.Drawing.Imaging.ImageFormat]::Png)
    $brush.Dispose(); $g.Dispose(); $bmp.Dispose()
}

Save-Gradient "sample_sunset.png" ([System.Drawing.Color]::FromArgb(255, 255, 140, 0)) ([System.Drawing.Color]::FromArgb(255, 139, 0, 0))
Save-Gradient "sample_night.png" ([System.Drawing.Color]::FromArgb(255, 11, 16, 38)) ([System.Drawing.Color]::FromArgb(255, 0, 0, 0))
Save-Gradient "sample_forest.png" ([System.Drawing.Color]::FromArgb(255, 46, 139, 87)) ([System.Drawing.Color]::FromArgb(255, 11, 61, 32))

Write-Output "Resources generated."
