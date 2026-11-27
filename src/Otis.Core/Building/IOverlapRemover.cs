// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Outlines;

namespace Otis.Core.Building;

public interface IOverlapRemover
{
    GlyphOutline RemoveOverlaps(GlyphOutline outline);
}
