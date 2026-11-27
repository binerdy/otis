// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Imaging;
using Otis.Core.Ink;
using Otis.Core.Projects;
using Otis.Core.Tracing;

namespace Otis.Core.Building;

// The one path from a cutout to curves, shared by the build and the editor preview so both always agree.
public static class CutoutTracer
{
    private const int SpeckPixels = 40;

    public static TracedCutout Trace(RgbImage page, Cutout cutout)
    {
        var ink = CutoutExtractor.Extract(page, cutout);
        var adjustments = cutout.Adjustments;
        var smoothing = SmoothingParameters.For(adjustments.Smoothing);
        var bitmap = adjustments.GreyTracing
            ? GreyTracing.Bitmap(page, ink, smoothing.BlurSigma)
            : TracePreparation.Smooth(ink.Mask, smoothing.BlurSigma);
        var options = new TraceOptions
        {
            TurdSize = SpeckPixels * TracePreparation.Upsampling * TracePreparation.Upsampling,
            AlphaMax = smoothing.AlphaMax,
            OptimizeTolerance = smoothing.OptimizeTolerance,
        };
        return new TracedCutout(ink, PotraceTracer.Trace(bitmap, options));
    }
}
