// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.Core.Outlines;

public static class OutlineOrientation
{
    // A contour inside an odd number of others is a hole. TrueType fills clockwise outer contours.
    public static GlyphOutline Normalize(GlyphOutline outline)
    {
        var polygons = outline.Contours.Select(Polygon).ToList();
        var result = new List<GlyphContour>(outline.Contours.Count);
        for (var i = 0; i < outline.Contours.Count; i++)
        {
            var probe = polygons[i][0];
            var depth = polygons.Where((_, j) => j != i).Count(other => Contains(other, probe));
            var isHole = depth % 2 == 1;
            var contour = outline.Contours[i];
            var isClockwise = contour.SignedArea < 0;
            result.Add(isClockwise == !isHole ? contour : contour.Reversed());
        }

        return new GlyphOutline(result);
    }

    private static List<FontPoint> Polygon(GlyphContour contour)
    {
        var points = new List<FontPoint> { contour.Start };
        var previous = contour.Start;
        foreach (var s in contour.Segments)
        {
            if (!s.IsLine)
            {
                for (var k = 1; k < 8; k++)
                {
                    var t = k / 8.0;
                    var u = 1 - t;
                    points.Add((previous * (u * u * u)) + (s.C1 * (3 * u * u * t)) + (s.C2 * (3 * u * t * t)) + (s.End * (t * t * t)));
                }
            }

            points.Add(s.End);
            previous = s.End;
        }

        return points;
    }

    private static bool Contains(List<FontPoint> polygon, FontPoint p)
    {
        var inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var (a, b) = (polygon[i], polygon[j]);
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < ((b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y)) + a.X)
            {
                inside = !inside;
            }
        }

        return inside;
    }
}
