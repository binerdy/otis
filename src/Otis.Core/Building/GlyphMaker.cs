// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Glyphs;
using Otis.Core.Imaging;
using Otis.Core.Ink;
using Otis.Core.Outlines;
using Otis.Core.Projects;
using Otis.Core.Tracing;

namespace Otis.Core.Building;

public static class GlyphMaker
{
    private const double BodyOpeningShare = 0.07;
    private const int SpeckPixels = 40;

    private static readonly TraceOptions Tracing = new()
    {
        TurdSize = SpeckPixels * TracePreparation.Upsampling * TracePreparation.Upsampling,
        AlphaMax = 1.0,
        OptimizeTolerance = 0.25,
    };

    public static PlacedGlyph Make(string name, RgbImage page, Cutout cutout, FontSettings font)
    {
        var ink = CutoutExtractor.Extract(page, cutout);
        var xHeightPixels = Math.Max(1, cutout.Baseline - cutout.XLine);
        var scale = (double)font.XHeight / xHeightPixels;
        var (bodyLeft, bodyRight) = BodyColumns(ink.Mask, xHeightPixels);

        var leftSidebearing = font.Sidebearing;
        var rightSidebearing = cutout.RightSidebearing ?? (IsCapital(name) ? font.CapitalRightSidebearing : font.Sidebearing);
        var bodyLeftOnPage = ink.Left + bodyLeft;

        FontPoint ToFont(TracePoint p)
        {
            var pageX = ink.Left + (p.X / TracePreparation.Upsampling);
            var pageY = ink.Top + (p.Y / TracePreparation.Upsampling);
            return new FontPoint(((pageX - bodyLeftOnPage) * scale) + leftSidebearing, (cutout.Baseline - pageY) * scale);
        }

        var contours = PotraceTracer.Trace(TracePreparation.Smooth(ink.Mask), Tracing)
            .Select(c => Orient(ToContour(c, ToFont), c.IsHole))
            .ToList();
        var bodyWidth = (bodyRight - bodyLeft) * scale;
        var advance = (int)Math.Round(bodyWidth + leftSidebearing + rightSidebearing);
        return new PlacedGlyph(name, new GlyphOutline(contours), Math.Max(advance, 0), leftSidebearing, leftSidebearing + bodyWidth);
    }

    // TrueType fills clockwise outer contours; holes run the other way.
    public static GlyphContour Orient(GlyphContour contour, bool isHole)
    {
        var isClockwise = contour.SignedArea < 0;
        return isClockwise == !isHole ? contour : contour.Reversed();
    }

    private static bool IsCapital(string name) => GlyphNames.CharacterOf(name) is { } c && char.IsUpper(c);

    // The body is the ink without hairlines; flourishes outside it may overhang the neighbours.
    private static (int Left, int Right) BodyColumns(BitGrid mask, int xHeightPixels)
    {
        var radius = Math.Max(1, (int)Math.Round(BodyOpeningShare * xHeightPixels));
        var body = Morphology.Open(mask, radius);
        var bounds = body.Bounds() ?? mask.Bounds();
        return bounds is { } b ? (b.Left, b.Right) : (0, 0);
    }

    private static GlyphContour ToContour(TracedContour traced, Func<TracePoint, FontPoint> map)
    {
        var segments = new List<ContourSegment>();
        foreach (var s in traced.Segments)
        {
            if (s.IsCorner)
            {
                segments.Add(ContourSegment.Line(map(s.C1)));
                segments.Add(ContourSegment.Line(map(s.End)));
            }
            else
            {
                segments.Add(ContourSegment.Cubic(map(s.C1), map(s.C2), map(s.End)));
            }
        }

        return new GlyphContour(map(traced.Start), segments);
    }
}
