// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.Core.Projects;

public sealed record CutoutAdjustments(bool GreyTracing, SmoothingLevel Smoothing, int CloseGaps, IReadOnlyList<InkStroke> InkStrokes)
{
    public const int DefaultCloseGaps = 2;
    public const int MaxCloseGaps = 6;

    public static CutoutAdjustments None { get; } = new(GreyTracing: false, SmoothingLevel.Off, DefaultCloseGaps, []);

    public bool Equals(CutoutAdjustments? other) =>
        other is not null && other.GreyTracing == this.GreyTracing && other.Smoothing == this.Smoothing
        && other.CloseGaps == this.CloseGaps && other.InkStrokes.SequenceEqual(this.InkStrokes);

    public override int GetHashCode() => HashCode.Combine(this.GreyTracing, this.Smoothing, this.CloseGaps, this.InkStrokes.Count);
}
