param([Parameter(Mandatory = $true)][string]$Manifest)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore, WindowsBase
$canvasSize = 96
$glyphViewBoxSize = 93
$glyphScale = $glyphViewBoxSize / 512.0
$glyphInset = ($canvasSize - $glyphViewBoxSize) / 2.0
$icons = Get-Content -LiteralPath $Manifest -Raw | ConvertFrom-Json
$brush = New-Object System.Windows.Media.SolidColorBrush ([System.Windows.Media.Color]::FromRgb(215, 186, 115))
foreach ($icon in $icons) {
    $geometry = [System.Windows.Media.Geometry]::Parse([string]$icon.geometry)
    $transform = New-Object System.Windows.Media.TransformGroup
    [void]$transform.Children.Add((New-Object System.Windows.Media.ScaleTransform ($glyphScale, $glyphScale, 0, 0)))
    [void]$transform.Children.Add((New-Object System.Windows.Media.TranslateTransform ($glyphInset, $glyphInset)))
    $group = New-Object System.Windows.Media.GeometryGroup
    [void]$group.Children.Add($geometry)
    $group.Transform = $transform

    $visual = New-Object System.Windows.Media.DrawingVisual
    $context = $visual.RenderOpen()
    try { $context.DrawGeometry($brush, $null, $group) }
    finally { $context.Close() }

    $bitmap = New-Object System.Windows.Media.Imaging.RenderTargetBitmap ($canvasSize, $canvasSize, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    [void]$encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $parent = Split-Path -Parent ([string]$icon.output)
    [void][System.IO.Directory]::CreateDirectory($parent)
    $stream = [System.IO.File]::Open([string]$icon.output, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
    try { $encoder.Save($stream) }
    finally { $stream.Dispose() }
}
Write-Output "Rendered $($icons.Count) transparent ${canvasSize}x${canvasSize} Bannerlord sprite-part PNGs for marks up to 88px."
