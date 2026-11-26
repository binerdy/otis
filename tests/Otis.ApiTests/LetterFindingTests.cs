// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.ApiTests.Support;
using Otis.Core.Finding;
using Otis.Core.Projects;

namespace Otis.ApiTests;

public sealed class LetterFindingTests
{
    [Fact]
    public void LinesForANewBoxSitOnTheLowercaseBody()
    {
        var line = new SampleLine("nonnon", 160, 300);
        using var page = new TestPage(1400, 500);
        line.Draw(page);

        var found = GuideLineFinder.Around(page.ToImage(), line.Box(1));

        Assert.NotNull(found);
        Assert.InRange(found.Value.Baseline, line.Baseline - 3, line.Baseline + 3);
        Assert.InRange(found.Value.XLine, line.XLine - 3, line.XLine + 3);
    }

    [Fact]
    public void CapitalTakesTheLinesOfItsNeighbours()
    {
        var line = new SampleLine("Hnnnnn", 160, 300);
        using var page = new TestPage(1400, 500);
        line.Draw(page);

        var found = GuideLineFinder.Around(page.ToImage(), line.Box(0));

        Assert.NotNull(found);
        Assert.InRange(found.Value.XLine, line.XLine - 4, line.XLine + 4);
    }

    [Fact]
    public void OtherExamplesAreFoundBeforeOtherLetters()
    {
        var first = new SampleLine("banana banana", 110, 200);
        using var page = new TestPage(1800, 400);
        first.Draw(page);
        var image = page.ToImage();
        var reference = first.Cutout(1);

        var matches = SimilarFinder.Best(SimilarFinder.SearchPage(SimilarFinder.TemplateOf(image, reference), "page.png", 1, PageInkMap.Of(image)), SimilarFinder.TemplateOf(image, reference));

        var otherAs = first.IndexesOf('a').Where(i => i != 1).ToList();
        Assert.True(matches.Count >= otherAs.Count);
        Assert.All(matches.Take(otherAs.Count), m => Assert.Equal('a', first.CharacterAt(m.Box)));
    }

    [Fact]
    public void LargerWritingIsFoundToo()
    {
        var small = new SampleLine("banana", 100, 180);
        var large = new SampleLine("nanna", 150, 420);
        using var page = new TestPage(1400, 560);
        small.Draw(page);
        large.Draw(page);
        var image = page.ToImage();
        var template = SimilarFinder.TemplateOf(image, small.Cutout(1));

        var matches = SimilarFinder.Best(SimilarFinder.SearchPage(template, "page.png", 1, PageInkMap.Of(image)), template);

        Assert.Contains(matches, m => large.CharacterAt(m.Box) == 'a');
    }

    [Fact]
    public void CleanLetterScoresHigherThanOneTouchingItsNeighbour()
    {
        using var page = new TestPage(600, 300);
        page.Rect(100, 100, 130, 200).Rect(300, 100, 330, 200).Rect(330, 140, 400, 150);
        var image = page.ToImage();

        var clean = QualityScore.Of(image, Cutout.Rectangle("page.png", 1, new PixelRect(90, 90, 140, 210), 200, 120));
        var touched = QualityScore.Of(image, Cutout.Rectangle("page.png", 1, new PixelRect(290, 90, 340, 210), 200, 120));

        Assert.True(clean.Score > touched.Score, $"{clean.Score} should beat {touched.Score}");
        Assert.True(clean.Isolation > touched.Isolation);
    }

    [Fact]
    public void WholeStrokeScoresHigherThanABrokenOne()
    {
        using var page = new TestPage(600, 300);
        page.Rect(100, 100, 120, 200).Rect(300, 100, 320, 145).Rect(300, 155, 320, 200);
        var image = page.ToImage();

        var whole = QualityScore.Of(image, Cutout.Rectangle("page.png", 1, new PixelRect(90, 90, 130, 210), 200, 120) with { Closing = 0 });
        var broken = QualityScore.Of(image, Cutout.Rectangle("page.png", 1, new PixelRect(290, 90, 330, 210), 200, 120) with { Closing = 0 });

        Assert.True(whole.Score > broken.Score, $"{whole.Score} should beat {broken.Score}");
        Assert.True(whole.Completeness > broken.Completeness);
    }
}
