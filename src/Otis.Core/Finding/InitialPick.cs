// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Glyphs;
using Otis.Core.Imaging;
using Otis.Core.Projects;

namespace Otis.Core.Finding;

public static class InitialPick
{
    public const double MinimumConfidence = 60;
    private const double SmallestSize = 0.65;
    private const double LargestSize = 1.5;
    private const double Widening = 0.15;
    private const string LongS = "ſ";

    // Only characters that are neither cut out nor derived are wanted; ſ also fills the building block for ß.
    public static IReadOnlySet<string> Wanted(OtisProject project)
    {
        var coverage = Coverage.Of(project);
        var wanted = CharacterSet.All
            .Where(g => !CharacterSet.IsBuildingBlock(g) && coverage[g] == CoverageState.Missing)
            .ToHashSet(StringComparer.Ordinal);
        if (!project.Cutouts.ContainsKey("s.alt"))
        {
            wanted.Add("s.alt");
        }

        return wanted;
    }

    public static IEnumerable<PickCandidate> CandidatesOn(
        RgbImage page, string source, int pageNumber, IReadOnlyList<RecognizedLetter> letters, IReadOnlySet<string> wanted, double? targetXHeight = null)
    {
        foreach (var letter in letters)
        {
            if (letter.Trust < MinimumConfidence || GlyphOf(letter.Text) is not { } glyph || !wanted.Contains(glyph))
            {
                continue;
            }

            var box = Widen(letter, letters, page);
            var lines = GuideLineFinder.Around(page, box);
            if (targetXHeight is { } target && (lines is not { } found || found.XHeight < target * SmallestSize || found.XHeight > target * LargestSize))
            {
                continue;
            }

            var cutout = Cutout.Rectangle(
                source,
                pageNumber,
                box,
                lines?.Baseline ?? letter.Box.Bottom,
                lines?.XLine ?? letter.Box.Top);
            yield return new PickCandidate(glyph, cutout, letter.Trust, QualityScore.Of(page, cutout));
        }
    }

    // The lettering style to complete is the one already cut out; without any letters every size counts.
    public static double? TargetXHeight(OtisProject project)
    {
        var heights = project.Cutouts.Values.Select(c => (double)(c.Baseline - c.XLine)).Where(h => h > 0).Order().ToList();
        return heights.Count == 0 ? null : heights[heights.Count / 2];
    }

    public static IReadOnlyDictionary<string, PickCandidate> Best(IEnumerable<PickCandidate> candidates) =>
        candidates.GroupBy(c => c.Glyph, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.MaxBy(c => c.Score)!, StringComparer.Ordinal);

    public static string? GlyphOf(string text)
    {
        if (text == LongS)
        {
            return "s.alt";
        }

        return text.Length == 1 && CharacterSet.All.Contains(GlyphNames.NameOf(text[0])) ? GlyphNames.NameOf(text[0]) : null;
    }

    // OCR boxes hug the ink; a little air helps, but never past halfway to the neighbouring letters.
    private static PixelRect Widen(RecognizedLetter letter, IReadOnlyList<RecognizedLetter> letters, RgbImage page)
    {
        var box = letter.Box;
        var margin = (int)Math.Round(box.Height * Widening);
        var sameRow = letters.Where(o => o != letter && VerticalOverlap(o.Box, box) > 0.5).Select(o => o.Box).ToList();
        var left = box.Left - margin;
        var right = box.Right + margin;
        foreach (var other in sameRow)
        {
            if (other.Right <= box.Left)
            {
                left = Math.Max(left, (other.Right + box.Left) / 2);
            }

            if (other.Left >= box.Right)
            {
                right = Math.Min(right, (box.Right + other.Left) / 2);
            }
        }

        return page.Clip(new PixelRect(left, box.Top - margin, right, box.Bottom + margin));
    }

    private static double VerticalOverlap(PixelRect a, PixelRect b)
    {
        var shared = Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Top, b.Top);
        return shared <= 0 ? 0 : (double)shared / Math.Min(a.Height, b.Height);
    }
}
