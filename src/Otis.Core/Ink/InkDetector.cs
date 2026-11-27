// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Imaging;

namespace Otis.Core.Ink;

public static class InkDetector
{
    private const double BlurSigma = 1.0;
    private const float DarkThreshold = 185f;
    private const float ColouredThreshold = 225f;
    private const float ColouredRedOverBlue = 45f;
    private const float AlwaysInkThreshold = 120f;

    public static BitGrid Detect(RgbImage image)
    {
        var (r, g, b) = Planes(image);
        r = GaussianBlur.Apply(r, image.Width, image.Height, BlurSigma);
        g = GaussianBlur.Apply(g, image.Width, image.Height, BlurSigma);
        b = GaussianBlur.Apply(b, image.Width, image.Height, BlurSigma);

        var ink = new BitGrid(image.Width, image.Height);
        var veryDark = new BitGrid(image.Width, image.Height);
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                var i = (y * image.Width) + x;
                var mean = (r[i] + g[i] + b[i]) / 3f;
                var coloured = r[i] - b[i] > ColouredRedOverBlue && mean < ColouredThreshold;
                ink[x, y] = mean < DarkThreshold || coloured;
                veryDark[x, y] = ink[x, y] && mean < AlwaysInkThreshold;
            }
        }

        return Morphology.Open(ink).Or(veryDark);
    }

    private static (float[] R, float[] G, float[] B) Planes(RgbImage image)
    {
        var count = image.Width * image.Height;
        float[] r = new float[count], g = new float[count], b = new float[count];
        var pixels = image.Pixels;
        for (var i = 0; i < count; i++)
        {
            r[i] = pixels[i * 3];
            g[i] = pixels[(i * 3) + 1];
            b[i] = pixels[(i * 3) + 2];
        }

        return (r, g, b);
    }
}
