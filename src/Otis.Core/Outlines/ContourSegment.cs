// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.Core.Outlines;

public readonly record struct ContourSegment(bool IsLine, FontPoint C1, FontPoint C2, FontPoint End)
{
    public static ContourSegment Line(FontPoint end) => new(IsLine: true, end, end, end);

    public static ContourSegment Cubic(FontPoint c1, FontPoint c2, FontPoint end) => new(IsLine: false, c1, c2, end);

    public ContourSegment Map(Func<FontPoint, FontPoint> map) => new(this.IsLine, map(this.C1), map(this.C2), map(this.End));
}
