// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.ApiTests.Support;
using Otis.Core.Finding;
using Otis.Core.Ink;

namespace Otis.ApiTests;

public sealed class WordSplitterTests
{
    [Theory]
    [InlineData("nonnon")]
    [InlineData("Hand")]
    [InlineData("mitten")]
    public void EachLetterGetsItsOwnInk(string word)
    {
        var line = new SampleLine(word, 150, 300);
        using var page = new TestPage(1400, 500);
        line.Draw(page);
        var image = page.ToImage();

        var letters = WordSplitter.Split(image, "page.png", 1, line.WordBox(), word);

        Assert.Equal(word.Length, letters.Count);
        for (var i = 0; i < word.Length; i++)
        {
            Assert.Equal(word[i], letters[i].Character);
            var ink = CutoutExtractor.Extract(image, letters[i].Cutout);
            var (left, _, right, _) = ink.Mask.Bounds()!.Value;
            var (spanLeft, spanRight) = line.Span(i);
            var centre = ink.Left + ((left + right) / 2.0);
            Assert.InRange(centre, spanLeft, spanRight);
        }
    }

    [Fact]
    public void SpacesSplitWordsAtTheWidestGap()
    {
        var line = new SampleLine("Hand Werk", 130, 300);
        using var page = new TestPage(1600, 500);
        line.Draw(page);

        var letters = WordSplitter.Split(page.ToImage(), "page.png", 1, line.WordBox(), "Hand Werk");

        Assert.Equal("HandWerk", string.Concat(letters.Select(l => l.Character)));
        var (mLeft, _) = line.Span(5);
        Assert.True(letters[4].Cutout.Bounds.Left >= mLeft - 40, "M starts after the gap");
        Assert.True(letters[3].Cutout.Bounds.Right <= mLeft, "s ends before M");
    }

    [Fact]
    public void LettersShareTheDetectedGuideLines()
    {
        var line = new SampleLine("nonnon", 150, 300);
        using var page = new TestPage(1400, 500);
        line.Draw(page);

        var letters = WordSplitter.Split(page.ToImage(), "page.png", 1, line.WordBox(), "nonnon");

        Assert.All(letters, l => Assert.InRange(l.Cutout.Baseline, line.Baseline - 3, line.Baseline + 3));
        Assert.All(letters, l => Assert.InRange(l.Cutout.XLine, line.XLine - 3, line.XLine + 3));
    }

    [Theory]
    [InlineData('a', "a")]
    [InlineData('ſ', "s.alt")]
    [InlineData('ü', "udieresis")]
    public void CharactersMapToGlyphs(char character, string glyph) =>
        Assert.Equal(glyph, new WordLetter(character, Core.Projects.Cutout.Rectangle("p", 1, new(0, 0, 1, 1), 1, 0)).Glyph);
}
