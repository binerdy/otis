// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.Core.Glyphs;

public static class CharacterSet
{
    public static IReadOnlyList<CharacterGroup> Groups { get; } =
    [
        new(CharacterGroupKind.Capitals, Names("ABCDEFGHIJKLMNOPQRSTUVWXYZÄÖÜ")),
        new(CharacterGroupKind.Lowercase, Names("abcdefghijklmnopqrstuvwxyzäöüß")),
        new(CharacterGroupKind.DigitsAndPunctuation, Names("0123456789.,-:;'()&!?")),
        new(CharacterGroupKind.BuildingBlocks, ["s.alt", "dotaccent.src"]),
    ];

    public static IEnumerable<string> All => Groups.SelectMany(g => g.Glyphs);

    public static bool IsBuildingBlock(string glyph) => Groups[^1].Glyphs.Contains(glyph);

    private static string[] Names(string characters) => [.. characters.Select(GlyphNames.NameOf)];
}
