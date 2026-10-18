$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Join-Path $PSScriptRoot '../KashefProject/KashefProject/wwwroot'
function Assert($value, $label) { if (!$value) { throw "FAIL: $label" }; Write-Output "PASS: $label" }
foreach ($asset in @(
    @{Path='images/brand/craftisma-logo.png';Size=256;Transparent=$true},
    @{Path='icons/favicon-16.png';Size=16;Transparent=$true},
    @{Path='icons/favicon-32.png';Size=32;Transparent=$true},
    @{Path='icons/icon-192.png';Size=192;Transparent=$true},
    @{Path='icons/icon-512.png';Size=512;Transparent=$true},
    @{Path='icons/icon-maskable-512.png';Size=512;Transparent=$false},
    @{Path='icons/apple-touch-icon.png';Size=180;Transparent=$false}
)) {
    $bitmap = [Drawing.Bitmap]::new((Join-Path $root $asset.Path))
    try {
        Assert ($bitmap.Width -eq $asset.Size -and $bitmap.Height -eq $asset.Size) "Correct dimensions: $($asset.Path)"
        Assert (($bitmap.GetPixel(0,0).A -eq 0) -eq $asset.Transparent) "Correct corner transparency: $($asset.Path)"
        Assert ($bitmap.GetPixel([int]($asset.Size/2),[int]($asset.Size/2)).A -gt 0) "Artwork present: $($asset.Path)"
    } finally { $bitmap.Dispose() }
}
$bytes = [IO.File]::ReadAllBytes((Join-Path $root 'favicon.ico'))
Assert ([BitConverter]::ToUInt16($bytes,0) -eq 0 -and [BitConverter]::ToUInt16($bytes,2) -eq 1 -and [BitConverter]::ToUInt16($bytes,4) -eq 7) 'Valid seven-size ICO header'
$sizes=@(16,24,32,48,64,128,256)
for ($index=0; $index -lt $sizes.Count; $index++) {
    $entry=6+16*$index
    $size=if ($bytes[$entry] -eq 0) { 256 } else { [int]$bytes[$entry] }
    $length=[BitConverter]::ToUInt32($bytes,$entry+8)
    $offset=[BitConverter]::ToUInt32($bytes,$entry+12)
    Assert ($size -eq $sizes[$index] -and $offset+$length -le $bytes.Length) "ICO directory entry: $size"
    $stream=[IO.MemoryStream]::new($bytes,[int]$offset,[int]$length)
    try {
        $bitmap=[Drawing.Bitmap]::new($stream)
        try { Assert ($bitmap.Width -eq $size -and $bitmap.Height -eq $size) "ICO PNG decodes: $size" }
        finally { $bitmap.Dispose() }
    } finally { $stream.Dispose() }
}
$worker=Get-Content (Join-Path $root 'service-worker.js') -Raw
Assert ($worker.Contains('${CACHE_PREFIX}v2')) 'Offline cache version updated for the new brand icon'
