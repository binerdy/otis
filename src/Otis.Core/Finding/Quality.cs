// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;

namespace Otis.Core.Finding;

public sealed record Quality(double Isolation, double Smoothness, double Completeness, double Contrast)
{
    public int Score => (int)Math.Round(100 * ((0.35 * this.Isolation) + (0.25 * this.Smoothness) + (0.2 * this.Completeness) + (0.2 * this.Contrast)));

    public string Describe(string isolation, string smoothness, string completeness, string contrast) => string.Create(
        CultureInfo.CurrentCulture,
        $"{isolation} {Percent(this.Isolation)} · {smoothness} {Percent(this.Smoothness)} · {completeness} {Percent(this.Completeness)} · {contrast} {Percent(this.Contrast)}");

    private static int Percent(double value) => (int)Math.Round(value * 100);
}
