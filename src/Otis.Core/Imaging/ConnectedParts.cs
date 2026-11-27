// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.Core.Imaging;

public static class ConnectedParts
{
    public static int[] Label(BitGrid grid, out int count)
    {
        var labels = new int[grid.Width * grid.Height];
        var stack = new Stack<(int X, int Y)>();
        count = 0;
        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                if (!grid[x, y] || labels[(y * grid.Width) + x] != 0)
                {
                    continue;
                }

                count++;
                labels[(y * grid.Width) + x] = count;
                stack.Push((x, y));
                while (stack.TryPop(out var p))
                {
                    for (var dy = -1; dy <= 1; dy++)
                    {
                        for (var dx = -1; dx <= 1; dx++)
                        {
                            int nx = p.X + dx, ny = p.Y + dy;
                            if (grid[nx, ny] && labels[(ny * grid.Width) + nx] == 0)
                            {
                                labels[(ny * grid.Width) + nx] = count;
                                stack.Push((nx, ny));
                            }
                        }
                    }
                }
            }
        }

        return labels;
    }

    public static BitGrid KeepLarge(BitGrid grid, double minimumShareOfLargest)
    {
        var labels = Label(grid, out var count);
        if (count == 0)
        {
            return grid.Clone();
        }

        var sizes = new int[count + 1];
        foreach (var label in labels)
        {
            sizes[label]++;
        }

        sizes[0] = 0;
        var threshold = minimumShareOfLargest * sizes.Max();
        var result = new BitGrid(grid.Width, grid.Height);
        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                var label = labels[(y * grid.Width) + x];
                result[x, y] = label != 0 && sizes[label] >= threshold;
            }
        }

        return result;
    }
}
