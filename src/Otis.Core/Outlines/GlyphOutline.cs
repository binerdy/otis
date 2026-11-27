// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.Core.Outlines;

public sealed record GlyphOutline(IReadOnlyList<GlyphContour> Contours)
{
    public static GlyphOutline Empty { get; } = new([]);

    public bool IsEmpty => this.Contours.Count == 0;

    public (double XMin, double YMin, double XMax, double YMax) Bounds()
    {
        var points = this.Contours.SelectMany(c => c.Segments.SelectMany(s => s.IsLine ? [s.End] : new[] { s.C1, s.C2, s.End }).Append(c.Start)).ToList();
        return points.Count == 0
            ? (0, 0, 0, 0)
            : (points.Min(p => p.X), points.Min(p => p.Y), points.Max(p => p.X), points.Max(p => p.Y));
    }

    public GlyphOutline Transform(double scale, double dx, double dy) =>
        new([.. this.Contours.Select(c => c.Map(p => new FontPoint((p.X * scale) + dx, (p.Y * scale) + dy)))]);

    public GlyphOutline Combine(GlyphOutline other) => new([.. this.Contours, .. other.Contours]);
}
