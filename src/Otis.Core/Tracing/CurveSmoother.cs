// SPDX-License-Identifier: GPL-3.0-or-later
// Ported from Potrace 1.16 (trace.c, stage 4). Copyright (C) 2001-2019 Peter Selinger.
// Potrace is free software covered by the GNU General Public License, version 2 or later.
using static Otis.Core.Tracing.PotraceMath;

namespace Otis.Core.Tracing;

internal static class CurveSmoother
{
    public static void Reverse(CurveElement[] curve)
    {
        for (int i = 0, j = curve.Length - 1; i < j; i++, j--)
        {
            (curve[i].Vertex, curve[j].Vertex) = (curve[j].Vertex, curve[i].Vertex);
        }
    }

    public static void Smooth(CurveElement[] curve, double alphaMax)
    {
        var m = curve.Length;
        for (var i = 0; i < m; i++)
        {
            var j = Mod(i + 1, m);
            var k = Mod(i + 2, m);
            var p4 = Interval(0.5, curve[k].Vertex, curve[j].Vertex);

            double alpha;
            var denom = Ddenom(curve[i].Vertex, curve[k].Vertex);
            if (denom != 0.0)
            {
                var dd = Math.Abs(Dpara(curve[i].Vertex, curve[j].Vertex, curve[k].Vertex) / denom);
                alpha = (dd > 1 ? 1 - (1.0 / dd) : 0) / 0.75;
            }
            else
            {
                alpha = 4 / 3.0;
            }

            curve[j].Alpha0 = alpha;

            if (alpha >= alphaMax)
            {
                curve[j].IsCorner = true;
                curve[j].C1 = curve[j].Vertex;
                curve[j].C2 = p4;
            }
            else
            {
                alpha = Math.Clamp(alpha, 0.55, 1);
                curve[j].IsCorner = false;
                curve[j].C0 = Interval(0.5 + (0.5 * alpha), curve[i].Vertex, curve[j].Vertex);
                curve[j].C1 = Interval(0.5 + (0.5 * alpha), curve[k].Vertex, curve[j].Vertex);
                curve[j].C2 = p4;
            }

            curve[j].Alpha = alpha;
            curve[j].Beta = 0.5;
        }
    }
}
