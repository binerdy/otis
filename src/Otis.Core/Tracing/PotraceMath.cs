// SPDX-License-Identifier: GPL-3.0-or-later
// Ported from Potrace 1.16. Copyright (C) 2001-2019 Peter Selinger.
// Potrace is free software covered by the GNU General Public License, version 2 or later.
namespace Otis.Core.Tracing;

internal static class PotraceMath
{
    public const int Infinity = 10000000;

    public static readonly double Cos179 = Math.Cos(179 * Math.PI / 180);

    // Potrace's sign() yields 0 for NaN, where Math.Sign would throw.
    public static int Sign(double x)
    {
        if (x > 0)
        {
            return 1;
        }

        return x < 0 ? -1 : 0;
    }

    public static int Sign(int x) => Math.Sign(x);

    // Correct for negative a, unlike C#'s % operator.
    public static int Mod(int a, int n)
    {
        if (a >= n)
        {
            return a % n;
        }

        return a >= 0 ? a : n - 1 - ((-1 - a) % n);
    }

    public static int FloorDiv(int a, int n) => a >= 0 ? a / n : -1 - ((-1 - a) / n);

    public static TracePoint Interval(double t, TracePoint a, TracePoint b) =>
        new(a.X + (t * (b.X - a.X)), a.Y + (t * (b.Y - a.Y)));

    // A direction 90 degrees counterclockwise from p2-p0, restricted to one of the eight wind directions.
    public static TracePoint DorthInfinity(TracePoint p0, TracePoint p2) => new(-Sign(p2.Y - p0.Y), Sign(p2.X - p0.X));

    // (p1-p0) x (p2-p0), the area of the parallelogram.
    public static double Dpara(TracePoint p0, TracePoint p1, TracePoint p2) =>
        ((p1.X - p0.X) * (p2.Y - p0.Y)) - ((p2.X - p0.X) * (p1.Y - p0.Y));

    // The unit square around p1 meets the line p0p2 iff |dpara(p0,p1,p2)| <= ddenom(p0,p2).
    public static double Ddenom(TracePoint p0, TracePoint p2)
    {
        var r = DorthInfinity(p0, p2);
        return (r.Y * (p2.X - p0.X)) - (r.X * (p2.Y - p0.Y));
    }

    public static bool Cyclic(int a, int b, int c) => a <= c ? a <= b && b < c : a <= b || b < c;

    public static double Quadform(double[,] q, TracePoint w)
    {
        Span<double> v = [w.X, w.Y, 1.0];
        var sum = 0.0;
        for (var i = 0; i < 3; i++)
        {
            for (var j = 0; j < 3; j++)
            {
                sum += v[i] * q[i, j] * v[j];
            }
        }

        return sum;
    }

    public static long Xprod(long ax, long ay, long bx, long by) => (ax * by) - (ay * bx);

    // (p1-p0) x (p3-p2)
    public static double Cprod(TracePoint p0, TracePoint p1, TracePoint p2, TracePoint p3) =>
        ((p1.X - p0.X) * (p3.Y - p2.Y)) - ((p3.X - p2.X) * (p1.Y - p0.Y));

    // (p1-p0) * (p2-p0)
    public static double Iprod(TracePoint p0, TracePoint p1, TracePoint p2) =>
        ((p1.X - p0.X) * (p2.X - p0.X)) + ((p1.Y - p0.Y) * (p2.Y - p0.Y));

    // (p1-p0) * (p3-p2)
    public static double Iprod1(TracePoint p0, TracePoint p1, TracePoint p2, TracePoint p3) =>
        ((p1.X - p0.X) * (p3.X - p2.X)) + ((p1.Y - p0.Y) * (p3.Y - p2.Y));

    public static double Sq(double x) => x * x;

    public static double Ddist(TracePoint p, TracePoint q) => Math.Sqrt(Sq(p.X - q.X) + Sq(p.Y - q.Y));

    public static TracePoint Bezier(double t, TracePoint p0, TracePoint p1, TracePoint p2, TracePoint p3)
    {
        var s = 1 - t;
        return new TracePoint(
            (s * s * s * p0.X) + (3 * (s * s * t) * p1.X) + (3 * (t * t * s) * p2.X) + (t * t * t * p3.X),
            (s * s * s * p0.Y) + (3 * (s * s * t) * p1.Y) + (3 * (t * t * s) * p2.Y) + (t * t * t * p3.Y));
    }

    // The t in [0..1] at which the convex curve (p0,p1,p2,p3) is tangent to q1-q0, or -1.
    public static double Tangent(TracePoint p0, TracePoint p1, TracePoint p2, TracePoint p3, TracePoint q0, TracePoint q1)
    {
        var a0 = Cprod(p0, p1, q0, q1);
        var b0 = Cprod(p1, p2, q0, q1);
        var c0 = Cprod(p2, p3, q0, q1);

        var a = a0 - (2 * b0) + c0;
        var b = (-2 * a0) + (2 * b0);
        var c = a0;
        var d = (b * b) - (4 * a * c);

        if (a == 0 || d < 0)
        {
            return -1.0;
        }

        var s = Math.Sqrt(d);
        var r1 = (-b + s) / (2 * a);
        var r2 = (-b - s) / (2 * a);

        if (r1 is >= 0 and <= 1)
        {
            return r1;
        }

        return r2 is >= 0 and <= 1 ? r2 : -1.0;
    }
}
