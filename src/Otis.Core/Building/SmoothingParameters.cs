// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Projects;

namespace Otis.Core.Building;

public sealed record SmoothingParameters(double BlurSigma, double AlphaMax, double OptimizeTolerance)
{
    public static SmoothingParameters For(SmoothingLevel level) => level switch
    {
        SmoothingLevel.Light => new(1.8, 1.1, 0.4),
        SmoothingLevel.Medium => new(2.4, 1.2, 0.6),
        SmoothingLevel.Strong => new(3.2, 1.3, 0.9),
        _ => new(1.3, 1.0, 0.25),
    };
}
