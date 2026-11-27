// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Glyphs;
using Otis.Core.Outlines;
using Otis.Core.Projects;
using Otis.Core.TrueType;

namespace Otis.Core.Building;

public static class FontBuilder
{
    public static FontBuild Build(OtisProject project, FontVersion version, DateTimeOffset builtAt, IPageSource pages, IOverlapRemover overlaps)
    {
        var problems = new List<string>();
        var placed = new Dictionary<string, PlacedGlyph>(StringComparer.Ordinal);
        foreach (var (name, cutout) in project.Cutouts)
        {
            var glyph = TryMake(name, cutout, project.Font, pages, problems);
            if (glyph is not null)
            {
                placed[name] = glyph with { Outline = OutlineOrientation.Normalize(glyph.Outline) };
            }
        }

        DerivedGlyphs.AddTo(placed, project.Derivations, project.Font, overlaps);

        var mapped = placed.Values
            .Select(g => (Glyph: g, Character: GlyphNames.CharacterOf(g.Name)))
            .Where(g => g.Character is not null and not ' ')
            .OrderBy(g => g.Character)
            .ToList();

        var glyphs = new List<TrueTypeGlyph> { NotDefined(project.Font), new("space", [], project.Font.WordSpace) };
        var characterMap = new Dictionary<int, int> { [' '] = 1 };
        foreach (var (glyph, character) in mapped)
        {
            characterMap[character!.Value] = glyphs.Count;
            glyphs.Add(TrueTypeGlyph.From(glyph.Name, glyph.Outline, glyph.AdvanceWidth));
        }

        var description = new FontDescription
        {
            FamilyName = project.Font.FamilyName,
            PostScriptName = project.Font.PostScriptName,
            Version = version,
            Created = builtAt,
            Designer = project.Font.Designer,
            UnitsPerEm = project.Font.UnitsPerEm,
            XHeight = project.Font.XHeight,
            CapHeight = project.Font.CapHeight,
        };

        return new FontBuild(TrueTypeWriter.Write(description, glyphs, characterMap), version, Coverage.Of(project), problems);
    }

    private static PlacedGlyph? TryMake(string name, Cutout cutout, FontSettings font, IPageSource pages, List<string> problems)
    {
        var display = GlyphNames.Display(name);
        try
        {
            var glyph = GlyphMaker.Make(name, pages.Page(cutout.Source, cutout.Page), cutout, font);
            if (glyph.Outline.IsEmpty)
            {
                problems.Add($"{display}: no ink inside the cutout.");
                return null;
            }

            return glyph;
        }
        catch (FileNotFoundException)
        {
            problems.Add($"{display}: source {cutout.Source} is missing.");
            return null;
        }
        catch (ArgumentOutOfRangeException)
        {
            problems.Add($"{display}: page {cutout.Page} does not exist in {cutout.Source}.");
            return null;
        }
    }

    private static TrueTypeGlyph NotDefined(FontSettings font)
    {
        var height = font.CapHeight;
        var width = height * 6 / 10;
        static GlyphContour Box(int left, int bottom, int right, int top, bool clockwise)
        {
            FontPoint[] corners = clockwise
                ? [new(left, bottom), new(left, top), new(right, top), new(right, bottom)]
                : [new(left, bottom), new(right, bottom), new(right, top), new(left, top)];
            return new GlyphContour(corners[0], [.. corners.Skip(1).Append(corners[0]).Select(ContourSegment.Line)]);
        }

        var outline = new GlyphOutline([Box(50, 0, 50 + width, height, clockwise: true), Box(100, 50, width, height - 50, clockwise: false)]);
        return TrueTypeGlyph.From(".notdef", outline, width + 100);
    }
}
