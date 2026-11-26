// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Building;
using Otis.Core.Outlines;
using SkiaSharp;

namespace Otis.Infrastructure.Outlines;

public sealed class SkiaOverlapRemover : IOverlapRemover
{
    public GlyphOutline RemoveOverlaps(GlyphOutline outline)
    {
        using var path = ToPath(outline);
        using var simplified = path.Simplify() ?? new SKPath(path);
        return OutlineOrientation.Normalize(FromPath(simplified));
    }

    private static SKPath ToPath(GlyphOutline outline)
    {
        using var builder = new SKPathBuilder { FillType = SKPathFillType.Winding };
        foreach (var contour in outline.Contours)
        {
            builder.MoveTo(Point(contour.Start));
            foreach (var s in contour.Segments)
            {
                if (s.IsLine)
                {
                    builder.LineTo(Point(s.End));
                }
                else
                {
                    builder.CubicTo(Point(s.C1), Point(s.C2), Point(s.End));
                }
            }

            builder.Close();
        }

        return builder.Detach();
    }

    private static GlyphOutline FromPath(SKPath path)
    {
        var contours = new List<GlyphContour>();
        var segments = new List<ContourSegment>();
        var start = default(FontPoint);
        var current = default(FontPoint);
        using var iterator = path.CreateRawIterator();
        var points = new SKPoint[4];
        SKPathVerb verb;
        while ((verb = iterator.Next(points)) != SKPathVerb.Done)
        {
            switch (verb)
            {
                case SKPathVerb.Move:
                    Flush();
                    start = current = Font(points[0]);
                    break;
                case SKPathVerb.Line:
                    current = Font(points[1]);
                    segments.Add(ContourSegment.Line(current));
                    break;
                case SKPathVerb.Quad:
                    segments.Add(QuadAsCubic(current, Font(points[1]), Font(points[2])));
                    current = Font(points[2]);
                    break;
                case SKPathVerb.Conic:
                    foreach (var (control, end) in Quads(points, iterator.ConicWeight()))
                    {
                        segments.Add(QuadAsCubic(current, control, end));
                        current = end;
                    }

                    break;
                case SKPathVerb.Cubic:
                    current = Font(points[3]);
                    segments.Add(ContourSegment.Cubic(Font(points[1]), Font(points[2]), current));
                    break;
                case SKPathVerb.Close:
                    Flush();
                    break;
            }
        }

        Flush();
        return new GlyphOutline(contours);

        void Flush()
        {
            if (segments.Count > 0)
            {
                contours.Add(new GlyphContour(start, [.. segments]));
                segments.Clear();
            }
        }
    }

    private static IEnumerable<(FontPoint Control, FontPoint End)> Quads(SKPoint[] conic, float weight)
    {
        var quads = SKPath.ConvertConicToQuads(conic[0], conic[1], conic[2], weight, 2);
        for (var i = 1; i + 1 < quads.Length; i += 2)
        {
            var (control, end) = (quads[i], quads[i + 1]);
            yield return (Font(control), Font(end));
        }
    }

    private static ContourSegment QuadAsCubic(FontPoint p0, FontPoint control, FontPoint end) =>
        ContourSegment.Cubic(p0 + ((control - p0) * (2.0 / 3)), end + ((control - end) * (2.0 / 3)), end);

    private static SKPoint Point(FontPoint p) => new((float)p.X, (float)p.Y);

    private static FontPoint Font(SKPoint p) => new(p.X, p.Y);
}
