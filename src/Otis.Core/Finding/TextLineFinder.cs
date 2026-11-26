// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Imaging;

namespace Otis.Core.Finding;

public static class TextLineFinder
{
    private const double StopShare = 0.08;
    private const int MaximumLines = 120;

    // Takes the densest band again and again; rows around a found body belong to its ascenders and descenders.
    public static IReadOnlyList<GuideLines> Find(BitGrid ink)
    {
        var profile = RowProfile.Of(ink);
        var remaining = (double[])profile.Clone();
        var globalPeak = profile.Max();
        var lines = new List<GuideLines>();
        while (lines.Count < MaximumLines && globalPeak > 0)
        {
            var peak = Array.IndexOf(remaining, remaining.Max());
            if (remaining[peak] < globalPeak * StopShare)
            {
                break;
            }

            var (top, bottom) = RowProfile.DensestRun(remaining, peak, peak, share: 0.5) ?? Grow(remaining, peak);
            var height = bottom - top;
            if (height >= RowProfile.MinimumBody)
            {
                lines.Add(new GuideLines(top, bottom));
            }

            var clearFrom = Math.Max(0, top - (height / 2));
            var clearTo = Math.Min(remaining.Length, bottom + (height / 2));
            Array.Clear(remaining, clearFrom, clearTo - clearFrom);
        }

        return [.. lines.OrderBy(l => l.XLine)];
    }

    private static (int Top, int Bottom) Grow(double[] profile, int peak)
    {
        var threshold = profile[peak] * 0.5;
        var top = peak;
        while (top > 0 && profile[top - 1] >= threshold)
        {
            top--;
        }

        var bottom = peak;
        while (bottom < profile.Length - 1 && profile[bottom + 1] >= threshold)
        {
            bottom++;
        }

        return (top, bottom + 1);
    }
}
