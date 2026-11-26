// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text.Json.Serialization;

namespace Otis.Core.Projects;

[JsonConverter(typeof(PixelRectConverter))]
public readonly record struct PixelRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => this.Right - this.Left;

    public int Height => this.Bottom - this.Top;

    public static PixelRect Around(IEnumerable<PixelPoint> points)
    {
        var list = points.ToList();
        return new PixelRect(list.Min(p => p.X), list.Min(p => p.Y), list.Max(p => p.X), list.Max(p => p.Y));
    }

    public PixelRect Offset(int dx, int dy) => new(this.Left + dx, this.Top + dy, this.Right + dx, this.Bottom + dy);
}
