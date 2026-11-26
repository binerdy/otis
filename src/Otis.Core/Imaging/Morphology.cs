// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.Core.Imaging;

public static class Morphology
{
    public static BitGrid Dilate(BitGrid grid, int iterations = 1)
    {
        for (var i = 0; i < iterations; i++)
        {
            grid = Step(grid, dilate: true);
        }

        return grid;
    }

    public static BitGrid Erode(BitGrid grid, int iterations = 1)
    {
        for (var i = 0; i < iterations; i++)
        {
            grid = Step(grid, dilate: false);
        }

        return grid;
    }

    public static BitGrid Open(BitGrid grid, int iterations = 1) => Dilate(Erode(grid, iterations), iterations);

    public static BitGrid Close(BitGrid grid, int iterations = 1) => Erode(Dilate(grid, iterations), iterations);

    // Outside pixels count as background for both operations, as in scipy.ndimage, so closing can eat into the border.
    private static BitGrid Step(BitGrid grid, bool dilate)
    {
        var result = new BitGrid(grid.Width, grid.Height);
        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                result[x, y] = dilate ? AnyAround(grid, x, y) : AllAround(grid, x, y);
            }
        }

        return result;
    }

    private static bool AnyAround(BitGrid grid, int x, int y)
    {
        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                if (grid[x + dx, y + dy])
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool AllAround(BitGrid grid, int x, int y)
    {
        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                if (!grid[x + dx, y + dy])
                {
                    return false;
                }
            }
        }

        return true;
    }
}
