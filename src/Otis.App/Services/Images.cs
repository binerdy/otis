// SPDX-License-Identifier: GPL-3.0-or-later
using Microsoft.UI.Xaml.Media.Imaging;
using Otis.Core.Imaging;
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

    private static void DrawLine(SKCanvas canvas, int? row, int width, SKColor color)
    {
        if (row is { } y)
        {
            using var paint = new SKPaint { Color = color, StrokeWidth = 2 };
            canvas.DrawLine(0, y, width, y, paint);
        }
    }
}
