// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Outlines;

namespace Otis.Core.TrueType;

public static class QuadraticConverter
{
    private const int MaxPieces = 32;
    private const int Samples = 9;

    // Splits the cubic into the fewest equal pieces whose single-quadratic approximations stay within maxError.
    public static IReadOnlyList<(FontPoint Control, FontPoint End)> Convert(FontPoint p0, FontPoint c1, FontPoint c2, FontPoint p3, double maxError)
    {
        for (var pieces = 1; pieces <= MaxPieces; pieces++)
        {
            var result = TryPieces(p0, c1, c2, p3, pieces, maxError);
            if (result is not null)
            {
                return result;
            }
        }

        return TryPieces(p0, c1, c2, p3, MaxPieces, double.MaxValue)!;
    }

    private static List<(FontPoint Control, FontPoint End)>? TryPieces(FontPoint p0, FontPoint c1, FontPoint c2, FontPoint p3, int pieces, double maxError)
    {
        var result = new List<(FontPoint Control, FontPoint End)>(pieces);
        for (var i = 0; i < pieces; i++)
        {
            var (q0, q1, q2, q3) = Segment(p0, c1, c2, p3, (double)i / pieces, (double)(i + 1) / pieces);
            var control = (((q1 + q2) * 3) - q0 - q3) * 0.25;
            for (var s = 1; s <= Samples; s++)
            {
                var t = (double)s / (Samples + 1);
                if (Cubic(q0, q1, q2, q3, t).DistanceTo(Quadratic(q0, control, q3, t)) > maxError)
                {
                    return null;
                }
            }

            result.Add((control, q3));
        }

        return result;
    }

    private static (FontPoint P0, FontPoint P1, FontPoint P2, FontPoint P3) Segment(FontPoint p0, FontPoint p1, FontPoint p2, FontPoint p3, double from, double to)
    {
        var (_, right) = Split(p0, p1, p2, p3, from);
        var local = from >= 1 ? 1 : (to - from) / (1 - from);
        var (left, _) = Split(right.P0, right.P1, right.P2, right.P3, local);
        return left;
    }

    private static ((FontPoint P0, FontPoint P1, FontPoint P2, FontPoint P3) Left, (FontPoint P0, FontPoint P1, FontPoint P2, FontPoint P3) Right) Split(
        FontPoint p0, FontPoint p1, FontPoint p2, FontPoint p3, double t)
    {
        var a = Lerp(p0, p1, t);
        var b = Lerp(p1, p2, t);
        var c = Lerp(p2, p3, t);
        var d = Lerp(a, b, t);
        var e = Lerp(b, c, t);
        var f = Lerp(d, e, t);
        return ((p0, a, d, f), (f, e, c, p3));
    }

    private static FontPoint Lerp(FontPoint a, FontPoint b, double t) => a + ((b - a) * t);

    private static FontPoint Cubic(FontPoint p0, FontPoint p1, FontPoint p2, FontPoint p3, double t)
    {
        var s = 1 - t;
        return (p0 * (s * s * s)) + (p1 * (3 * s * s * t)) + (p2 * (3 * s * t * t)) + (p3 * (t * t * t));
    }

    private static FontPoint Quadratic(FontPoint p0, FontPoint p1, FontPoint p2, double t)
    {
        var s = 1 - t;
        return (p0 * (s * s)) + (p1 * (2 * s * t)) + (p2 * (t * t));
    }
}
