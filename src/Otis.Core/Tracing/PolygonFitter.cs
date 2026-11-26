// SPDX-License-Identifier: GPL-3.0-or-later
// Ported from Potrace 1.16 (trace.c, stages 1-3). Copyright (C) 2001-2019 Peter Selinger.
// Potrace is free software covered by the GNU General Public License, version 2 or later.
using static Otis.Core.Tracing.PotraceMath;

namespace Otis.Core.Tracing;

internal static class PolygonFitter
{
    public static void CalcSums(PotracePath pp)
    {
        var n = pp.Length;
        pp.X0 = pp.Xs[0];
        pp.Y0 = pp.Ys[0];
        pp.SumX = new double[n + 1];
        pp.SumY = new double[n + 1];
        pp.SumX2 = new double[n + 1];
        pp.SumXY = new double[n + 1];
        pp.SumY2 = new double[n + 1];
        for (var i = 0; i < n; i++)
        {
            double x = pp.Xs[i] - pp.X0, y = pp.Ys[i] - pp.Y0;
            pp.SumX[i + 1] = pp.SumX[i] + x;
            pp.SumY[i + 1] = pp.SumY[i] + y;
            pp.SumX2[i + 1] = pp.SumX2[i] + (x * x);
            pp.SumXY[i + 1] = pp.SumXY[i] + (x * y);
            pp.SumY2[i + 1] = pp.SumY2[i] + (y * y);
        }
    }

    // Stage 1: for each i, lon[i] is the furthest index such that a straight line can be drawn from i to lon[i].
    public static void CalcLon(PotracePath pp)
    {
        var xs = pp.Xs;
        var ys = pp.Ys;
        var n = pp.Length;
        var ct = new int[4];
        var pivk = new int[n];
        var nc = new int[n];

        var k = 0;
        for (var i = n - 1; i >= 0; i--)
        {
            if (xs[i] != xs[k] && ys[i] != ys[k])
            {
                k = i + 1;
            }

            nc[i] = k;
        }

        pp.Lon = new int[n];

        for (var i = n - 1; i >= 0; i--)
        {
            Array.Clear(ct);
            var dir = (3 + (3 * (xs[Mod(i + 1, n)] - xs[i])) + (ys[Mod(i + 1, n)] - ys[i])) / 2;
            ct[dir]++;

            long c0x = 0, c0y = 0, c1x = 0, c1y = 0;
            k = nc[i];
            var k1 = i;
            var foundk = false;
            while (true)
            {
                dir = (3 + (3 * Sign(xs[k] - xs[k1])) + Sign(ys[k] - ys[k1])) / 2;
                ct[dir]++;

                if (ct[0] != 0 && ct[1] != 0 && ct[2] != 0 && ct[3] != 0)
                {
                    pivk[i] = k1;
                    foundk = true;
                    break;
                }

                long curx = xs[k] - xs[i], cury = ys[k] - ys[i];
                if (Xprod(c0x, c0y, curx, cury) < 0 || Xprod(c1x, c1y, curx, cury) > 0)
                {
                    break;
                }

                if (Math.Abs(curx) > 1 || Math.Abs(cury) > 1)
                {
                    var offx = curx + (cury >= 0 && (cury > 0 || curx < 0) ? 1 : -1);
                    var offy = cury + (curx <= 0 && (curx < 0 || cury < 0) ? 1 : -1);
                    if (Xprod(c0x, c0y, offx, offy) >= 0)
                    {
                        (c0x, c0y) = (offx, offy);
                    }

                    offx = curx + (cury <= 0 && (cury < 0 || curx < 0) ? 1 : -1);
                    offy = cury + (curx >= 0 && (curx > 0 || cury < 0) ? 1 : -1);
                    if (Xprod(c1x, c1y, offx, offy) <= 0)
                    {
                        (c1x, c1y) = (offx, offy);
                    }
                }

                k1 = k;
                k = nc[k1];
                if (!Cyclic(k, i, k1))
                {
                    break;
                }
            }

            if (foundk)
            {
                continue;
            }

            // k1 was the last corner satisfying the constraint and k the first violating it; find the last
            // point along k1..k that still satisfies it, with integer arithmetic.
            long dkx = Sign(xs[k] - xs[k1]), dky = Sign(ys[k] - ys[k1]);
            long cx = xs[k1] - xs[i], cy = ys[k1] - ys[i];
            var a = Xprod(c0x, c0y, cx, cy);
            var b = Xprod(c0x, c0y, dkx, dky);
            var c = Xprod(c1x, c1y, cx, cy);
            var d = Xprod(c1x, c1y, dkx, dky);
            var j = Infinity;
            if (b < 0)
            {
                j = FloorDiv((int)a, (int)-b);
            }

            if (d > 0)
            {
                j = Math.Min(j, FloorDiv((int)-c, (int)d));
            }

            pivk[i] = Mod(k1 + j, n);
        }

        var jj = pivk[n - 1];
        pp.Lon[n - 1] = jj;
        for (var i = n - 2; i >= 0; i--)
        {
            if (Cyclic(i + 1, pivk[i], jj))
            {
                jj = pivk[i];
            }

            pp.Lon[i] = jj;
        }

        for (var i = n - 1; Cyclic(Mod(i + 1, n), jj, pp.Lon[i]); i--)
        {
            pp.Lon[i] = jj;
        }
    }

    // Stage 2: the optimal polygon. Non-cyclic: assumes point 0 is on the polygon, as Potrace does.
    public static void BestPolygon(PotracePath pp)
    {
        var n = pp.Length;
        var pen = new double[n + 1];
        var prev = new int[n + 1];
        var clip0 = new int[n];
        var clip1 = new int[n + 1];
        var seg0 = new int[n + 1];
        var seg1 = new int[n + 1];

        for (var i = 0; i < n; i++)
        {
            var c = Mod(pp.Lon[Mod(i - 1, n)] - 1, n);
            if (c == i)
            {
                c = Mod(i + 1, n);
            }

            clip0[i] = c < i ? n : c;
        }

        var j = 1;
        for (var i = 0; i < n; i++)
        {
            while (j <= clip0[i])
            {
                clip1[j] = i;
                j++;
            }
        }

        var ii = 0;
        j = 0;
        while (ii < n)
        {
            seg0[j] = ii;
            ii = clip0[ii];
            j++;
        }

        seg0[j] = n;
        var m = j;

        ii = n;
        for (j = m; j > 0; j--)
        {
            seg1[j] = ii;
            ii = clip1[ii];
        }

        seg1[0] = 0;

        pen[0] = 0;
        for (j = 1; j <= m; j++)
        {
            for (var i = seg1[j]; i <= seg0[j]; i++)
            {
                var best = -1.0;
                for (var k = seg0[j - 1]; k >= clip1[i]; k--)
                {
                    var thispen = Penalty3(pp, k, i) + pen[k];
                    if (best < 0 || thispen < best)
                    {
                        prev[i] = k;
                        best = thispen;
                    }
                }

                pen[i] = best;
            }
        }

        pp.Polygon = new int[m];
        ii = n;
        for (j = m - 1; ii > 0; j--)
        {
            ii = prev[ii];
            pp.Polygon[j] = ii;
        }
    }

    // Stage 3: move each vertex to the point in its unit square closest to both adjacent optimal lines.
    public static void AdjustVertices(PotracePath pp)
    {
        var m = pp.Polygon.Length;
        var po = pp.Polygon;
        var n = pp.Length;
        var ctr = new TracePoint[m];
        var dir = new TracePoint[m];
        var q = new double[m][,];
        var v = new double[3];
        pp.Curve = [.. Enumerable.Range(0, m).Select(_ => new CurveElement())];

        for (var i = 0; i < m; i++)
        {
            var j = po[Mod(i + 1, m)];
            j = Mod(j - po[i], n) + po[i];
            (ctr[i], dir[i]) = PointSlope(pp, po[i], j);
        }

        for (var i = 0; i < m; i++)
        {
            q[i] = new double[3, 3];
            var d = Sq(dir[i].X) + Sq(dir[i].Y);
            if (d != 0.0)
            {
                v[0] = dir[i].Y;
                v[1] = -dir[i].X;
                v[2] = (-v[1] * ctr[i].Y) - (v[0] * ctr[i].X);
                for (var l = 0; l < 3; l++)
                {
                    for (var k = 0; k < 3; k++)
                    {
                        q[i][l, k] = v[l] * v[k] / d;
                    }
                }
            }
        }

        var qq = new double[3, 3];
        for (var i = 0; i < m; i++)
        {
            var s = new TracePoint(pp.Xs[po[i]] - pp.X0, pp.Ys[po[i]] - pp.Y0);
            var j = Mod(i - 1, m);
            for (var l = 0; l < 3; l++)
            {
                for (var k = 0; k < 3; k++)
                {
                    qq[l, k] = q[j][l, k] + q[i][l, k];
                }
            }

            var w = Minimum(qq, s, v);
            if (Math.Abs(w.X - s.X) <= 0.5 && Math.Abs(w.Y - s.Y) <= 0.5)
            {
                pp.Curve[i].Vertex = new TracePoint(w.X + pp.X0, w.Y + pp.Y0);
                continue;
            }

            var best = MinimumOnSquare(qq, s);
            pp.Curve[i].Vertex = new TracePoint(best.X + pp.X0, best.Y + pp.Y0);
        }
    }

    // The penalty of an edge from i to j: the deviation of the path from the straight line. Needs lon and sums.
    private static double Penalty3(PotracePath pp, int i, int j)
    {
        var n = pp.Length;
        var r = 0;
        if (j >= n)
        {
            j -= n;
            r = 1;
        }

        double x, y, x2, xy, y2, k;
        if (r == 0)
        {
            x = pp.SumX[j + 1] - pp.SumX[i];
            y = pp.SumY[j + 1] - pp.SumY[i];
            x2 = pp.SumX2[j + 1] - pp.SumX2[i];
            xy = pp.SumXY[j + 1] - pp.SumXY[i];
            y2 = pp.SumY2[j + 1] - pp.SumY2[i];
            k = j + 1 - i;
        }
        else
        {
            x = pp.SumX[j + 1] - pp.SumX[i] + pp.SumX[n];
            y = pp.SumY[j + 1] - pp.SumY[i] + pp.SumY[n];
            x2 = pp.SumX2[j + 1] - pp.SumX2[i] + pp.SumX2[n];
            xy = pp.SumXY[j + 1] - pp.SumXY[i] + pp.SumXY[n];
            y2 = pp.SumY2[j + 1] - pp.SumY2[i] + pp.SumY2[n];
            k = j + 1 - i + n;
        }

        var px = ((pp.Xs[i] + pp.Xs[j]) / 2.0) - pp.Xs[0];
        var py = ((pp.Ys[i] + pp.Ys[j]) / 2.0) - pp.Ys[0];
        double ey = pp.Xs[j] - pp.Xs[i];
        double ex = -(pp.Ys[j] - pp.Ys[i]);

        var a = ((x2 - (2 * x * px)) / k) + (px * px);
        var b = ((xy - (x * py) - (y * px)) / k) + (px * py);
        var c = ((y2 - (2 * y * py)) / k) + (py * py);

        return Math.Sqrt((ex * ex * a) + (2 * ex * ey * b) + (ey * ey * c));
    }

    // The centre and direction of the best-fitting line through points i..j (i < j, possibly wrapping).
    private static (TracePoint Centre, TracePoint Direction) PointSlope(PotracePath pp, int i, int j)
    {
        var n = pp.Length;
        var r = 0;
        while (j >= n)
        {
            j -= n;
            r++;
        }

        while (i >= n)
        {
            i -= n;
            r--;
        }

        while (j < 0)
        {
            j += n;
            r--;
        }

        while (i < 0)
        {
            i += n;
            r++;
        }

        var x = pp.SumX[j + 1] - pp.SumX[i] + (r * pp.SumX[n]);
        var y = pp.SumY[j + 1] - pp.SumY[i] + (r * pp.SumY[n]);
        var x2 = pp.SumX2[j + 1] - pp.SumX2[i] + (r * pp.SumX2[n]);
        var xy = pp.SumXY[j + 1] - pp.SumXY[i] + (r * pp.SumXY[n]);
        var y2 = pp.SumY2[j + 1] - pp.SumY2[i] + (r * pp.SumY2[n]);
        double k = j + 1 - i + (r * n);

        var centre = new TracePoint(x / k, y / k);

        var a = (x2 - (x * x / k)) / k;
        var b = (xy - (x * y / k)) / k;
        var c = (y2 - (y * y / k)) / k;

        var lambda2 = (a + c + Math.Sqrt(((a - c) * (a - c)) + (4 * b * b))) / 2;
        a -= lambda2;
        c -= lambda2;

        double l;
        var direction = default(TracePoint);
        if (Math.Abs(a) >= Math.Abs(c))
        {
            l = Math.Sqrt((a * a) + (b * b));
            if (l != 0)
            {
                direction = new TracePoint(-b / l, a / l);
            }
        }
        else
        {
            l = Math.Sqrt((c * c) + (b * b));
            if (l != 0)
            {
                direction = new TracePoint(-c / l, b / l);
            }
        }

        // Both eigenvalues coincide when k is 4; the direction is then undefined.
        return (centre, l == 0 ? default : direction);
    }

    private static TracePoint Minimum(double[,] q, TracePoint s, double[] v)
    {
        while (true)
        {
            var det = (q[0, 0] * q[1, 1]) - (q[0, 1] * q[1, 0]);
            if (det != 0.0)
            {
                return new TracePoint(
                    ((-q[0, 2] * q[1, 1]) + (q[1, 2] * q[0, 1])) / det,
                    ((q[0, 2] * q[1, 0]) - (q[1, 2] * q[0, 0])) / det);
            }

            // Parallel lines: add an orthogonal axis through the centre of the unit square.
            if (q[0, 0] > q[1, 1])
            {
                v[0] = -q[0, 1];
                v[1] = q[0, 0];
            }
            else if (q[1, 1] != 0)
            {
                v[0] = -q[1, 1];
                v[1] = q[1, 0];
            }
            else
            {
                v[0] = 1;
                v[1] = 0;
            }

            var d = Sq(v[0]) + Sq(v[1]);
            v[2] = (-v[1] * s.Y) - (v[0] * s.X);
            for (var l = 0; l < 3; l++)
            {
                for (var k = 0; k < 3; k++)
                {
                    q[l, k] += v[l] * v[k] / d;
                }
            }
        }
    }

    private static TracePoint MinimumOnSquare(double[,] q, TracePoint s)
    {
        var min = Quadform(q, s);
        var best = s;

        if (q[0, 0] != 0.0)
        {
            for (var z = 0; z < 2; z++)
            {
                var wy = s.Y - 0.5 + z;
                var w = new TracePoint(-((q[0, 1] * wy) + q[0, 2]) / q[0, 0], wy);
                var cand = Quadform(q, w);
                if (Math.Abs(w.X - s.X) <= 0.5 && cand < min)
                {
                    min = cand;
                    best = w;
                }
            }
        }

        if (q[1, 1] != 0.0)
        {
            for (var z = 0; z < 2; z++)
            {
                var wx = s.X - 0.5 + z;
                var w = new TracePoint(wx, -((q[1, 0] * wx) + q[1, 2]) / q[1, 1]);
                var cand = Quadform(q, w);
                if (Math.Abs(w.Y - s.Y) <= 0.5 && cand < min)
                {
                    min = cand;
                    best = w;
                }
            }
        }

        for (var l = 0; l < 2; l++)
        {
            for (var k = 0; k < 2; k++)
            {
                var w = new TracePoint(s.X - 0.5 + l, s.Y - 0.5 + k);
                var cand = Quadform(q, w);
                if (cand < min)
                {
                    min = cand;
                    best = w;
                }
            }
        }

        return best;
    }
}
