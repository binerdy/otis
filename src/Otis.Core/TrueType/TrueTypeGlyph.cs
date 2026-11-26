// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Outlines;

namespace Otis.Core.TrueType;

public sealed record TrueTypeGlyph(string Name, IReadOnlyList<IReadOnlyList<GlyphPoint>> Contours, int AdvanceWidth)
{
    private const double MaxCurveError = 1.0;

    public bool IsEmpty => this.Contours.Count == 0;

    public int PointCount => this.Contours.Sum(c => c.Count);

    public int XMin => this.IsEmpty ? 0 : this.Contours.SelectMany(c => c).Min(p => p.X);

    public int XMax => this.IsEmpty ? 0 : this.Contours.SelectMany(c => c).Max(p => p.X);

    public int YMin => this.IsEmpty ? 0 : this.Contours.SelectMany(c => c).Min(p => p.Y);

    public int YMax => this.IsEmpty ? 0 : this.Contours.SelectMany(c => c).Max(p => p.Y);

    public static TrueTypeGlyph From(string name, GlyphOutline outline, int advanceWidth) =>
        new(name, [.. outline.Contours.Select(ToPoints).Where(c => c.Count >= 3)], advanceWidth);

    private static IReadOnlyList<GlyphPoint> ToPoints(GlyphContour contour)
    {
        var points = new List<GlyphPoint> { Round(contour.Start, onCurve: true) };
        var previous = contour.Start;
        foreach (var segment in contour.Segments)
        {
            if (segment.IsLine)
            {
                points.Add(Round(segment.End, onCurve: true));
            }
            else
            {
                foreach (var (control, end) in QuadraticConverter.Convert(previous, segment.C1, segment.C2, segment.End, MaxCurveError))
                {
                    points.Add(Round(control, onCurve: false));
                    points.Add(Round(end, onCurve: true));
                }
            }

            previous = segment.End;
        }

        return Clean(points);
    }

    // Closing points and repeats that rounding produces would only add zero-length segments.
    private static List<GlyphPoint> Clean(List<GlyphPoint> points)
    {
        var cleaned = new List<GlyphPoint>(points.Count);
        foreach (var point in points)
        {
            if (cleaned.Count == 0 || cleaned[^1] != point)
            {
                cleaned.Add(point);
            }
        }

        while (cleaned.Count > 1 && cleaned[^1].X == cleaned[0].X && cleaned[^1].Y == cleaned[0].Y)
        {
            cleaned.RemoveAt(cleaned.Count - 1);
        }

        return cleaned;
    }

    private static GlyphPoint Round(FontPoint p, bool onCurve) =>
        new((int)Math.Round(p.X, MidpointRounding.AwayFromZero), (int)Math.Round(p.Y, MidpointRounding.AwayFromZero), onCurve);
}
