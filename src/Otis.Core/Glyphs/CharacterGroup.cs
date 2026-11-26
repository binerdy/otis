// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.Core.Glyphs;

public sealed record CharacterGroup(CharacterGroupKind Kind, IReadOnlyList<string> Glyphs);
