// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.Core.Projects;

public sealed record InkStroke(int Width, IReadOnlyList<PixelPoint> Points)
{
    public InkStroke Offset(int dx, int dy) => this with { Points = [.. this.Points.Select(p => new PixelPoint(p.X + dx, p.Y + dy))] };

    public bool Equals(InkStroke? other) => other is not null && other.Width == this.Width && other.Points.SequenceEqual(this.Points);

    public override int GetHashCode() => HashCode.Combine(this.Width, this.Points.Count);
}
