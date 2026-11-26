// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Outlines;

namespace Otis.Core.Building;

public sealed record PlacedGlyph(string Name, GlyphOutline Outline, int AdvanceWidth, double BodyLeft, double BodyRight);
