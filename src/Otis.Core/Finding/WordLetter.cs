// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Projects;

namespace Otis.Core.Finding;

public sealed record WordLetter(char Character, Cutout Cutout)
{
    public string? Glyph => InitialPick.GlyphOf(this.Character.ToString());
}
