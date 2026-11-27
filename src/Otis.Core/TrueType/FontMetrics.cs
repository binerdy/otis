// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.Core.TrueType;

internal sealed record FontMetrics(int XMin, int YMin, int XMax, int YMax, int Ascender, int Descender)
{
    public int WinAscent => Math.Max(0, this.Ascender);

    public int WinDescent => Math.Max(0, -this.Descender);

    // Flourished capitals reach far above and below; every metric covers them so Word never clips a swash.
    public static FontMetrics Of(FontDescription font, IReadOnlyList<TrueTypeGlyph> glyphs)
    {
        var drawn = glyphs.Where(g => !g.IsEmpty).ToList();
        if (drawn.Count == 0)
        {
            return new FontMetrics(0, 0, 0, 0, font.UnitsPerEm * 8 / 10, -font.UnitsPerEm * 2 / 10);
        }

        var yMax = drawn.Max(g => g.YMax);
        var yMin = drawn.Min(g => g.YMin);
        return new FontMetrics(
            drawn.Min(g => g.XMin),
            yMin,
            drawn.Max(g => g.XMax),
            yMax,
            Math.Max(yMax, font.CapHeight),
            Math.Min(yMin, -font.UnitsPerEm / 5));
    }
}
