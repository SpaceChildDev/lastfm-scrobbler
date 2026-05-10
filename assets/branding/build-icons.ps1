#Requires -Version 7
<#
.SYNOPSIS
    Render the Last.fm Font Awesome path into multi-resolution ICO files.
    No external dependencies — uses WPF (PresentationCore) for SVG path
    parsing + RenderTargetBitmap for rasterization, then assembles ICO
    bytes by hand (PNG-encoded sub-images, modern Windows compatible).
#>

Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase, System.Xaml

# Font Awesome's Last.fm path, viewBox 0 0 640 640.
$pathData = "M289.8 431.1L271 380.1C271 380.1 240.5 414.1 194.8 414.1C154.3 414.1 125.6 378.9 125.6 322.6C125.6 250.5 162 224.7 197.7 224.7C264.2 224.7 272.5 278 298.6 359.6C317.4 416.5 352.6 462.2 454 462.2C526.7 462.2 576 439.9 576 381.3C576 308.4 513.3 300.7 461 289.2C435.2 283.3 427.6 272.8 427.6 255.2C427.6 235.3 443.4 223.5 469.2 223.5C497.4 223.5 512.6 234.1 514.9 259.3L573.5 252.3C568.8 199.5 532.4 177.8 472.6 177.8C419.8 177.8 368.2 197.7 368.2 261.7C368.2 301.6 387.6 326.8 436.2 338.5C481.1 349.1 516 352.3 516 384.2C516 405.9 494.9 414.7 455 414.7C395.8 414.7 371.1 383.6 357.1 340.8C325.1 244 313.5 177.8 195.8 177.8C109.7 177.8 64 232.3 64 325C64 414.1 109.7 462.2 191.9 462.2C258.1 462.2 289.8 431.1 289.8 431.1z"

$red = [System.Windows.Media.Color]::FromRgb(0xBA, 0x00, 0x00)

function Render-Variant {
    param(
        [int]$Size,
        [bool]$WithBg,
        [System.Windows.Media.Color]$IconColor,
        [System.Windows.Media.Color]$BgColor
    )

    $visual = New-Object System.Windows.Media.DrawingVisual
    $dc     = $visual.RenderOpen()

    if ($WithBg) {
        $bgBrush = New-Object System.Windows.Media.SolidColorBrush $BgColor
        $bgBrush.Freeze()
        $r = $Size / 2.0
        $center = New-Object System.Windows.Point $r, $r
        $dc.DrawEllipse($bgBrush, $null, $center, $r, $r)
    }

    $geometry = [System.Windows.Media.Geometry]::Parse($pathData)

    # Path viewBox is 640x640; the actual content occupies roughly
    # x=64..576, y=177..462 — pad ~12% so the glyph fills the ICO nicely.
    $bounds = $geometry.Bounds
    $contentW = $bounds.Width
    $contentH = $bounds.Height
    $pad     = if ($WithBg) { 0.22 } else { 0.08 }
    $target  = $Size * (1 - 2 * $pad)
    $scale   = [Math]::Min($target / $contentW, $target / $contentH)

    $tx = ($Size - $contentW * $scale) / 2.0 - $bounds.X * $scale
    $ty = ($Size - $contentH * $scale) / 2.0 - $bounds.Y * $scale

    $tg = New-Object System.Windows.Media.TransformGroup
    $tg.Children.Add((New-Object System.Windows.Media.ScaleTransform $scale, $scale))
    $tg.Children.Add((New-Object System.Windows.Media.TranslateTransform $tx, $ty))

    $clone = $geometry.Clone()
    $clone.Transform = $tg
    $clone.Freeze()

    $brush = New-Object System.Windows.Media.SolidColorBrush $IconColor
    $brush.Freeze()
    $dc.DrawGeometry($brush, $null, $clone)
    $dc.Close()

    $rtb = New-Object System.Windows.Media.Imaging.RenderTargetBitmap $Size, $Size, 96, 96, ([System.Windows.Media.PixelFormats]::Pbgra32)
    $rtb.Render($visual)

    # Encode to PNG bytes
    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($rtb))
    $ms = New-Object System.IO.MemoryStream
    $encoder.Save($ms)
    return $ms.ToArray()
}

function Build-Ico {
    param([byte[][]]$Pngs, [int[]]$Sizes, [string]$Out)

    $count = $Pngs.Length
    $ms    = New-Object System.IO.MemoryStream
    $bw    = New-Object System.IO.BinaryWriter $ms

    # ICONDIR
    $bw.Write([uint16]0)        # reserved
    $bw.Write([uint16]1)        # type = ICO
    $bw.Write([uint16]$count)   # count

    $offset = 6 + 16 * $count
    for ($i = 0; $i -lt $count; $i++) {
        $sz   = $Sizes[$i]
        $w    = if ($sz -ge 256) { 0 } else { $sz }
        $bw.Write([byte]$w)             # width
        $bw.Write([byte]$w)             # height
        $bw.Write([byte]0)              # palette
        $bw.Write([byte]0)              # reserved
        $bw.Write([uint16]1)            # planes
        $bw.Write([uint16]32)           # bpp
        $bw.Write([uint32]$Pngs[$i].Length)
        $bw.Write([uint32]$offset)
        $offset += $Pngs[$i].Length
    }

    foreach ($png in $Pngs) { $bw.Write($png) }
    $bw.Flush()
    [System.IO.File]::WriteAllBytes($Out, $ms.ToArray())
    Write-Host "  wrote $Out ($([Math]::Round($ms.Length/1KB,1)) KB, $count sizes)" -ForegroundColor Green
}

$sizes = 16, 24, 32, 48, 64, 128, 256
$here  = $PSScriptRoot

# Variant A: red glyph on transparent background
Write-Host "Variant A — red on transparent" -ForegroundColor Cyan
$pngsA = $sizes | ForEach-Object {
    $b = Render-Variant -Size $_ -WithBg $false -IconColor $red -BgColor ([System.Windows.Media.Colors]::Transparent)
    Write-Host "  rendered $($_)x$($_) ($([Math]::Round($b.Length/1KB,1)) KB)"
    ,$b
}
Build-Ico -Pngs $pngsA -Sizes $sizes -Out (Join-Path $here "app-red-on-transparent.ico")

# Variant B: white glyph on red filled circle background
Write-Host "Variant B — white on red circle" -ForegroundColor Cyan
$white = [System.Windows.Media.Colors]::White
$pngsB = $sizes | ForEach-Object {
    $b = Render-Variant -Size $_ -WithBg $true -IconColor $white -BgColor $red
    Write-Host "  rendered $($_)x$($_) ($([Math]::Round($b.Length/1KB,1)) KB)"
    ,$b
}
Build-Ico -Pngs $pngsB -Sizes $sizes -Out (Join-Path $here "app-white-on-red-circle.ico")

# Also export 256px PNGs of each variant for previewing
[System.IO.File]::WriteAllBytes((Join-Path $here "preview-red-on-transparent.png"), $pngsA[-1])
[System.IO.File]::WriteAllBytes((Join-Path $here "preview-white-on-red-circle.png"), $pngsB[-1])

Write-Host ""
Write-Host "Done. Preview PNGs:" -ForegroundColor Green
Write-Host "  $here\preview-red-on-transparent.png"
Write-Host "  $here\preview-white-on-red-circle.png"
