// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.ApiTests.Support;
using Otis.Core.Finding;
using Otis.Core.Projects;
using SkiaSharp;

namespace Otis.ApiTests;

public sealed class LetterFindingTests
{
    private const string Family = "Times New Roman";

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

    private sealed class SampleLine
    {
        private readonly string text;
        private readonly float size;
        private readonly List<(float Left, float Right)> spans = [];

        public SampleLine(string text, float size, int baseline)
        {
            this.text = text;
            this.size = size;
            this.Baseline = baseline;
            using var typeface = SKTypeface.FromFamilyName(Family);
            using var font = new SKFont(typeface, size);
            using var x = font.GetTextPath("x");
            this.XLine = (int)Math.Round(baseline + x.Bounds.Top);
            var cursor = 40f;
            foreach (var c in text)
            {
                var width = font.MeasureText(c.ToString());
                this.spans.Add((cursor, cursor + width));
                cursor += width;
            }
        }

        public int Baseline { get; }

        public int XLine { get; }

        public void Draw(TestPage page) => page.Text(this.text, 40, this.Baseline, this.size, Family);

        public PixelRect Box(int index)
        {
            var (left, right) = this.spans[index];
            var height = this.Baseline - this.XLine;
            return new PixelRect((int)left - 2, this.XLine - (height / 3), (int)right + 2, this.Baseline + (height / 6));
        }

        public Cutout Cutout(int index) => Core.Projects.Cutout.Rectangle("page.png", 1, this.Box(index), this.Baseline, this.XLine);

        public IEnumerable<int> IndexesOf(char c) => Enumerable.Range(0, this.text.Length).Where(i => this.text[i] == c);

        public char? CharacterAt(PixelRect box)
        {
            if (box.Bottom < this.XLine || box.Top > this.Baseline)
            {
                return null;
            }

            var centre = (box.Left + box.Right) / 2f;
            var index = this.spans.FindIndex(s => centre >= s.Left && centre < s.Right);
            return index < 0 ? null : this.text[index];
        }
    }
}
