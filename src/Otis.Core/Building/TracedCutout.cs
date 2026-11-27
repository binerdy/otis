// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Ink;
using Otis.Core.Tracing;

namespace Otis.Core.Building;

// Contours are in the upsampled grid of the ink; PagePoint maps them back to page pixels.
public sealed record TracedCutout(CutoutInk Ink, IReadOnlyList<TracedContour> Contours)
{
    public TracePoint PagePoint(TracePoint p) =>
        new(this.Ink.Left + (p.X / TracePreparation.Upsampling), this.Ink.Top + (p.Y / TracePreparation.Upsampling));
}
