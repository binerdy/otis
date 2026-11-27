// SPDX-License-Identifier: GPL-3.0-or-later
// Ported from Potrace 1.16. Copyright (C) 2001-2019 Peter Selinger.
// Potrace is free software covered by the GNU General Public License, version 2 or later.
namespace Otis.Core.Tracing;

internal sealed class PotracePath(int[] xs, int[] ys, int area, bool sign)
{
    public int[] Xs { get; } = xs;

    public int[] Ys { get; } = ys;

    public int Length => this.Xs.Length;

    public int Area { get; } = area;

    public bool Sign { get; } = sign;

    public int X0 { get; set; }

    public int Y0 { get; set; }

    public double[] SumX { get; set; } = [];

    public double[] SumY { get; set; } = [];

    public double[] SumX2 { get; set; } = [];

    public double[] SumXY { get; set; } = [];

    public double[] SumY2 { get; set; } = [];

    public int[] Lon { get; set; } = [];

    public int[] Polygon { get; set; } = [];

    public CurveElement[] Curve { get; set; } = [];

    public CurveElement[] FinalCurve { get; set; } = [];
}
