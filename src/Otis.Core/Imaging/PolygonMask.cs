// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Projects;

namespace Otis.Core.Imaging;

public static class PolygonMask
{
    public static BitGrid Fill(IReadOnlyList<PixelPoint> polygon, int left, int top, int width, int height)
    {
        var mask = new BitGrid(width, height);
        var crossings = new List<double>();
        for (var row = 0; row < height; row++)
        {
            var y = top + row + 0.5;
            crossings.Clear();
            for (var i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Count];
                if ((a.Y <= y) != (b.Y <= y))
                {
                    crossings.Add(a.X + ((y - a.Y) * (b.X - a.X) / (b.Y - a.Y)));
                }
            }

            crossings.Sort();
            for (var c = 0; c + 1 < crossings.Count; c += 2)
            {
                var from = Math.Max(0, (int)Math.Ceiling(crossings[c] - 0.5) - left);
                var to = Math.Min(width - 1, (int)Math.Floor(crossings[c + 1] - 0.5) - left);
                for (var x = from; x <= to; x++)
                {
                    mask[x, row] = true;
                }
            }
        }

        return mask;
    }
}
