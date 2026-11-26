// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Projects;

namespace Otis.Core.Imaging;

public sealed class RgbImage
{
    private readonly byte[] rgb;

    public RgbImage(int width, int height, byte[] rgb)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(rgb.Length, width * height * 3);
        this.Width = width;
        this.Height = height;
        this.rgb = rgb;
    }

    public int Width { get; }

    public int Height { get; }

    public ReadOnlySpan<byte> Pixels => this.rgb;

    public (byte R, byte G, byte B) this[int x, int y]
    {
        get
        {
            var i = ((y * this.Width) + x) * 3;
            return (this.rgb[i], this.rgb[i + 1], this.rgb[i + 2]);
        }
    }

    public RgbImage Crop(PixelRect area)
    {
        var clipped = this.Clip(area);
        var bytes = new byte[clipped.Width * clipped.Height * 3];
        for (var y = 0; y < clipped.Height; y++)
        {
            Array.Copy(this.rgb, (((clipped.Top + y) * this.Width) + clipped.Left) * 3, bytes, y * clipped.Width * 3, clipped.Width * 3);
        }

        return new RgbImage(clipped.Width, clipped.Height, bytes);
    }

    // Averages blocks of factor × factor pixels.
    public RgbImage Shrink(int factor)
    {
        var width = this.Width / factor;
        var height = this.Height / factor;
        var bytes = new byte[width * height * 3];
        var area = factor * factor;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                for (var c = 0; c < 3; c++)
                {
                    var sum = 0;
                    for (var dy = 0; dy < factor; dy++)
                    {
                        for (var dx = 0; dx < factor; dx++)
                        {
                            var pixel = (((y * factor) + dy) * this.Width) + (x * factor) + dx;
                            sum += this.rgb[(pixel * 3) + c];
                        }
                    }

                    bytes[(((y * width) + x) * 3) + c] = (byte)(sum / area);
                }
            }
        }

        return new RgbImage(width, height, bytes);
    }

    public PixelRect Clip(PixelRect area) => new(
        Math.Clamp(area.Left, 0, this.Width),
        Math.Clamp(area.Top, 0, this.Height),
        Math.Clamp(area.Right, 0, this.Width),
        Math.Clamp(area.Bottom, 0, this.Height));
}
