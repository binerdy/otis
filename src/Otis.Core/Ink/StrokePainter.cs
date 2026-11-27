// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Imaging;
using Otis.Core.Projects;

namespace Otis.Core.Ink;

public static class StrokePainter
{
    // Stroke points name pixels; a pixel becomes ink when it lies within half the width of a segment.
    public static BitGrid Paint(IReadOnlyList<InkStroke> strokes, int left, int top, int width, int height)
    {
        var grid = new BitGrid(width, height);
        foreach (var stroke in strokes)
        {
            var radius = Math.Max(0.5, stroke.Width / 2.0);
            var points = stroke.Points;
            for (var i = 0; i < points.Count; i++)
            {
                var a = points[i];
                var b = points[Math.Min(i + 1, points.Count - 1)];
                PaintSegment(grid, a.X - left, a.Y - top, b.X - left, b.Y - top, radius);
            }
        }

        return grid;
    }

    private static void PaintSegment(BitGrid grid, double ax, double ay, double bx, double by, double radius)
    {
        var x0 = Math.Max(0, (int)Math.Floor(Math.Min(ax, bx) - radius));
        var x1 = Math.Min(grid.Width - 1, (int)Math.Ceiling(Math.Max(ax, bx) + radius));
        var y0 = Math.Max(0, (int)Math.Floor(Math.Min(ay, by) - radius));
        var y1 = Math.Min(grid.Height - 1, (int)Math.Ceiling(Math.Max(ay, by) + radius));
        double dx = bx - ax, dy = by - ay;
        var length = (dx * dx) + (dy * dy);
        for (var y = y0; y <= y1; y++)
        {
            for (var x = x0; x <= x1; x++)
            {
                double px = x, py = y;
                var t = length == 0 ? 0 : Math.Clamp((((px - ax) * dx) + ((py - ay) * dy)) / length, 0, 1);
                var ex = px - (ax + (t * dx));
                var ey = py - (ay + (t * dy));
                if ((ex * ex) + (ey * ey) <= radius * radius)
                {
                    grid[x, y] = true;
                }
            }
        }
    }
}
