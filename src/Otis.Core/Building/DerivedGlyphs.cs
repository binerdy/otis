// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Glyphs;
using Otis.Core.Projects;

namespace Otis.Core.Building;

public static class DerivedGlyphs
{
    private const double DotGap = 40;
    private const double DotLift = 40;
    private const double CapitalDotHeightShare = 0.75;

    // Adds what the rules can derive; a glyph that already exists, because it was cut out, always wins.
    public static void AddTo(Dictionary<string, PlacedGlyph> glyphs, DerivationRules rules, FontSettings font, IOverlapRemover overlaps)
    {
        foreach (var (name, source) in rules.Aliases)
        {
            if (!glyphs.ContainsKey(name) && glyphs.TryGetValue(source, out var original))
            {
                glyphs[name] = original with { Name = name };
            }
        }

        if (glyphs.TryGetValue(rules.DotGlyph, out var dot))
        {
            foreach (var (name, baseName) in rules.Umlauts)
            {
                if (!glyphs.ContainsKey(name) && glyphs.TryGetValue(baseName, out var baseGlyph))
                {
                    glyphs[name] = Umlaut(name, baseGlyph, dot, rules.DotScale, font, overlaps);
                }
            }
        }

        foreach (var (name, parts) in rules.Ligatures)
        {
            if (!glyphs.ContainsKey(name) && parts.All(glyphs.ContainsKey))
            {
                glyphs[name] = Ligature(name, [.. parts.Select(p => glyphs[p])], overlaps);
            }
        }
    }

    private static PlacedGlyph Umlaut(string name, PlacedGlyph baseGlyph, PlacedGlyph dot, double dotScale, FontSettings font, IOverlapRemover overlaps)
    {
        var (dx0, dy0, dx1, _) = dot.Outline.Bounds();
        var dotWidth = (dx1 - dx0) * dotScale;
        var centre = (baseGlyph.BodyLeft + baseGlyph.BodyRight) / 2;
        var isCapital = GlyphNames.CharacterOf(name) is { } c && char.IsUpper(c);
        var bottom = isCapital ? font.CapHeight * CapitalDotHeightShare : baseGlyph.Outline.Bounds().YMax + DotLift;

        var outline = baseGlyph.Outline;
        foreach (var side in new[] { -1, 1 })
        {
            var left = centre + (side * ((DotGap / 2) + (dotWidth / 2))) - (dotWidth / 2);
            outline = outline.Combine(dot.Outline.Transform(dotScale, left - (dx0 * dotScale), bottom - (dy0 * dotScale)));
        }

        return baseGlyph with { Name = name, Outline = overlaps.RemoveOverlaps(outline) };
    }

    private static PlacedGlyph Ligature(string name, IReadOnlyList<PlacedGlyph> parts, IOverlapRemover overlaps)
    {
        var outline = parts[0].Outline;
        var x = (double)parts[0].AdvanceWidth;
        foreach (var part in parts.Skip(1))
        {
            outline = outline.Combine(part.Outline.Transform(1, x, 0));
            x += part.AdvanceWidth;
        }

        return new PlacedGlyph(name, overlaps.RemoveOverlaps(outline), (int)Math.Round(x), parts[0].BodyLeft, x - (parts[^1].AdvanceWidth - parts[^1].BodyRight));
    }
}
