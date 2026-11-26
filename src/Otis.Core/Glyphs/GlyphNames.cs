// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;

namespace Otis.Core.Glyphs;

public static class GlyphNames
{
    private static readonly Dictionary<char, string> Named = new()
    {
        ['0'] = "zero",
        ['1'] = "one",
        ['2'] = "two",
        ['3'] = "three",
        ['4'] = "four",
        ['5'] = "five",
        ['6'] = "six",
        ['7'] = "seven",
        ['8'] = "eight",
        ['9'] = "nine",
        ['.'] = "period",
        [','] = "comma",
        ['-'] = "hyphen",
        [':'] = "colon",
        [';'] = "semicolon",
        ['\''] = "quotesingle",
        ['('] = "parenleft",
        [')'] = "parenright",
        ['&'] = "ampersand",
        ['!'] = "exclam",
        ['?'] = "question",
        ['Ä'] = "Adieresis",
        ['Ö'] = "Odieresis",
        ['Ü'] = "Udieresis",
        ['ä'] = "adieresis",
        ['ö'] = "odieresis",
        ['ü'] = "udieresis",
        ['ß'] = "germandbls",
        ['ſ'] = "longs",
    };

    private static readonly Dictionary<string, char> ByName = Named.ToDictionary(p => p.Value, p => p.Key, StringComparer.Ordinal);

    private static readonly Dictionary<string, string> BuildingBlocks = new(StringComparer.Ordinal)
    {
        ["s.alt"] = "ſ",
        ["dotaccent.src"] = "·",
    };

    public static string NameOf(char character)
    {
        if (char.IsAsciiLetter(character))
        {
            return character.ToString();
        }

        return Named.TryGetValue(character, out var name) ? name : string.Create(CultureInfo.InvariantCulture, $"uni{(int)character:X4}");
    }

    public static char? CharacterOf(string glyph)
    {
        if (glyph.Length == 1 && char.IsAsciiLetter(glyph[0]))
        {
            return glyph[0];
        }

        if (ByName.TryGetValue(glyph, out var character))
        {
            return character;
        }

        var isUnicodeName = glyph.StartsWith("uni", StringComparison.Ordinal) && glyph.Length == 7;
        if (isUnicodeName && int.TryParse(glyph.AsSpan(3), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
        {
            return (char)code;
        }

        return null;
    }

    public static string Display(string glyph) =>
        BuildingBlocks.TryGetValue(glyph, out var shown) ? shown : CharacterOf(glyph)?.ToString() ?? glyph;
}
