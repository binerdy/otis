// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.ApiTests.Support;
using Otis.Core.Finding;
using Otis.Core.Projects;
using Otis.Infrastructure.Recognition;

namespace Otis.ApiTests;

public sealed class InitialPickTests
{
    [Fact]
    public void OnlyMissingCharactersAreWanted()
    {
        var project = OtisProject.Create("Kanzlei Fraktur")
            .WithCutout("a", Cutout.Rectangle("p.png", 1, new PixelRect(0, 0, 10, 10), 9, 4))
            .WithCutout("dotaccent.src", Cutout.Rectangle("p.png", 1, new PixelRect(0, 0, 10, 10), 9, 4));

        var wanted = InitialPick.Wanted(project);

        Assert.DoesNotContain("a", wanted);
        Assert.DoesNotContain("adieresis", wanted);
        Assert.Contains("b", wanted);
        Assert.Contains("s.alt", wanted);
    }

    [Theory]
    [InlineData("a", "a")]
    [InlineData("ſ", "s.alt")]
    [InlineData("7", "seven")]
    [InlineData("€", null)]
    [InlineData("ch", null)]
    public void RecognisedTextMapsToGlyphs(string text, string? glyph) => Assert.Equal(glyph, InitialPick.GlyphOf(text));

    [Fact]
    public void BestCandidateWinsAndGarbledWordsAreIgnored()
    {
        using var page = new TestPage(900, 400);
        page.Text("nonnon", 60, 250, 140);
        var image = page.ToImage();
        RecognizedLetter[] letters =
        [
            new("o", new PixelRect(130, 175, 200, 252), 96, 92),
            new("o", new PixelRect(340, 175, 410, 252), 99, 30),
        ];

        var candidates = InitialPick.CandidatesOn(image, "p.png", 1, letters, new HashSet<string>(["o"])).ToList();

        var only = Assert.Single(candidates);
        Assert.Equal(92, only.Confidence);
        Assert.Equal("o", Assert.Single(InitialPick.Best(candidates)).Key);
    }

    [Fact]
    public void BoxesStopHalfwayToTheNeighbours()
    {
        using var page = new TestPage(900, 400);
        page.Text("nonnon", 60, 250, 140);
        RecognizedLetter[] letters =
        [
            new("n", new PixelRect(60, 175, 130, 252), 95, 95),
            new("o", new PixelRect(140, 175, 200, 252), 95, 95),
            new("n", new PixelRect(210, 175, 280, 252), 95, 95),
        ];

        var o = InitialPick.CandidatesOn(page.ToImage(), "p.png", 1, letters, new HashSet<string>(["o"])).Single();

        Assert.Equal(135, o.Cutout.Bounds.Left);
        Assert.Equal(205, o.Cutout.Bounds.Right);
    }

    [Fact]
    public void LettersOfAnotherSizeAreSkippedOnceAStyleExists()
    {
        using var page = new TestPage(900, 400);
        page.Text("nonnon", 60, 250, 60);
        RecognizedLetter[] letters = [new("o", new PixelRect(90, 218, 115, 252), 95, 95)];

        Assert.Empty(InitialPick.CandidatesOn(page.ToImage(), "p.png", 1, letters, new HashSet<string>(["o"]), targetXHeight: 90));
    }

    [Fact]
    public void BundledTesseractReadsDigits()
    {
        using var page = new TestPage(1600, 500);
        page.Text("1234567890", 80, 300, 220);
        using var recognizer = new TesseractRecognizer(Path.Combine(AppContext.BaseDirectory, "tessdata"), TesseractRecognizer.BundledModel);

        var letters = recognizer.Recognize(page.ToImage(), TestContext.Current.CancellationToken);

        var text = string.Concat(letters.Select(l => l.Text));
        Assert.True(text.Count(char.IsDigit) >= 8, $"read '{text}'");
        Assert.All(letters, l => Assert.InRange(l.Box.Top, 100, 330));
    }
}
