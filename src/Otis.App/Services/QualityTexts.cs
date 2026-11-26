// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Finding;

namespace Otis.App.Services;

public static class QualityTexts
{
    public static string Describe(Quality quality) =>
        Strings.Format("QualityFormat", quality.Score) + Environment.NewLine
        + quality.Describe(Strings.Get("Isolation"), Strings.Get("Smoothness"), Strings.Get("Completeness"), Strings.Get("Contrast"));
}
