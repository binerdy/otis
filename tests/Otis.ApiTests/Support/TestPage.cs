// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Imaging;
using SkiaSharp;

namespace Otis.ApiTests.Support;

internal sealed class TestPage : IDisposable
{
    private readonly SKBitmap bitmap;

    public TestPage(int width = 600, int height = 400)
    {
        this.bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
        this.Canvas = new SKCanvas(this.bitmap);
        this.Canvas.Clear(new SKColor(250, 248, 242));
    }

    public SKCanvas Canvas { get; }

    public TestPage Rect(int left, int top, int right, int bottom, SKColor? color = null)
    {
        using var paint = new SKPaint { Color = color ?? new SKColor(20, 20, 25), IsAntialias = true };
        this.Canvas.DrawRect(SKRect.Create(left, top, right - left, bottom - top), paint);
        return this;
    }

    public TestPage Circle(int cx, int cy, int radius, SKColor? color = null)
    {
        using var paint = new SKPaint { Color = color ?? new SKColor(20, 20, 25), IsAntialias = true };
        this.Canvas.DrawCircle(cx, cy, radius, paint);
        return this;
    }

    public TestPage Text(string text, float x, float baseline, float size, string family = "Times New Roman")
    {
        using var typeface = SKTypeface.FromFamilyName(family);
        using var font = new SKFont(typeface, size);
        using var paint = new SKPaint { Color = new SKColor(20, 20, 25), IsAntialias = true };
        this.Canvas.DrawText(text, x, baseline, SKTextAlign.Left, font, paint);
        return this;
    }

    public RgbImage ToImage()
    {
        this.Canvas.Flush();
        var rgba = this.bitmap.GetPixelSpan();
        var rgb = new byte[this.bitmap.Width * this.bitmap.Height * 3];
        for (int i = 0, j = 0; i < rgb.Length; i += 3, j += 4)
        {
            rgb[i] = rgba[j];
            rgb[i + 1] = rgba[j + 1];
            rgb[i + 2] = rgba[j + 2];
        }

        return new RgbImage(this.bitmap.Width, this.bitmap.Height, rgb);
    }

    public void SavePng(string path)
    {
        this.Canvas.Flush();
        using var data = this.bitmap.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, data.ToArray());
    }

    public void Dispose()
    {
        this.Canvas.Dispose();
        this.bitmap.Dispose();
    }
}
