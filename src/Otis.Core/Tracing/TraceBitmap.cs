// SPDX-License-Identifier: GPL-3.0-or-later
// Ported from Potrace 1.16. Copyright (C) 2001-2019 Peter Selinger.
// Potrace is free software covered by the GNU General Public License, version 2 or later.
using Otis.Core.Imaging;

namespace Otis.Core.Tracing;

internal sealed class TraceBitmap
{
    private readonly bool[] bits;

    private TraceBitmap(int width, int height, bool[] bits)
    {
        this.Width = width;
        this.Height = height;
        this.bits = bits;
    }

    public int Width { get; }

    public int Height { get; }

    public static TraceBitmap From(BitGrid grid)
    {
        var bits = new bool[grid.Width * grid.Height];
        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                bits[(y * grid.Width) + x] = grid[x, y];
            }
        }

        return new TraceBitmap(grid.Width, grid.Height, bits);
    }

    public TraceBitmap Copy() => new(this.Width, this.Height, (bool[])this.bits.Clone());

    // Pixels outside the bitmap are background, as with Potrace's BM_GET.
    public bool Get(int x, int y) => x >= 0 && y >= 0 && x < this.Width && y < this.Height && this.bits[(y * this.Width) + x];

    public void Invert(int y, int fromX, int toX)
    {
        if (y < 0 || y >= this.Height)
        {
            return;
        }

        for (var x = Math.Max(0, fromX); x < Math.Min(this.Width, toX); x++)
        {
            this.bits[(y * this.Width) + x] = !this.bits[(y * this.Width) + x];
        }
    }

    // Searches rows from the highest y downwards and each row from the left, starting at row y.
    public bool FindNext(ref int x, ref int y)
    {
        for (; y >= 0; y--)
        {
            for (var cx = 0; cx < this.Width; cx++)
            {
                if (this.bits[(y * this.Width) + cx])
                {
                    x = cx;
                    return true;
                }
            }
        }

        return false;
    }
}
