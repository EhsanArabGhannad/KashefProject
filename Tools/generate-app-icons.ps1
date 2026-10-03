# Rebuild the code-drawn Craftisma mark on Windows; no font or external image dependency.
Add-Type -AssemblyName System.Drawing
$iconRoot = Join-Path (Split-Path -Parent $PSScriptRoot) 'KashefProject/KashefProject/wwwroot/icons'
New-Item -ItemType Directory -Force -Path $iconRoot | Out-Null
foreach ($icon in @(
    @{ Name = 'icon-192.png'; Size = 192 },
    @{ Name = 'icon-512.png'; Size = 512 },
    @{ Name = 'icon-maskable-512.png'; Size = 512 },
    @{ Name = 'apple-touch-icon.png'; Size = 180 }
)) {
    $bitmap = [System.Drawing.Bitmap]::new($icon.Size, $icon.Size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $pen = [System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml('#c9ff3d'), [float]($icon.Size * 0.075))
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.Clear([System.Drawing.ColorTranslator]::FromHtml('#181818'))
        $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $graphics.DrawArc($pen, [float]($icon.Size * 0.28), [float]($icon.Size * 0.28), [float]($icon.Size * 0.44), [float]($icon.Size * 0.44), [float]45, [float]270)
        $bitmap.Save((Join-Path $iconRoot $icon.Name), [System.Drawing.Imaging.ImageFormat]::Png)
    } finally {
        $pen.Dispose()
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}
