// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.ApiTests.Support;
using Otis.Core.Building;
using Otis.Core.Imaging;
using Otis.Core.Projects;
using Otis.Core.TrueType;
using Otis.Infrastructure.Fonts;
using Otis.Infrastructure.Outlines;
using SkiaSharp;

namespace Otis.ApiTests;

public sealed class FontBuildingTests
{
    private static readonly DateTimeOffset BuiltAt = new(2026, 11, 26, 9, 0, 0, TimeSpan.Zero);
    private static readonly FontVersion Version = FontVersion.Parse("1.004");

    [Fact]
    public void WordGetsTheFontNotAFallback()
    {
        var font = Build("aenoJA·");

        Assert.Equal("Otis Sample", WindowsFontCheck.FaceForWesternText(font.FontFile.Span, "Otis Sample"));
    }

    [Fact]
    public void FontCarriesWhatWindowsNeeds()
    {
        var tables = new FontTables(Build("aeno").FontFile.ToArray());

        Assert.Equal(1u, tables.Os2Long(78) & 1);
        Assert.Equal(0, tables.Os2(8));
        Assert.Equal("Otis Sample", tables.Name(1));
        Assert.Equal("Regular", tables.Name(2));
        Assert.Equal("Version 1.004", tables.Name(5));
        Assert.Equal("1.004;OTIS;OtisSample-Regular", tables.Name(3));
        Assert.Equal("OtisSample-Regular", tables.Name(6));
    }

    [Fact]
    public void ChecksumsAndTablesAreValid()
    {
        var bytes = Build("aeno").FontFile.ToArray();
        var tables = new FontTables(bytes);

        Assert.Equal(0xB1B0AFBAu, TrueTypeWriter.Checksum(bytes));
        Assert.Equal(["OS/2", "cmap", "gasp", "glyf", "head", "hhea", "hmtx", "loca", "maxp", "name", "post"], tables.Tags());
        Assert.Equal(0, bytes.Length % 4);
    }

    [Fact]
    public void WindowsMetricsCoverEveryGlyph()
    {
        var tables = new FontTables(Build("aenoJA").FontFile.ToArray());
        var head = tables.Table("head");
        var yMax = (short)((head[42] << 8) | head[43]);
        var yMin = (short)((head[38] << 8) | head[39]);

        Assert.True(tables.Os2(74) >= yMax);
        Assert.True(tables.Os2(76) >= -yMin);
    }

    [Fact]
    public void DerivedGlyphsAreMappedAndMissingOnesAreNot()
    {
        var build = Build("aenoJ·");

        using var typeface = Typeface(build);
        using var font = new SKFont(typeface, 100);
        Assert.True(font.ContainsGlyphs("aeno"));
        Assert.True(font.ContainsGlyphs("äö"));
        Assert.True(font.ContainsGlyphs("I"));
        Assert.False(font.ContainsGlyphs("O"));
        Assert.Contains("O", build.Coverage.MissingGlyphs);
    }

    [Fact]
    public void CounterOfAnORendersAsAHole()
    {
        using var typeface = Typeface(Build("o"));
        using var font = new SKFont(typeface, 400);
        using var bitmap = new SKBitmap(500, 500);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        using var paint = new SKPaint { Color = SKColors.Black };
        canvas.DrawText("o", 50, 400, SKTextAlign.Left, font, paint);
        using var path = font.GetTextPath("o", new SKPoint(50, 400));

        var centre = bitmap.GetPixel((int)path.Bounds.MidX, (int)path.Bounds.MidY);
        var stroke = bitmap.GetPixel((int)path.Bounds.Left + 8, (int)path.Bounds.MidY);

        Assert.Equal(SKColors.White, centre);
        Assert.Equal(SKColors.Black, stroke);
    }

    [Fact]
    public void LettersFromDifferentLineHeightsMatch()
    {
        var (bigPage, bigProject) = SampleLettering.Create("n", size: 200, baseline: 300, source: "big.png");
        var (smallPage, smallProject) = SampleLettering.Create("x", size: 100, baseline: 200, source: "small.png");
        var project = bigProject.WithCutout("x", smallProject.Cutouts["x"]) with { Sources = ["big.png", "small.png"] };
        var pages = new MemoryPageSource(new() { [("big.png", 1)] = bigPage, [("small.png", 1)] = smallPage });

        var build = FontBuilder.Build(project, Version, BuiltAt, pages, new SkiaOverlapRemover());

        using var typeface = Typeface(build);
        using var font = new SKFont(typeface, 1000);
        Assert.InRange(Height(font, "n"), 380, 430);
        Assert.InRange(Height(font, "x"), 380, 430);
    }

    [Fact]
    public void SameProjectAndTimeGiveIdenticalFiles()
    {
        Assert.Equal(Build("aeno").FontFile.ToArray(), Build("aeno").FontFile.ToArray());
    }

    [Fact]
    public void MissingSourceIsReportedNotFatal()
    {
        var (page, project) = SampleLettering.Create("ae");
        project = project.WithCutout("o", project.Cutouts["a"] with { Source = "gone.png" });

        var build = FontBuilder.Build(project, Version, BuiltAt, Pages(page), new SkiaOverlapRemover());

        Assert.Contains(build.Problems, p => p.Contains("gone.png", StringComparison.Ordinal));
    }

    private static FontBuild Build(string letters)
    {
        var (page, project) = SampleLettering.Create(letters);
        return FontBuilder.Build(project, Version, BuiltAt, Pages(page), new SkiaOverlapRemover());
    }

    private static MemoryPageSource Pages(RgbImage page) => new(new() { [(SampleLettering.Source, 1)] = page });

    private static SKTypeface Typeface(FontBuild build)
    {
        using var data = SKData.CreateCopy(build.FontFile.Span);
        return SKTypeface.FromData(data);
    }

    private static float Height(SKFont font, string text)
    {
        using var path = font.GetTextPath(text);
        return path.Bounds.Height;
    }
}
