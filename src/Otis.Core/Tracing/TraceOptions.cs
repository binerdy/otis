// SPDX-License-Identifier: GPL-3.0-or-later
// Ported from Potrace 1.16. Copyright (C) 2001-2019 Peter Selinger.
// Potrace is free software covered by the GNU General Public License, version 2 or later.
namespace Otis.Core.Tracing;

public sealed record TraceOptions
{
    public int TurdSize { get; init; } = 2;

    public TurnPolicy TurnPolicy { get; init; } = TurnPolicy.Minority;

    public double AlphaMax { get; init; } = 1.0;

    public bool OptimizeCurves { get; init; } = true;

    public double OptimizeTolerance { get; init; } = 0.2;
}
