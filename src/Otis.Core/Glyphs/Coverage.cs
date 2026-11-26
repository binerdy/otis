// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Projects;

namespace Otis.Core.Glyphs;

public sealed class Coverage
{
    private readonly Dictionary<string, CoverageState> states;

    private Coverage(Dictionary<string, CoverageState> states) => this.states = states;

    public int CutOut => this.Count(CoverageState.CutOut);

    public int Derived => this.Count(CoverageState.Derived);

    public int Missing => this.Count(CoverageState.Missing);

    public IEnumerable<string> MissingGlyphs => this.states.Where(s => s.Value == CoverageState.Missing).Select(s => s.Key);

    public CoverageState this[string glyph] => this.states.GetValueOrDefault(glyph, CoverageState.Missing);

    public static Coverage Of(OtisProject project)
    {
        var states = new Dictionary<string, CoverageState>(StringComparer.Ordinal);
        foreach (var glyph in CharacterSet.All.Where(g => !CharacterSet.IsBuildingBlock(g)))
        {
            states[glyph] = StateOf(project, glyph, depth: 0);
        }

        return new Coverage(states);
    }

    private static CoverageState StateOf(OtisProject project, string glyph, int depth)
    {
        if (project.Cutouts.ContainsKey(glyph))
        {
            return CoverageState.CutOut;
        }

        var sources = project.Derivations.SourcesOf(glyph);
        return depth < 4 && sources.Count > 0 && sources.All(s => StateOf(project, s, depth + 1) != CoverageState.Missing)
            ? CoverageState.Derived
            : CoverageState.Missing;
    }

    private int Count(CoverageState state) => this.states.Values.Count(s => s == state);
}
