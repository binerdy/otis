// SPDX-License-Identifier: GPL-3.0-or-later
// Ported from Potrace 1.16. Copyright (C) 2001-2019 Peter Selinger.
// Potrace is free software covered by the GNU General Public License, version 2 or later.
namespace Otis.Core.Tracing;

public readonly record struct TracedSegment(bool IsCorner, TracePoint C1, TracePoint C2, TracePoint End);
