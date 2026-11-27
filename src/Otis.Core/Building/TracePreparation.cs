// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Imaging;

namespace Otis.Core.Building;

public static class TracePreparation
{
    public const int Upsampling = 3;

    // Blurring and upsampling first keeps scan stair-steps from turning into jagged outlines.
    public static BitGrid Smooth(BitGrid ink, double blurSigma)
    {
        var plane = new float[ink.Width * ink.Height];
        for (var y = 0; y < ink.Height; y++)
        {
            for (var x = 0; x < ink.Width; x++)
            {
                plane[(y * ink.Width) + x] = ink[x, y] ? 1f : 0f;
            }
        }

        var big = Upsample(GaussianBlur.Apply(plane, ink.Width, ink.Height, blurSigma), ink.Width, ink.Height);

        var result = new BitGrid(ink.Width * Upsampling, ink.Height * Upsampling);
        for (var y = 0; y < result.Height; y++)
        {
            for (var x = 0; x < result.Width; x++)
            {
                result[x, y] = big[(y * result.Width) + x] > 0.5f;
            }
        }

        return result;
    }

    public static float[] Upsample(float[] plane, int width, int height) =>
        ResizeColumns(ResizeRows(plane, width, height, width * Upsampling), width * Upsampling, height, height * Upsampling);

    private static float[] ResizeRows(float[] source, int width, int height, int newWidth)
    {
        var result = new float[newWidth * height];
        for (var x = 0; x < newWidth; x++)
        {
            var (first, weights) = Taps(x, width, newWidth);
            for (var y = 0; y < height; y++)
            {
                var sum = 0f;
                for (var k = 0; k < 4; k++)
                {
                    sum += weights[k] * source[(y * width) + Math.Clamp(first + k, 0, width - 1)];
                }

                result[(y * newWidth) + x] = sum;
            }
        }

        return result;
    }

    private static float[] ResizeColumns(float[] source, int width, int height, int newHeight)
    {
        var result = new float[width * newHeight];
        for (var y = 0; y < newHeight; y++)
        {
            var (first, weights) = Taps(y, height, newHeight);
            for (var x = 0; x < width; x++)
            {
                var sum = 0f;
                for (var k = 0; k < 4; k++)
                {
                    sum += weights[k] * source[(Math.Clamp(first + k, 0, height - 1) * width) + x];
                }

                result[(y * width) + x] = sum;
            }
        }

        return result;
    }

    // Keys cubic convolution with a = -0.5, sampling at pixel centres.
    private static (int First, float[] Weights) Taps(int target, int size, int newSize)
    {
        var centre = ((target + 0.5) * size / newSize) - 0.5;
        var first = (int)Math.Floor(centre) - 1;
        var weights = new float[4];
        for (var k = 0; k < 4; k++)
        {
            weights[k] = (float)Keys(centre - (first + k));
        }

        return (first, weights);
    }

    private static double Keys(double t)
    {
        const double A = -0.5;
        t = Math.Abs(t);
        if (t <= 1)
        {
            return ((((A + 2) * t) - (A + 3)) * t * t) + 1;
        }

        return t < 2 ? (((((A * t) - (5 * A)) * t) + (8 * A)) * t) - (4 * A) : 0;
    }
}
