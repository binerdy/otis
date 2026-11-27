// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.ApiTests.Support;
using Otis.Core.Finding;
using Otis.Core.Projects;
using Otis.Infrastructure.Recognition;
using Otis.Infrastructure.Storage;

namespace Otis.ApiTests;

public sealed class WordReadingTests
{
    [Fact]
    public void ATallPageIsReadInThreeOverlappingStrips()
    {
        var strips = PageStrip.For(3300, 4680);

        Assert.Equal(3, strips.Count);
        Assert.Equal(0, strips[0].Area.Top);
        Assert.Equal(4680, strips[^1].Area.Bottom);
        Assert.True(strips[1].Area.Top < strips[0].Area.Bottom, "strips overlap");
        Assert.All(strips, s => Assert.True(Math.Max(s.ImageWidth, s.ImageHeight) <= PageStrip.LongestSide));
    }

    [Fact]
    public void ASmallImageIsSentWhole()
    {
        var strip = Assert.Single(PageStrip.For(1200, 900));

        Assert.Equal(new PixelRect(0, 0, 1200, 900), strip.Area);
        Assert.Equal(1.0, strip.Scale);
    }

    [Fact]
    public void BoxesAreMappedBackToPagePixels()
    {
        var strip = new PageStrip(new PixelRect(0, 1000, 3000, 2000), 0.5);

        var words = strip.ParseWords("""{"words":[{"text":"Hand","box":[100,50,300,120]},{"text":"x","box":[5,5,5,5]}]}""");

        var word = Assert.Single(words);
        Assert.Equal("Hand", word.Text);
        Assert.Equal(new PixelRect(200, 1100, 600, 1240), word.Box);
    }

    [Fact]
    public void ReversedOrOversizedBoxesAreTidied()
    {
        var strip = new PageStrip(new PixelRect(0, 0, 1000, 500), 1.0);

        var word = Assert.Single(strip.ParseWords("""{"words":[{"text":"ſo","box":[400,90,200,10]},{"text":"","box":[1,1,9,9]}]}"""));

        Assert.Equal(new PixelRect(200, 10, 400, 90), word.Box);
    }

    [Fact]
    public void AWordReadInTwoStripsIsKeptOnce()
    {
        var words = ReadWords.WithoutRepeats(
        [
            new ReadWord("Werk", new PixelRect(100, 100, 300, 160)),
            new ReadWord("Werk", new PixelRect(104, 98, 302, 158)),
            new ReadWord("Hand", new PixelRect(400, 100, 600, 160)),
        ]);

        Assert.Equal(["Werk", "Hand"], words.Select(w => w.Text));
    }

    [Fact]
    public void TheKeyIsNotStoredAsPlainText()
    {
        using var folder = new TempDirectory();
        var store = new ApiKeyStore(folder.Path);
        const string key = "sk-ant-test-0123456789";

        store.Save(key);

        Assert.True(store.HasKey);
        Assert.Equal(key, store.Load());
        Assert.DoesNotContain(key, File.ReadAllText(folder.File("anthropic.key")), StringComparison.Ordinal);
        store.Remove();
        Assert.False(store.HasKey);
        Assert.Null(store.Load());
    }

    [Fact]
    public void ReadWordsBecomeDraftsOnlyForMissingLetters()
    {
        var line = new SampleLine("Hand", 150, 300);
        using var page = new TestPage(1400, 500);
        line.Draw(page);
        var image = page.ToImage();
        var words = new[] { new ReadWord("Hand", line.WordBox()) };

        var candidates = WordPick.CandidatesOn(image, "page.png", 1, words, new HashSet<string>(StringComparer.Ordinal) { "a", "d" }).ToList();

        Assert.Equal(["a", "d"], candidates.Select(c => c.Glyph));
        foreach (var candidate in candidates)
        {
            var index = candidate.Glyph == "a" ? 1 : 3;
            var (left, right) = line.Span(index);
            var centre = (candidate.Cutout.Bounds.Left + candidate.Cutout.Bounds.Right) / 2.0;
            Assert.InRange(centre, left, right);
        }
    }

    [Fact]
    public async Task AFakeReaderFeedsThePickWithoutNetwork()
    {
        var line = new SampleLine("nonnon", 150, 300);
        using var page = new TestPage(1400, 500);
        line.Draw(page);
        var image = page.ToImage();
        IWordReader reader = new FixedReader([new ReadWord("nonnon", line.WordBox())]);

        var words = await reader.ReadAsync(image, TestContext.Current.CancellationToken);
        var best = InitialPick.Best(WordPick.CandidatesOn(image, "page.png", 1, words, new HashSet<string>(StringComparer.Ordinal) { "o" }));

        Assert.Equal(["o"], best.Keys);
    }

    private sealed class FixedReader(IReadOnlyList<ReadWord> words) : IWordReader
    {
        public Task<IReadOnlyList<ReadWord>> ReadAsync(Otis.Core.Imaging.RgbImage page, CancellationToken cancellationToken) => Task.FromResult(words);
    }
}
