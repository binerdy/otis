// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Projects;

namespace Otis.Core.Finding;

public sealed record SimilarMatch(string Source, int Page, PixelRect Box, GuideLines Lines, double Similarity)
{
    public Cutout ToCutout() => Cutout.Rectangle(this.Source, this.Page, this.Box, this.Lines.Baseline, this.Lines.XLine);
}
