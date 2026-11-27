// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.Core.Imaging;

public static class MaxFilter
{
    // Separable square maximum; edges use only the pixels that exist.
    public static float[] Apply(float[] plane, int width, int height, int radius)
    {
        var rows = new float[plane.Length];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var max = float.MinValue;
                for (var k = Math.Max(0, x - radius); k <= Math.Min(width - 1, x + radius); k++)
                {
                    max = Math.Max(max, plane[(y * width) + k]);
                }

                rows[(y * width) + x] = max;
            }
        }

        var result = new float[plane.Length];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var max = float.MinValue;
                for (var k = Math.Max(0, y - radius); k <= Math.Min(height - 1, y + radius); k++)
                {
                    max = Math.Max(max, rows[(k * width) + x]);
                }

                result[(y * width) + x] = max;
            }
        }

        return result;
    }
}
