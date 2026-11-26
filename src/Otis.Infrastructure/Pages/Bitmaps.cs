// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Imaging;
using SkiaSharp;

namespace Otis.Infrastructure.Pages;

public static class Bitmaps
{
    public static RgbImage ToRgb(SKBitmap bitmap)
    {
        using var rgba = bitmap.ColorType == SKColorType.Rgba8888 ? null : bitmap.Copy(SKColorType.Rgba8888);
        var source = rgba ?? bitmap;
        var pixels = source.GetPixelSpan();
        var rgb = new byte[source.Width * source.Height * 3];
        for (int i = 0, j = 0; i < rgb.Length; i += 3, j += 4)
        {
            // Transparent areas count as white paper.
            var alpha = pixels[j + 3];
            rgb[i] = Over(pixels[j], alpha);
            rgb[i + 1] = Over(pixels[j + 1], alpha);
            rgb[i + 2] = Over(pixels[j + 2], alpha);
        }

        return new RgbImage(source.Width, source.Height, rgb);
    }

    public static SKBitmap ToBitmap(RgbImage image)
    {
        var bitmap = new SKBitmap(image.Width, image.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        var pixels = bitmap.GetPixelSpan();
        var rgb = image.Pixels;
        for (int i = 0, j = 0; i < rgb.Length; i += 3, j += 4)
        {
            pixels[j] = rgb[i];
            pixels[j + 1] = rgb[i + 1];
            pixels[j + 2] = rgb[i + 2];
            pixels[j + 3] = 255;
        }

        return bitmap;
    }

    public static void SavePng(SKBitmap bitmap, string path)
    {
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        var temporary = path + ".tmp";
        using (var stream = File.Create(temporary))
        {
            data.SaveTo(stream);
        }

        File.Move(temporary, path, overwrite: true);
    }

    private static byte Over(byte value, byte alpha) => (byte)(((value * alpha) + (255 * (255 - alpha)) + 127) / 255);
}
