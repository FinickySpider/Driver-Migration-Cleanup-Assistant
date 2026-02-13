# Generate a basic application icon for DMCA
# This creates a simple 256x256 PNG that can be converted to .ico

Add-Type -AssemblyName System.Drawing

# Create a 256x256 bitmap
$bitmap = New-Object System.Drawing.Bitmap 256, 256
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias

# Background gradient (blue to dark blue)
$gradientBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.Point 0, 0),
    (New-Object System.Drawing.Point 0, 256),
    [System.Drawing.Color]::FromArgb(41, 128, 185),
    [System.Drawing.Color]::FromArgb(20, 64, 92)
)
$graphics.FillRectangle($gradientBrush, 0, 0, 256, 256)

# Draw a stylized "D" for Driver cleanup
$font = New-Object System.Drawing.Font("Segoe UI", 140, [System.Drawing.FontStyle]::Bold)
$brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$format = New-Object System.Drawing.StringFormat
$format.Alignment = [System.Drawing.StringAlignment]::Center
$format.LineAlignment = [System.Drawing.StringAlignment]::Center
$graphics.DrawString("D", $font, $brush, 128, 128, $format)

# Save as PNG first
$pngPath = "$PSScriptRoot\app_icon_temp.png"
$bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)

Write-Host "Generated PNG icon at: $pngPath"
Write-Host ""
Write-Host "To convert to .ico (multiple sizes):"
Write-Host "1. Install ImageMagick: winget install ImageMagick.ImageMagick"
Write-Host "2. Run: magick convert app_icon_temp.png -define icon:auto-resize=256,128,64,48,32,16 app.ico"
Write-Host ""
Write-Host "Or use an online converter:"
Write-Host "- https://convertio.co/png-ico/"
Write-Host "- https://cloudconvert.com/png-to-ico"
Write-Host ""
Write-Host "For a professional icon, use a tool like:"
Write-Host "- Figma, Inkscape, or Adobe Illustrator (design)"
Write-Host "- IconWorkshop or IcoFX (icon creation)"

# Cleanup
$graphics.Dispose()
$bitmap.Dispose()
