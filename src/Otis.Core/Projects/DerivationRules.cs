// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Glyphs;

namespace Otis.Core.Projects;

public sealed record DerivationRules
{
    public static DerivationRules Default { get; } = new()
    {
        Umlauts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Adieresis"] = "A",
            ["Odieresis"] = "O",
            ["Udieresis"] = "U",
            ["adieresis"] = "a",
            ["odieresis"] = "o",
            ["udieresis"] = "u",
        },
        Ligatures = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal) { ["germandbls"] = ["s.alt", "z"] },
        Aliases = new Dictionary<string, string>(StringComparer.Ordinal) { ["I"] = "J" },
    };

    public string DotGlyph { get; init; } = "dotaccent.src";

    public double DotScale { get; init; } = 0.7;

    public IReadOnlyDictionary<string, string> Umlauts { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, IReadOnlyList<string>> Ligatures { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, string> Aliases { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);

    public IReadOnlyList<string> SourcesOf(string glyph)
    {
        if (this.Umlauts.TryGetValue(glyph, out var umlautBase))
        {
            return [umlautBase, this.DotGlyph];
        }

        if (this.Ligatures.TryGetValue(glyph, out var parts))
        {
            return parts;
        }

        return this.Aliases.TryGetValue(glyph, out var alias) ? [alias] : [];
    }

    public string? Describe(string glyph)
    {
        if (this.Umlauts.TryGetValue(glyph, out var umlautBase))
        {
            return GlyphNames.Display(umlautBase) + " + ··";
        }

        if (this.Ligatures.TryGetValue(glyph, out var parts))
        {
            return string.Join(" + ", parts.Select(GlyphNames.Display));
        }

        return this.Aliases.TryGetValue(glyph, out var alias) ? "= " + GlyphNames.Display(alias) : null;
    }
}
