// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Imaging;
using Otis.Core.Ink;

namespace Otis.Core.Finding;

// Ink of a whole page at reduced resolution: enough to compare letter shapes, four times less work than the full page.
public sealed record PageInkMap(BitGrid Ink, int Factor, IReadOnlyList<GuideLines> Lines)
{
    public const int Reduction = 2;

    public bool this[double pageX, double pageY] => this.Ink[(int)(pageX / this.Factor), (int)(pageY / this.Factor)];

    public static PageInkMap Of(RgbImage page)
    {
        var ink = InkDetector.Detect(page.Shrink(Reduction));
        return new PageInkMap(ink, Reduction, TextLineFinder.Find(ink));
    }
}
