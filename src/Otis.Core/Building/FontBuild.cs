// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Glyphs;
using Otis.Core.Projects;

namespace Otis.Core.Building;

public sealed record FontBuild(ReadOnlyMemory<byte> FontFile, FontVersion Version, Coverage Coverage, IReadOnlyList<string> Problems)
{
    public string FileName(FontSettings font) => $"{font.PostScriptName}-{this.Version}.ttf";
}
