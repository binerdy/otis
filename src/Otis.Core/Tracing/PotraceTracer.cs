// SPDX-License-Identifier: GPL-3.0-or-later
// Ported from Potrace 1.16. Copyright (C) 2001-2019 Peter Selinger.
// Potrace is free software covered by the GNU General Public License, version 2 or later.
using Otis.Core.Imaging;

namespace Otis.Core.Tracing;

public static class PotraceTracer
{
    // Coordinates are pixel corners in the grid's own rows: y grows downwards, as in the page.
    public static IReadOnlyList<TracedContour> Trace(BitGrid ink, TraceOptions? options = null)
    {
        options ??= new TraceOptions();
        var paths = PathDecomposer.Decompose(TraceBitmap.From(ink), options);
        var contours = new List<TracedContour>(paths.Count);
        foreach (var path in paths)
        {
            PolygonFitter.CalcSums(path);
            PolygonFitter.CalcLon(path);
            PolygonFitter.BestPolygon(path);
            PolygonFitter.AdjustVertices(path);
            if (!path.Sign)
            {
                CurveSmoother.Reverse(path.Curve);
            }

            CurveSmoother.Smooth(path.Curve, options.AlphaMax);
            path.FinalCurve = options.OptimizeCurves ? CurveOptimizer.Optimize(path.Curve, options.OptimizeTolerance) : path.Curve;
            contours.Add(new TracedContour([.. path.FinalCurve.Select(ToSegment)], IsHole: !path.Sign));
        }

        return contours;
    }

    private static TracedSegment ToSegment(CurveElement e) =>
        e.IsCorner ? new TracedSegment(IsCorner: true, e.C1, e.C1, e.C2) : new TracedSegment(IsCorner: false, e.C0, e.C1, e.C2);
}
