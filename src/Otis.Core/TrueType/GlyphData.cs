// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.Core.TrueType;

internal static class GlyphData
{
    private const int OnCurve = 0x01;
    private const int XShort = 0x02;
    private const int YShort = 0x04;
    private const int XSameOrPositive = 0x10;
    private const int YSameOrPositive = 0x20;

    public static void Write(BigEndianWriter w, TrueTypeGlyph glyph)
    {
        w.I16(glyph.Contours.Count).I16(glyph.XMin).I16(glyph.YMin).I16(glyph.XMax).I16(glyph.YMax);

        var end = -1;
        foreach (var contour in glyph.Contours)
        {
            end += contour.Count;
            w.U16(end);
        }

        w.U16(0);

        var points = glyph.Contours.SelectMany(c => c).ToList();
        var flags = new BigEndianWriter();
        var xs = new BigEndianWriter();
        var ys = new BigEndianWriter();
        int x = 0, y = 0;
        foreach (var p in points)
        {
            var flag = p.OnCurve ? OnCurve : 0;
            flag |= Delta(xs, p.X - x, XShort, XSameOrPositive);
            flag |= Delta(ys, p.Y - y, YShort, YSameOrPositive);
            flags.U8(flag);
            (x, y) = (p.X, p.Y);
        }

        w.Bytes(flags.ToArray()).Bytes(xs.ToArray()).Bytes(ys.ToArray());
    }

    private static int Delta(BigEndianWriter coordinates, int delta, int shortFlag, int sameOrPositiveFlag)
    {
        if (delta == 0)
        {
            return sameOrPositiveFlag;
        }

        if (Math.Abs(delta) <= 255)
        {
            coordinates.U8(Math.Abs(delta));
            return shortFlag | (delta > 0 ? sameOrPositiveFlag : 0);
        }

        coordinates.I16(delta);
        return 0;
    }
}
