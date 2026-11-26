// SPDX-License-Identifier: GPL-3.0-or-later
// Ported from Potrace 1.16 (trace.c, stage 5). Copyright (C) 2001-2019 Peter Selinger.
// Potrace is free software covered by the GNU General Public License, version 2 or later.
using static Otis.Core.Tracing.PotraceMath;

namespace Otis.Core.Tracing;

internal static class CurveOptimizer
{
    // Replaces runs of Bezier segments by a single segment where that stays within the tolerance.
    public static CurveElement[] Optimize(CurveElement[] curve, double tolerance)
    {
        var m = curve.Length;
        var pt = new int[m + 1];
        var pen = new double[m + 1];
        var len = new int[m + 1];
        var opt = new Fit?[m + 1];
        var convc = new int[m];
        var areac = new double[m + 1];

        for (var i = 0; i < m; i++)
        {
            convc[i] = curve[i].IsCorner ? 0 : Sign(Dpara(curve[Mod(i - 1, m)].Vertex, curve[i].Vertex, curve[Mod(i + 1, m)].Vertex));
        }

        var area = 0.0;
        var p0 = curve[0].Vertex;
        for (var i = 0; i < m; i++)
        {
            var i1 = Mod(i + 1, m);
            if (!curve[i1].IsCorner)
            {
                var alpha = curve[i1].Alpha;
                area += 0.3 * alpha * (4 - alpha) * Dpara(curve[i].C2, curve[i1].Vertex, curve[i1].C2) / 2;
                area += Dpara(p0, curve[i].C2, curve[i1].C2) / 2;
            }

            areac[i + 1] = area;
        }

        pt[0] = -1;
        for (var j = 1; j <= m; j++)
        {
            pt[j] = j - 1;
            pen[j] = pen[j - 1];
            len[j] = len[j - 1] + 1;
            for (var i = j - 2; i >= 0; i--)
            {
                var o = Penalty(curve, i, Mod(j, m), tolerance, convc, areac);
                if (o is null)
                {
                    break;
                }

                if (len[j] > len[i] + 1 || (len[j] == len[i] + 1 && pen[j] > pen[i] + o.Pen))
                {
                    opt[j] = o;
                    pt[j] = i;
                    pen[j] = pen[i] + o.Pen;
                    len[j] = len[i] + 1;
                }
            }
        }

        var om = len[m];
        var result = new CurveElement[om];
        var s = new double[om];
        var t = new double[om];
        var jj = m;
        for (var i = om - 1; i >= 0; i--)
        {
            var source = curve[Mod(jj, m)];
            if (pt[jj] == jj - 1)
            {
                result[i] = source.Copy();
                s[i] = t[i] = 1.0;
            }
            else
            {
                var fit = opt[jj]!;
                result[i] = new CurveElement
                {
                    IsCorner = false,
                    C0 = fit.C0,
                    C1 = fit.C1,
                    C2 = source.C2,
                    Vertex = Interval(fit.S, source.C2, source.Vertex),
                    Alpha = fit.Alpha,
                    Alpha0 = fit.Alpha,
                };
                s[i] = fit.S;
                t[i] = fit.T;
            }

            jj = pt[jj];
        }

        for (var i = 0; i < om; i++)
        {
            result[i].Beta = s[i] / (s[i] + t[Mod(i + 1, om)]);
        }

        return result;
    }

    // The best single curve from i+.5 to j+.5 (i < j cyclically), or null when none is acceptable.
    private static Fit? Penalty(CurveElement[] curve, int i, int j, double tolerance, int[] convc, double[] areac)
    {
        var m = curve.Length;
        if (i == j || !IsConvexRun(curve, i, j, convc))
        {
            return null;
        }

        var p0 = curve[Mod(i, m)].C2;
        var p1 = curve[Mod(i + 1, m)].Vertex;
        var p2 = curve[Mod(j, m)].Vertex;
        var p3 = curve[Mod(j, m)].C2;

        var area = areac[j] - areac[i];
        area -= Dpara(curve[0].Vertex, curve[i].C2, curve[j].C2) / 2;
        if (i >= j)
        {
            area += areac[m];
        }

        var a1 = Dpara(p0, p1, p2);
        var a2 = Dpara(p0, p1, p3);
        var a3 = Dpara(p0, p2, p3);
        var a4 = a1 + a3 - a2;

        if (a2 == a1)
        {
            return null;
        }

        var t = a3 / (a3 - a4);
        var s = a2 / (a2 - a1);
        var triangle = a2 * t / 2.0;
        if (triangle == 0.0)
        {
            return null;
        }

        var r = area / triangle;
        var alpha = 2 - Math.Sqrt(4 - (r / 0.3));
        var c0 = Interval(t * alpha, p0, p1);
        var c1 = Interval(s * alpha, p3, p2);

        var pen = EdgePenalty(curve, i, j, (p0, c0, c1, p3), tolerance);
        if (pen is null)
        {
            return null;
        }

        var cornerPen = CornerPenalty(curve, i, j, (p0, c0, c1, p3), tolerance);
        return cornerPen is null ? null : new Fit(pen.Value + cornerPen.Value, c0, c1, t, s, alpha);
    }

    // Convex, free of corners, and no bend of 179 degrees or more between i and j.
    private static bool IsConvexRun(CurveElement[] curve, int i, int j, int[] convc)
    {
        var m = curve.Length;
        var i1 = Mod(i + 1, m);
        var conv = convc[i1];
        if (conv == 0)
        {
            return false;
        }

        var d = Ddist(curve[i].Vertex, curve[i1].Vertex);
        for (var k = i1; k != j; k = Mod(k + 1, m))
        {
            var k1 = Mod(k + 1, m);
            var k2 = Mod(k + 2, m);
            if (convc[k1] != conv
                || Sign(Cprod(curve[i].Vertex, curve[i1].Vertex, curve[k1].Vertex, curve[k2].Vertex)) != conv
                || Iprod1(curve[i].Vertex, curve[i1].Vertex, curve[k1].Vertex, curve[k2].Vertex)
                    < d * Ddist(curve[k1].Vertex, curve[k2].Vertex) * Cos179)
            {
                return false;
            }
        }

        return true;
    }

    // The proposed curve must touch every polygon edge between i and j within the tolerance.
    private static double? EdgePenalty(
        CurveElement[] curve, int i, int j, (TracePoint P0, TracePoint P1, TracePoint P2, TracePoint P3) b, double tolerance)
    {
        var m = curve.Length;
        var pen = 0.0;
        for (var k = Mod(i + 1, m); k != j; k = Mod(k + 1, m))
        {
            var k1 = Mod(k + 1, m);
            var t = Tangent(b.P0, b.P1, b.P2, b.P3, curve[k].Vertex, curve[k1].Vertex);
            if (t < -0.5)
            {
                return null;
            }

            var pt = Bezier(t, b.P0, b.P1, b.P2, b.P3);
            var d = Ddist(curve[k].Vertex, curve[k1].Vertex);
            if (d == 0.0)
            {
                return null;
            }

            var d1 = Dpara(curve[k].Vertex, curve[k1].Vertex, pt) / d;
            if (Math.Abs(d1) > tolerance
                || Iprod(curve[k].Vertex, curve[k1].Vertex, pt) < 0
                || Iprod(curve[k1].Vertex, curve[k].Vertex, pt) < 0)
            {
                return null;
            }

            pen += Sq(d1);
        }

        return pen;
    }

    // The proposed curve must not cut the corners of the smoothed curve by more than the tolerance.
    private static double? CornerPenalty(
        CurveElement[] curve, int i, int j, (TracePoint P0, TracePoint P1, TracePoint P2, TracePoint P3) b, double tolerance)
    {
        var m = curve.Length;
        var pen = 0.0;
        for (var k = i; k != j; k = Mod(k + 1, m))
        {
            var k1 = Mod(k + 1, m);
            var t = Tangent(b.P0, b.P1, b.P2, b.P3, curve[k].C2, curve[k1].C2);
            if (t < -0.5)
            {
                return null;
            }

            var pt = Bezier(t, b.P0, b.P1, b.P2, b.P3);
            var d = Ddist(curve[k].C2, curve[k1].C2);
            if (d == 0.0)
            {
                return null;
            }

            var d1 = Dpara(curve[k].C2, curve[k1].C2, pt) / d;
            var d2 = Dpara(curve[k].C2, curve[k1].C2, curve[k1].Vertex) / d * (0.75 * curve[k1].Alpha);
            if (d2 < 0)
            {
                d1 = -d1;
                d2 = -d2;
            }

            if (d1 < d2 - tolerance)
            {
                return null;
            }

            if (d1 < d2)
            {
                pen += Sq(d1 - d2);
            }
        }

        return pen;
    }

    private sealed record Fit(double Pen, TracePoint C0, TracePoint C1, double T, double S, double Alpha);
}
