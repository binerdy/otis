// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Projects;
using SkiaSharp;

namespace Otis.ApiTests.Support;

// A line of text drawn with a system font, remembering where each character went.
internal sealed class SampleLine
{
    private const string Family = "Times New Roman";
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

    public (float Left, float Right) Span(int index) => this.spans[index];

    public PixelRect WordBox()
    {
        var height = this.Baseline - this.XLine;
        return new PixelRect((int)this.spans[0].Left - 6, this.XLine - height, (int)this.spans[^1].Right + 6, this.Baseline + (height / 2));
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
