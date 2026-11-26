// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Finding;
using Otis.Core.Projects;

namespace Otis.Infrastructure.Recognition;

public static class ReadWords
{
    private const double SameWordOverlap = 0.3;

    // Strips overlap, so a word near a seam can be read twice.
    public static IReadOnlyList<ReadWord> WithoutRepeats(IEnumerable<ReadWord> words)
    {
        var kept = new List<ReadWord>();
        foreach (var word in words.Where(w => !kept.Exists(k => Overlap(k.Box, w.Box) >= SameWordOverlap)))
        {
            kept.Add(word);
        }

        return kept;
    }

    private static double Overlap(PixelRect a, PixelRect b)
    {
        var width = Math.Min(a.Right, b.Right) - Math.Max(a.Left, b.Left);
        var height = Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Top, b.Top);
        if (width <= 0 || height <= 0)
        {
            return 0;
        }

        double shared = width * height;
        return shared / ((a.Width * a.Height) + (b.Width * b.Height) - shared);
    }
}
