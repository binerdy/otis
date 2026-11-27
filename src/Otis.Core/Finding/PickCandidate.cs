// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Projects;

namespace Otis.Core.Finding;

public sealed record PickCandidate(string Glyph, Cutout Cutout, double Confidence, Quality Quality)
{
    public double Score => (0.5 * this.Confidence / 100) + (0.5 * this.Quality.Score / 100);
}
