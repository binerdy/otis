// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Imaging;
using Otis.Core.Ink;
using Otis.Core.Projects;

namespace Otis.Core.Finding;

public static class SimilarFinder
{
    public const int MaximumMatches = 12;
    public const double MinimumSimilarity = 0.5;

    // Every line is compared at the same x-height, so letters written larger or smaller match too.
    private const double NormalXHeight = 20;
    private const double MinimumInkRatio = 0.6;
    private const double MaximumInkRatio = 1.6;
    private const double OverlapLimit = 0.3;

    public static Template TemplateOf(RgbImage page, Cutout reference)
    {
        var ink = CutoutExtractor.Extract(page, reference);
        var box = reference.Bounds;
        var scale = NormalXHeight / Math.Max(1, reference.Baseline - reference.XLine);
        var width = Math.Max(1, (int)Math.Round(box.Width * scale));
        var height = Math.Max(1, (int)Math.Round(box.Height * scale));
        var values = Sample(width, height, (u, v) => ink.Mask[(int)(box.Left + (u / scale)) - ink.Left, (int)(box.Top + (v / scale)) - ink.Top]);
        return new Template(values, width, height, (box.Top - reference.XLine) * scale, reference);
    }

    public static IReadOnlyList<SimilarMatch> SearchPage(Template template, string source, int pageNumber, PageInkMap map)
    {
        var matches = new List<SimilarMatch>();
        foreach (var reduced in map.Lines)
        {
            var line = new GuideLines(reduced.XLine * map.Factor, reduced.Baseline * map.Factor);
            matches.AddRange(SearchLine(template, source, pageNumber, map, line));
        }

        return matches;
    }

    // Keeps the best of overlapping matches and drops the example itself.
    public static IReadOnlyList<SimilarMatch> Best(IEnumerable<SimilarMatch> matches, Template template)
    {
        var kept = new List<SimilarMatch>();
        foreach (var match in matches.OrderByDescending(m => m.Similarity))
        {
            var overlapsExample = match.Source == template.Reference.Source && match.Page == template.Reference.Page
                && Overlap(match.Box, template.Reference.Bounds) > OverlapLimit;
            if (!overlapsExample && kept.All(k => k.Source != match.Source || k.Page != match.Page || Overlap(k.Box, match.Box) <= OverlapLimit))
            {
                kept.Add(match);
            }

            if (kept.Count == MaximumMatches)
            {
                break;
            }
        }

        return kept;
    }

    private static List<SimilarMatch> SearchLine(Template template, string source, int pageNumber, PageInkMap map, GuideLines line)
    {
        var scale = NormalXHeight / line.XHeight;
        var top = line.XLine + (template.TopOffset / scale);
        var bandWidth = (int)(map.Ink.Width * map.Factor * scale);
        if (bandWidth <= template.Width)
        {
            return [];
        }

        var band = Sample(bandWidth, template.Height + 2, (u, v) => map[u / scale, top + ((v - 1) / scale)]);
        var columns = ColumnSums(band, bandWidth, template.Height + 2);
        var results = new List<SimilarMatch>();
        for (var x = 0; x + template.Width <= bandWidth; x++)
        {
            var best = (Similarity: -1.0, Dy: 0);
            for (var dy = 0; dy <= 2; dy++)
            {
                var amount = InkIn(columns, x, template.Width, dy, template.Height, template.Height + 2);
                var ratio = amount / Math.Max(1e-6, template.InkAmount);
                if (ratio is < MinimumInkRatio or > MaximumInkRatio)
                {
                    continue;
                }

                var similarity = template.Correlate(band, bandWidth, x, dy);
                if (similarity > best.Similarity)
                {
                    best = (similarity, dy);
                }
            }

            if (best.Similarity >= MinimumSimilarity)
            {
                var left = (int)Math.Round(x / scale);
                var boxTop = (int)Math.Round(top + ((best.Dy - 1) / scale));
                var box = new PixelRect(left, boxTop, left + (int)Math.Round(template.Width / scale), boxTop + (int)Math.Round(template.Height / scale));
                results.Add(new SimilarMatch(source, pageNumber, box, line, best.Similarity));
            }
        }

        return results;
    }

    private static float[] Sample(int width, int height, Func<double, double, bool> isInk)
    {
        var values = new float[width * height];
        for (var v = 0; v < height; v++)
        {
            for (var u = 0; u < width; u++)
            {
                var hits = 0;
                for (var sy = 0; sy < 2; sy++)
                {
                    for (var sx = 0; sx < 2; sx++)
                    {
                        hits += isInk(u + 0.25 + (sx * 0.5), v + 0.25 + (sy * 0.5)) ? 1 : 0;
                    }
                }

                values[(v * width) + u] = hits / 4f;
            }
        }

        return GaussianBlur.Apply(values, width, height, 0.8);
    }

    private static float[][] ColumnSums(float[] band, int width, int height)
    {
        var sums = new float[height + 1][];
        sums[0] = new float[width];
        for (var y = 0; y < height; y++)
        {
            sums[y + 1] = new float[width];
            for (var x = 0; x < width; x++)
            {
                sums[y + 1][x] = sums[y][x] + band[(y * width) + x];
            }
        }

        return sums;
    }

    private static double InkIn(float[][] columns, int x, int width, int dy, int height, int bandHeight)
    {
        var bottom = Math.Min(bandHeight, dy + height);
        var sum = 0.0;
        for (var u = x; u < x + width; u++)
        {
            sum += columns[bottom][u] - columns[dy][u];
        }

        return sum;
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
        return shared / Math.Min(a.Width * a.Height, b.Width * b.Height);
    }
}
