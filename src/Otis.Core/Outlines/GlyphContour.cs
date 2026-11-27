// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.Core.Outlines;

public sealed record GlyphContour(FontPoint Start, IReadOnlyList<ContourSegment> Segments)
{
    // Positive for counter-clockwise contours in y-up coordinates, judged by the on-curve polygon.
    public double SignedArea
    {
        get
        {
            var area = 0.0;
            var previous = this.Start;
            foreach (var segment in this.Segments)
            {
                foreach (var point in segment.IsLine ? [segment.End] : new[] { segment.C1, segment.C2, segment.End })
                {
                    area += (previous.X * point.Y) - (point.X * previous.Y);
                    previous = point;
                }
            }

            return area / 2;
        }
    }

    public GlyphContour Map(Func<FontPoint, FontPoint> map) => new(map(this.Start), [.. this.Segments.Select(s => s.Map(map))]);

    public GlyphContour Reversed()
    {
        var segments = new List<ContourSegment>(this.Segments.Count);
        for (var i = this.Segments.Count - 1; i >= 0; i--)
        {
            var from = i == 0 ? this.Start : this.Segments[i - 1].End;
            var s = this.Segments[i];
            segments.Add(s.IsLine ? ContourSegment.Line(from) : ContourSegment.Cubic(s.C2, s.C1, from));
        }

        return new GlyphContour(this.Segments[^1].End, segments);
    }
}
