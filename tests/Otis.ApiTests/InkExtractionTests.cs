// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.ApiTests.Support;
using Otis.Core.Imaging;
using Otis.Core.Ink;
using Otis.Core.Projects;
using SkiaSharp;

namespace Otis.ApiTests;

public sealed class InkExtractionTests
{
    [Fact]
    public void ShadedCapitalStaysWhole()
    {
        using var page = new TestPage();
        for (var y = 100; y < 300; y++)
        {
            var t = (y - 100) / 200f;
            var color = new SKColor((byte)(180 - (150 * t)), 30, (byte)(30 + (60 * t)));
            page.Rect(100, y, 130, y + 1, color);
        }

        var ink = Extract(page, new PixelRect(80, 80, 150, 320));

        Assert.Equal(1, Parts(ink));
        AssertSize(ink, 200, 30);
    }

    [Fact]
    public void PaperTextureIsNotInk()
    {
        using var page = new TestPage();
        for (var x = 100; x < 200; x += 7)
        {
            page.Rect(x, 100, x + 2, 200, new SKColor(205, 203, 198));
        }

        var ink = Extract(page, new PixelRect(90, 90, 210, 210));

        Assert.False(ink.Mask.Any());
    }

    [Fact]
    public void NotchedCapitalLeavesTheNextLetterOut()
    {
        using var page = new TestPage();
        page.Rect(100, 100, 160, 300).Rect(160, 200, 200, 300);
        var notched = new Cutout
        {
            Source = "page.png",
            Page = 1,
            Outline = [new(90, 90), new(220, 90), new(220, 195), new(160, 195), new(160, 310), new(90, 310)],
            Baseline = 300,
            XLine = 200,
        };

        var ink = CutoutExtractor.Extract(page.ToImage(), notched);

        AssertSize(ink, 200, 60);
    }

    [Fact]
    public void ErasureRemovesTheNeighboursFragment()
    {
        using var page = new TestPage();
        page.Rect(100, 100, 160, 300).Rect(170, 150, 185, 300);
        var cutout = Cutout.Rectangle("page.png", 1, new PixelRect(90, 90, 200, 310), 300, 200) with
        {
            Erasures = [new PixelRect(166, 140, 200, 310)],
        };

        var ink = CutoutExtractor.Extract(page.ToImage(), cutout);

        AssertSize(ink, 200, 60);
    }

    [Fact]
    public void LetterIKeepsItsDot()
    {
        using var page = new TestPage();
        page.Rect(100, 150, 120, 300).Circle(110, 120, 12);

        var ink = Extract(page, new PixelRect(85, 95, 135, 310));

        Assert.Equal(2, Parts(ink));
    }

    [Fact]
    public void SpecksAreDropped()
    {
        using var page = new TestPage();
        page.Rect(100, 100, 140, 300).Rect(170, 280, 173, 283);

        var ink = Extract(page, new PixelRect(90, 90, 200, 310));

        Assert.Equal(1, Parts(ink));
        AssertSize(ink, 200, 40);
    }

    [Fact]
    public void ExtractionIsDeterministic()
    {
        using var page = new TestPage();
        page.Text("ae", 100, 300, 160);
        var image = page.ToImage();
        var cutout = Cutout.Rectangle("page.png", 1, new PixelRect(90, 150, 300, 320), 300, 225);

        Assert.Equal(CutoutExtractor.Extract(image, cutout).Mask, CutoutExtractor.Extract(image, cutout).Mask);
    }

    private static CutoutInk Extract(TestPage page, PixelRect box) =>
        CutoutExtractor.Extract(page.ToImage(), Cutout.Rectangle("page.png", 1, box, box.Bottom, box.Top));

    private static int Parts(CutoutInk ink)
    {
        ConnectedParts.Label(ink.Mask, out var count);
        return count;
    }

    // The blur widens strokes by about a pixel on each side, as it should for smooth outlines.
    private static void AssertSize(CutoutInk ink, int height, int width)
    {
        var (left, top, right, bottom) = ink.Mask.Bounds()!.Value;
        Assert.InRange(bottom - top, height, height + 2);
        Assert.InRange(right - left, width, width + 2);
    }
}
