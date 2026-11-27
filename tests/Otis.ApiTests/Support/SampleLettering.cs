// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Imaging;
using Otis.Core.Projects;
using SkiaSharp;

namespace Otis.ApiTests.Support;

// Draws letters with a system font onto a page at test time, so the repository holds no scans.
internal static class SampleLettering
{
    public const string Source = "sample.png";
    private const string Family = "Times New Roman";

    public static (RgbImage Page, OtisProject Project) Create(string letters, float size = 200, int baseline = 300, string source = Source)
    {
        using var typeface = SKTypeface.FromFamilyName(Family);
        using var font = new SKFont(typeface, size);
        var widths = letters.Select(c => font.MeasureText(c.ToString())).ToList();
        var width = (int)(widths.Sum() + (letters.Length * size * 0.6f) + size);
        using var page = new TestPage(width, baseline + (int)size);

        var xHeight = XHeight(font);
        var project = OtisProject.Create("Otis Sample") with { Sources = [source] };
        var x = size / 2;
        for (var i = 0; i < letters.Length; i++)
        {
            var letter = letters[i];
            page.Text(letter.ToString(), x, baseline, size, Family);
            var box = new PixelRect((int)(x - (size * 0.15)), (int)(baseline - (size * 1.05)), (int)(x + widths[i] + (size * 0.15)), (int)(baseline + (size * 0.35)));
            var name = letter == '·' ? "dotaccent.src" : Otis.Core.Glyphs.GlyphNames.NameOf(letter);
            project = project.WithCutout(name, Cutout.Rectangle(source, 1, box, baseline, (int)Math.Round(baseline - xHeight)));
            x += widths[i] + (size * 0.6f);
        }

        return (page.ToImage(), project);
    }

    private static float XHeight(SKFont font)
    {
        using var path = font.GetTextPath("x");
        return -path.Bounds.Top;
    }
}
