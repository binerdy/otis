// SPDX-License-Identifier: GPL-3.0-or-later
// Ported from Potrace 1.16 (decompose.c). Copyright (C) 2001-2019 Peter Selinger.
// Potrace is free software covered by the GNU General Public License, version 2 or later.
namespace Otis.Core.Tracing;

internal static class PathDecomposer
{
    // Constant term of the inverse in GF(8) mod x^8+x^4+x^3+x+1; a deterministic pseudo-random bit table.
    private const string DetrandTable =
        "0110101101100111000111010110100000011101100100000100110001011111"
        + "1011111110110111101000110000101100111001011111111111010000001010"
        + "1010010010111010000100000010101010011010000001000011110110011001"
        + "1011000111101000010111000101100111010011001110011100001010101010";

    public static List<PotracePath> Decompose(TraceBitmap original, TraceOptions options)
    {
        var bm = original.Copy();
        var paths = new List<PotracePath>();
        int x = 0, y = bm.Height - 1;
        while (bm.FindNext(ref x, ref y))
        {
            var sign = original.Get(x, y);
            var path = FindPath(bm, x, y + 1, sign, options.TurnPolicy);
            XorPath(bm, path);
            if (path.Area > options.TurdSize)
            {
                paths.Add(path);
            }
        }

        return paths;
    }

    // Walks the boundary between set and unset pixels, starting at an upper-left corner (x0, y0).
    private static PotracePath FindPath(TraceBitmap bm, int x0, int y0, bool sign, TurnPolicy policy)
    {
        int x = x0, y = y0, dirx = 0, diry = -1, area = 0;
        var xs = new List<int>();
        var ys = new List<int>();

        while (true)
        {
            xs.Add(x);
            ys.Add(y);
            x += dirx;
            y += diry;
            area += x * diry;

            if (x == x0 && y == y0)
            {
                break;
            }

            var c = bm.Get(x + ((dirx + diry - 1) / 2), y + ((diry - dirx - 1) / 2));
            var d = bm.Get(x + ((dirx - diry - 1) / 2), y + ((diry + dirx - 1) / 2));

            if (c && !d)
            {
                if (TurnsRight(bm, x, y, sign, policy))
                {
                    (dirx, diry) = (diry, -dirx);
                }
                else
                {
                    (dirx, diry) = (-diry, dirx);
                }
            }
            else if (c)
            {
                (dirx, diry) = (diry, -dirx);
            }
            else if (!d)
            {
                (dirx, diry) = (-diry, dirx);
            }
        }

        return new PotracePath([.. xs], [.. ys], area, sign);
    }

    private static bool TurnsRight(TraceBitmap bm, int x, int y, bool sign, TurnPolicy policy) => policy switch
    {
        TurnPolicy.Right => true,
        TurnPolicy.Black => sign,
        TurnPolicy.White => !sign,
        TurnPolicy.Random => Detrand(x, y),
        TurnPolicy.Majority => Majority(bm, x, y),
        TurnPolicy.Minority => !Majority(bm, x, y),
        _ => false,
    };

    // The majority value of the bitmap around the intersection (x, y), assuming balance at radius 1.
    private static bool Majority(TraceBitmap bm, int x, int y)
    {
        for (var i = 2; i < 5; i++)
        {
            var ct = 0;
            for (var a = -i + 1; a <= i - 1; a++)
            {
                ct += bm.Get(x + a, y + i - 1) ? 1 : -1;
                ct += bm.Get(x + i - 1, y + a - 1) ? 1 : -1;
                ct += bm.Get(x + a - 1, y - i) ? 1 : -1;
                ct += bm.Get(x - i, y + a) ? 1 : -1;
            }

            if (ct > 0)
            {
                return true;
            }

            if (ct < 0)
            {
                return false;
            }
        }

        return false;
    }

    private static bool Detrand(int x, int y)
    {
        var z = unchecked((uint)(((0x04b3e375 * x) ^ y) * 0x05a8ef93));
        var bit = Bit(z & 0xff) ^ Bit((z >> 8) & 0xff) ^ Bit((z >> 16) & 0xff) ^ Bit((z >> 24) & 0xff);
        return bit != 0;

        static int Bit(uint index) => DetrandTable[(int)index] - '0';
    }

    // Inverts the interior of the path, using the path's first x as the reference column.
    private static void XorPath(TraceBitmap bm, PotracePath path)
    {
        if (path.Length == 0)
        {
            return;
        }

        var y1 = path.Ys[^1];
        var xa = path.Xs[0];
        for (var k = 0; k < path.Length; k++)
        {
            int x = path.Xs[k], y = path.Ys[k];
            if (y != y1)
            {
                var row = Math.Min(y, y1);
                if (x < xa)
                {
                    bm.Invert(row, x, xa);
                }
                else if (x != xa)
                {
                    bm.Invert(row, xa, x);
                }

                y1 = y;
            }
        }
    }
}
