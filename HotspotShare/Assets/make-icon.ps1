Add-Type -AssemblyName System.Drawing

function New-IconFrame {
    param([int]$size)

    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.Clear([System.Drawing.Color]::Transparent)

    $rect = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $radius = $size * 0.26
    $d = $radius * 2
    $path.AddArc(0, 0, $d, $d, 180, 90)
    $path.AddArc($size - $d, 0, $d, $d, 270, 90)
    $path.AddArc($size - $d, $size - $d, $d, $d, 0, 90)
    $path.AddArc(0, $size - $d, $d, $d, 90, 90)
    $path.CloseFigure()

    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.Point(0,0)),
        (New-Object System.Drawing.Point($size,$size)),
        [System.Drawing.Color]::FromArgb(255, 0x8B, 0x5C, 0xF6),
        [System.Drawing.Color]::FromArgb(255, 0x22, 0xD3, 0xEE)
    )
    $blend = New-Object System.Drawing.Drawing2D.ColorBlend(3)
    $blend.Colors = @(
        [System.Drawing.Color]::FromArgb(255, 0x8B, 0x5C, 0xF6),
        [System.Drawing.Color]::FromArgb(255, 0xC0, 0x26, 0xD3),
        [System.Drawing.Color]::FromArgb(255, 0x22, 0xD3, 0xEE)
    )
    $blend.Positions = @(0.0, 0.5, 1.0)
    $brush.InterpolationColors = $blend

    $g.FillPath($brush, $path)

    # Wi-Fi style arcs (signal waves) in white, bottom-center anchored
    $cx = $size * 0.5
    $cy = $size * 0.66
    $penWidth = [Math]::Max(1.0, $size * 0.075)
    for ($i = 0; $i -lt 3; $i++) {
        $r = $size * (0.16 + $i * 0.155)
        $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(235,255,255,255), $penWidth)
        $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $arcRect = New-Object System.Drawing.RectangleF(($cx - $r), ($cy - $r), ($r * 2), ($r * 2))
        $g.DrawArc($pen, $arcRect, 200, 140)
        $pen.Dispose()
    }
    $dotR = $size * 0.052
    $dotBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $g.FillEllipse($dotBrush, ($cx - $dotR), ($cy - $dotR), ($dotR*2), ($dotR*2))

    $g.Dispose()
    return $bmp
}

$sizes = @(16, 32, 48, 64, 128, 256)
$assets = "C:\Users\telefonchy-programer\PhpstormProjects\HotspotShare\HotspotShare\Assets"
$pngStreams = @()

foreach ($s in $sizes) {
    $bmp = New-IconFrame -size $s
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngStreams += ,@{ Size = $s; Bytes = $ms.ToArray() }
    $bmp.Save("$assets\icon-$s.png", [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

# Build a valid .ico container embedding PNG frames (supported since Windows Vista)
$icoPath = "$assets\app.ico"
$fs = [System.IO.File]::Create($icoPath)
$writer = New-Object System.IO.BinaryWriter($fs)

$count = $pngStreams.Count
$writer.Write([UInt16]0)      # reserved
$writer.Write([UInt16]1)      # type: icon
$writer.Write([UInt16]$count) # image count

$headerSize = 6 + (16 * $count)
$offset = $headerSize

foreach ($entry in $pngStreams) {
    $s = $entry.Size
    $len = $entry.Bytes.Length
    $w = if ($s -ge 256) { 0 } else { $s }
    $h = if ($s -ge 256) { 0 } else { $s }
    $writer.Write([byte]$w)
    $writer.Write([byte]$h)
    $writer.Write([byte]0)    # color palette
    $writer.Write([byte]0)    # reserved
    $writer.Write([UInt16]1)  # color planes
    $writer.Write([UInt16]32) # bits per pixel
    $writer.Write([UInt32]$len)
    $writer.Write([UInt32]$offset)
    $offset += $len
}

foreach ($entry in $pngStreams) {
    $writer.Write($entry.Bytes)
}

$writer.Flush()
$writer.Close()
$fs.Close()

Write-Output "Icon created at $icoPath"
