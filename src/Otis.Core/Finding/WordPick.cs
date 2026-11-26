// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Imaging;
using Otis.Core.Projects;

namespace Otis.Core.Finding;

public static class WordPick
{
    private const double Widening = 0.06;

    public static IEnumerable<PickCandidate> CandidatesOn(
        RgbImage page, string source, int pageNumber, IReadOnlyList<ReadWord> words, IReadOnlySet<string> wanted)
    {
        foreach (var word in words)
        {
            if (!word.Text.Any(c => InitialPick.GlyphOf(c.ToString()) is { } g && wanted.Contains(g)))
            {
                continue;
            }

            var margin = (int)Math.Round(word.Box.Height * Widening);
            var box = page.Clip(new PixelRect(word.Box.Left - margin, word.Box.Top - margin, word.Box.Right + margin, word.Box.Bottom + margin));
            foreach (var letter in WordSplitter.Split(page, source, pageNumber, box, word.Text))
            {
                if (letter.Glyph is { } glyph && wanted.Contains(glyph))
                {
                    yield return new PickCandidate(glyph, letter.Cutout, Confidence: 100, QualityScore.Of(page, letter.Cutout));
                }
            }
        }
    }
}
