// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Otis.Core.Projects;

public sealed record Cutout
{
    public required string Source { get; init; }

    public required int Page { get; init; }

    public required IReadOnlyList<PixelPoint> Outline { get; init; }

    public IReadOnlyList<PixelRect> Erasures { get; init; } = [];

    public required int Baseline { get; init; }

    public required int XLine { get; init; }

    public int? Closing { get; init; }

    public double? MinimumPartShare { get; init; }

    public int? RightSidebearing { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Unknown { get; init; }

    [JsonIgnore]
    public PixelRect Bounds => PixelRect.Around(this.Outline);

    public static Cutout Rectangle(string source, int page, PixelRect box, int baseline, int xLine) => new()
    {
        Source = source,
        Page = page,
        Outline = [new(box.Left, box.Top), new(box.Right, box.Top), new(box.Right, box.Bottom), new(box.Left, box.Bottom)],
        Baseline = baseline,
        XLine = xLine,
    };

    public Cutout MoveBy(int dx, int dy) => this with
    {
        Outline = [.. this.Outline.Select(p => new PixelPoint(p.X + dx, p.Y + dy))],
        Erasures = [.. this.Erasures.Select(e => e.Offset(dx, dy))],
        Baseline = this.Baseline + dy,
        XLine = this.XLine + dy,
    };
}
