// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Glyphs;
using SkiaSharp;

namespace Otis.Infrastructure.Fonts;

public static class SpecimenRenderer
{
    private const float Size = 110;
    private const float LineHeight = Size * 2.2f;
    private const float Margin = Size;

    // Rendered with the built file itself, so the specimen shows exactly what Word will show.
    public static byte[] Render(ReadOnlySpan<byte> font, string sampleText)
    {
        using var data = SKData.CreateCopy(font);
        using var typeface = SKTypeface.FromData(data);
        using var skFont = new SKFont(typeface, Size) { Subpixel = true, Edging = SKFontEdging.Antialias };

        var lines = CharacterLines(skFont).Concat(sampleText.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0)).ToList();
        var width = (int)(lines.Max(l => skFont.MeasureText(l)) + (2 * Margin));
        var height = (int)((lines.Count * LineHeight) + Margin);

        using var bitmap = new SKBitmap(Math.Max(width, 200), Math.Max(height, 200));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        using var ink = new SKPaint { Color = SKColors.Black, IsAntialias = true };
        using var guide = new SKPaint { Color = new SKColor(205, 225, 255), StrokeWidth = 1 };
        for (var i = 0; i < lines.Count; i++)
        {
            var baseline = (Size * 1.4f) + (i * LineHeight);
            canvas.DrawLine(0, baseline, bitmap.Width, baseline, guide);
            canvas.DrawText(lines[i], Margin, baseline, SKTextAlign.Left, skFont, ink);
        }

        using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return png.ToArray();
    }

    private static IEnumerable<string> CharacterLines(SKFont font) =>
        CharacterSet.Groups
            .Where(g => g.Kind != CharacterGroupKind.BuildingBlocks)
            .Select(g => string.Concat(g.Glyphs.Select(GlyphNames.CharacterOf).OfType<char>().Where(c => font.ContainsGlyph(c))))
            .Where(l => l.Length > 0);
}
