param([string]$SourcePath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'Assets/Brand/craftisma-logo-source.png'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$webRoot = Join-Path (Split-Path -Parent $PSScriptRoot) 'KashefProject/KashefProject/wwwroot'
if (!(Test-Path -LiteralPath $SourcePath)) { throw 'Brand source PNG is missing.' }

# Deterministic resizing of the supplied artwork, not a redraw. Preserve its alpha.
if (!('CraftismaBrandAssets' -as [type])) {
    Add-Type -ReferencedAssemblies System.Drawing.Common, System.Drawing.Primitives, System.Private.Windows.GdiPlus, System.Private.Windows.Core -TypeDefinition @'
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public static class CraftismaBrandAssets
{
    static Rectangle Bounds(Bitmap source)
    {
        int left = source.Width, top = source.Height, right = -1, bottom = -1;
        for (int y = 0; y < source.Height; y++)
        for (int x = 0; x < source.Width; x++)
        {
            // Ignore nearly invisible export noise when finding the artwork bounds.
            if (source.GetPixel(x, y).A <= 16) continue;
            left = Math.Min(left, x); top = Math.Min(top, y);
            right = Math.Max(right, x); bottom = Math.Max(bottom, y);
        }
        if (right < left) throw new InvalidOperationException("The logo is entirely transparent.");
        return Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

    static Bitmap Render(Bitmap source, Rectangle bounds, int size, float fill, bool opaque)
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        using (var attributes = new ImageAttributes())
        {
            graphics.Clear(opaque ? Color.FromArgb(24, 24, 24) : Color.Transparent);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            attributes.SetWrapMode(WrapMode.TileFlipXY);
            float scale = size * fill / Math.Max(bounds.Width, bounds.Height);
            int width = Math.Max(1, (int)Math.Round(bounds.Width * scale));
            int height = Math.Max(1, (int)Math.Round(bounds.Height * scale));
            graphics.DrawImage(source, new Rectangle((size-width)/2, (size-height)/2, width, height),
                bounds.X, bounds.Y, bounds.Width, bounds.Height, GraphicsUnit.Pixel, attributes);
        }
        return bitmap;
    }

    static void Png(Bitmap source, Rectangle bounds, string path, int size, float fill, bool opaque)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (var bitmap = Render(source, bounds, size, fill, opaque)) bitmap.Save(path, ImageFormat.Png);
    }

    public static void Generate(string sourcePath, string webRoot)
    {
        using (var source = new Bitmap(sourcePath))
        {
            var bounds = Bounds(source);
            Png(source, bounds, Path.Combine(webRoot, "images/brand/craftisma-logo.png"), 256, .94f, false);
            Png(source, bounds, Path.Combine(webRoot, "icons/favicon-16.png"), 16, .94f, false);
            Png(source, bounds, Path.Combine(webRoot, "icons/favicon-32.png"), 32, .94f, false);
            Png(source, bounds, Path.Combine(webRoot, "icons/icon-192.png"), 192, .84f, false);
            Png(source, bounds, Path.Combine(webRoot, "icons/icon-512.png"), 512, .84f, false);
            // Keep the full square artwork inside the central mask-safe circle.
            Png(source, bounds, Path.Combine(webRoot, "icons/icon-maskable-512.png"), 512, .56f, true);
            Png(source, bounds, Path.Combine(webRoot, "icons/apple-touch-icon.png"), 180, .78f, true);
            int[] sizes = {16, 24, 32, 48, 64, 128, 256};
            byte[][] images = new byte[sizes.Length][];
            for (int i=0; i<sizes.Length; i++)
            using (var bitmap = Render(source, bounds, sizes[i], .94f, false))
            using (var stream = new MemoryStream())
            { bitmap.Save(stream, ImageFormat.Png); images[i] = stream.ToArray(); }
            using (var writer = new BinaryWriter(File.Create(Path.Combine(webRoot, "favicon.ico"))))
            {
                writer.Write((ushort)0); writer.Write((ushort)1); writer.Write((ushort)sizes.Length);
                uint offset = (uint)(6 + 16 * sizes.Length);
                for (int i=0; i<sizes.Length; i++)
                {
                    writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i])); writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i]));
                    writer.Write((byte)0); writer.Write((byte)0); writer.Write((ushort)1); writer.Write((ushort)32);
                    writer.Write((uint)images[i].Length); writer.Write(offset); offset += (uint)images[i].Length;
                }
                foreach (var bytes in images) writer.Write(bytes);
            }
        }
    }
}
'@
}
[CraftismaBrandAssets]::Generate((Resolve-Path -LiteralPath $SourcePath).Path, $webRoot)
Write-Output 'Generated logo, favicon (7 sizes), app and Apple icons from the supplied transparent PNG.'
