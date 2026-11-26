// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.Core.Imaging;

public static class GaussianBlur
{
    public static float[] Apply(float[] plane, int width, int height, double sigma)
    {
        var kernel = Kernel(sigma);
        var temp = new float[plane.Length];
        var result = new float[plane.Length];
        var radius = kernel.Length / 2;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var sum = 0f;
                for (var k = -radius; k <= radius; k++)
                {
                    sum += kernel[k + radius] * plane[(y * width) + Reflect(x + k, width)];
                }

                temp[(y * width) + x] = sum;
            }
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var sum = 0f;
                for (var k = -radius; k <= radius; k++)
                {
                    sum += kernel[k + radius] * temp[(Reflect(y + k, height) * width) + x];
                }

                result[(y * width) + x] = sum;
            }
        }

        return result;
    }

    // Truncating at four sigma and mirroring the edge pixel follows scipy.ndimage, which the prototype used.
    private static float[] Kernel(double sigma)
    {
        var radius = (int)((4 * sigma) + 0.5);
        var kernel = new float[(2 * radius) + 1];
        var sum = 0.0;
        for (var i = -radius; i <= radius; i++)
        {
            var v = Math.Exp(-(i * i) / (2 * sigma * sigma));
            kernel[i + radius] = (float)v;
            sum += v;
        }

        for (var i = 0; i < kernel.Length; i++)
        {
            kernel[i] = (float)(kernel[i] / sum);
        }

        return kernel;
    }

    private static int Reflect(int i, int length)
    {
        if (length == 1)
        {
            return 0;
        }

        while (i < 0 || i >= length)
        {
            i = i < 0 ? -i - 1 : (2 * length) - i - 1;
        }

        return i;
    }
}
