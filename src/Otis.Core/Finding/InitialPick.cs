// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Glyphs;
using Otis.Core.Projects;

namespace Otis.Core.Finding;

public static class InitialPick
{
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
}
