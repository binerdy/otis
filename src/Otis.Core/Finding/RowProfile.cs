// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Imaging;

namespace Otis.Core.Finding;

public static class RowProfile
{
    public const int MinimumBody = 8;

    public static double[] Of(BitGrid ink, int smoothing = 2)
    {
        var raw = new double[ink.Height];
        for (var y = 0; y < ink.Height; y++)
        {
            for (var x = 0; x < ink.Width; x++)
            {
                if (ink[x, y])
                {
                    raw[y]++;
                }
            }
        }

        var smooth = new double[raw.Length];
        for (var y = 0; y < raw.Length; y++)
        {
            var from = Math.Max(0, y - smoothing);
            var to = Math.Min(raw.Length - 1, y + smoothing);
            var sum = 0.0;
            for (var k = from; k <= to; k++)
            {
                sum += raw[k];
            }

            smooth[y] = sum / (to - from + 1);
        }

        return smooth;
    }

    // The lowercase body is where a text line is densest; flourishes above and below are thinner.
    public static (int Top, int Bottom)? DensestRun(double[] profile, int from, int to, double share = 0.5)
    {
        from = Math.Max(0, from);
        to = Math.Min(profile.Length - 1, to);
        if (to <= from)
        {
            return null;
        }

        var peak = from;
        for (var y = from; y <= to; y++)
        {
            if (profile[y] > profile[peak])
            {
                peak = y;
            }
        }

        if (profile[peak] <= 0)
        {
            return null;
        }

        var threshold = profile[peak] * share;
        var top = peak;
        while (top > from && profile[top - 1] >= threshold)
        {
            top--;
        }

        var bottom = peak;
        while (bottom < to && profile[bottom + 1] >= threshold)
        {
            bottom++;
        }

        return bottom - top + 1 >= MinimumBody ? (top, bottom + 1) : null;
    }
}
