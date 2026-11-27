// SPDX-License-Identifier: GPL-3.0-or-later
using Microsoft.UI.Xaml.Media.Imaging;
using Otis.Core.Building;
using Otis.Core.Imaging;
using Otis.Core.Projects;
using Otis.Core.Tracing;
using SkiaSharp;
using Windows.Storage.Streams;

namespace Otis.App.Services;

public static class Images
{
    public static async Task<BitmapImage> FromPngAsync(byte[] png)
    {
        using var stream = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
        {
            writer.WriteBytes(png);
            await writer.StoreAsync();
            writer.DetachStream();
        }

        var image = new BitmapImage();
        await image.SetSourceAsync(stream);
        return image;
    }

    public static async Task<BitmapImage> FromFileAsync(string path) => await FromPngAsync(await File.ReadAllBytesAsync(path));

    // Ink is shown black on white, with the baseline and x-line drawn across, as the build will cut it out.
    public static byte[] InkPng(BitGrid mask, int? baselineRow = null, int? xLineRow = null)
    {
        var width = Math.Max(1, mask.Width);
        var height = Math.Max(1, mask.Height);
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        var pixels = bitmap.GetPixelSpan();
        for (var y = 0; y < mask.Height; y++)
        {
            for (var x = 0; x < mask.Width; x++)
            {
                var i = ((y * width) + x) * 4;
                var value = mask[x, y] ? (byte)20 : (byte)255;
                pixels[i] = pixels[i + 1] = pixels[i + 2] = value;
                pixels[i + 3] = 255;
            }
        }

        using (var canvas = new SKCanvas(bitmap))
        {
            DrawLine(canvas, baselineRow, width, new SKColor(0, 160, 0));
            DrawLine(canvas, xLineRow, width, new SKColor(42, 90, 255));
        }

        using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return png.ToArray();
    }

    public static RgbImage DecodeRgb(string png)
    {
        using var bitmap = SKBitmap.Decode(png);
        return Otis.Infrastructure.Pages.Bitmaps.ToRgb(bitmap);
    }

    public static byte[] CropPng(RgbImage page, PixelRect box, int size = 96)
    {
        var area = page.Clip(box);
        using var crop = Otis.Infrastructure.Pages.Bitmaps.ToBitmap(page.Crop(area));
        var zoom = Math.Min(1f, Math.Min((float)size / crop.Width, (float)size / crop.Height));
        using var scaled = crop.Resize(new SKImageInfo(Math.Max(1, (int)(crop.Width * zoom)), Math.Max(1, (int)(crop.Height * zoom))), new SKSamplingOptions(SKFilterMode.Linear));
        using var png = scaled.Encode(SKEncodedImageFormat.Png, 100);
        return png.ToArray();
    }

    // The traced curves, filled as the font will draw them, at twice the scan resolution.
    public static byte[] OutlinePng(TracedCutout traced, Cutout cutout)
    {
        const float Zoom = 2;
        var width = Math.Max(1, (int)(traced.Ink.Mask.Width * Zoom));
        var height = Math.Max(1, (int)(traced.Ink.Mask.Height * Zoom));
        SKPoint Local(TracePoint p)
        {
            var page = traced.PagePoint(p);
            return new SKPoint((float)(page.X - traced.Ink.Left) * Zoom, (float)(page.Y - traced.Ink.Top) * Zoom);
        }

        using var builder = new SKPathBuilder { FillType = SKPathFillType.EvenOdd };
        foreach (var contour in traced.Contours)
        {
            builder.MoveTo(Local(contour.Start));
            foreach (var s in contour.Segments)
            {
                if (s.IsCorner)
                {
                    builder.LineTo(Local(s.C1));
                    builder.LineTo(Local(s.End));
                }
                else
                {
                    builder.CubicTo(Local(s.C1), Local(s.C2), Local(s.End));
                }
            }

            builder.Close();
        }

        using var path = builder.Detach();
        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            using var fill = new SKPaint { Color = new SKColor(20, 20, 20), IsAntialias = true };
            canvas.DrawPath(path, fill);
            DrawLine(canvas, (int)((cutout.Baseline - traced.Ink.Top) * Zoom), width, new SKColor(0, 160, 0));
            DrawLine(canvas, (int)((cutout.XLine - traced.Ink.Top) * Zoom), width, new SKColor(42, 90, 255));
        }

        using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return png.ToArray();
    }

    private static void DrawLine(SKCanvas canvas, int? row, int width, SKColor color)
    {
        if (row is { } y)
        {
            using var paint = new SKPaint { Color = color, StrokeWidth = 2 };
            canvas.DrawLine(0, y, width, y, paint);
        }
    }
}
