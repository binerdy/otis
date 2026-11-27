// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Imaging;
using Otis.Core.Ink;
using Otis.Core.Projects;

namespace Otis.Core.Finding;

// Splits a written word into its letters when the text is known: cuts go where there is least ink,
// near where the letter widths say they should be, along seams that may bend with slanted joins.
public static class WordSplitter
{
    private const double SeamShare = 0.35;
    private const double WidthWeight = 2.0;
    private const double InkWeight = 3.0;
    private const int OutlineSamples = 14;
    private const double Unreachable = double.MaxValue;

    public static IReadOnlyList<WordLetter> Split(RgbImage page, string source, int pageNumber, PixelRect box, string text)
    {
        box = page.Clip(box);
        var characters = text.Where(c => !char.IsWhiteSpace(c)).ToArray();
        if (characters.Length == 0 || box.Width < characters.Length || box.Height < 4)
        {
            return [];
        }

        var lines = GuideLineFinder.Around(page, box)
            ?? new GuideLines(box.Top + (int)(box.Height * 0.3), box.Top + (int)(box.Height * 0.8));
        var ink = InkDetector.Detect(page.Crop(box));
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var letters = new List<WordLetter>();
        foreach (var (word, left, right) in SplitWords(ink, words))
        {
            letters.AddRange(SplitWord(ink, box, left, right, word, lines, source, pageNumber));
        }

        return letters;
    }

    // Words are separated at the widest runs of empty columns.
    private static List<(string Word, int Left, int Right)> SplitWords(BitGrid ink, string[] words)
    {
        if (words.Length == 1)
        {
            return [(words[0], 0, ink.Width)];
        }

        var columns = Columns(ink, 0, ink.Height);
        var gaps = new List<(int Start, int Length)>();
        for (var x = 0; x < columns.Length;)
        {
            if (columns[x] == 0)
            {
                var start = x;
                while (x < columns.Length && columns[x] == 0)
                {
                    x++;
                }

                gaps.Add((start, x - start));
            }
            else
            {
                x++;
            }
        }

        var cuts = gaps.Where(g => g.Start > 0 && g.Start + g.Length < columns.Length)
            .OrderByDescending(g => g.Length)
            .Take(words.Length - 1)
            .Select(g => g.Start + (g.Length / 2))
            .ToList();
        if (cuts.Count < words.Length - 1)
        {
            var total = words.Sum(w => w.Length);
            cuts = [.. Enumerable.Range(1, words.Length - 1).Select(i => ink.Width * words.Take(i).Sum(w => w.Length) / total)];
        }

        cuts.Sort();
        var edges = cuts.Prepend(0).Append(ink.Width).ToList();
        return [.. words.Select((w, i) => (w, edges[i], edges[i + 1]))];
    }

    private static IEnumerable<WordLetter> SplitWord(BitGrid ink, PixelRect box, int left, int right, string word, GuideLines lines, string source, int pageNumber)
    {
        var bandTop = Math.Clamp(lines.XLine - box.Top, 0, ink.Height - 1);
        var bandBottom = Math.Clamp(lines.Baseline - box.Top, bandTop + 1, ink.Height);
        var body = Columns(ink, bandTop, bandBottom);
        var (inkLeft, inkRight) = Extent(body, left, right);
        var letterUnit = Math.Max(4.0, lines.XHeight * 0.8);
        var cuts = Segment(body, inkLeft, inkRight, word, lines.XHeight, bandBottom - bandTop);
        var seamRadius = Math.Max(2, (int)(letterUnit * SeamShare));
        var seams = new List<int[]> { Straight(left, ink.Height) };
        seams.AddRange(cuts.Select(c => Seam(ink, c, seamRadius)));
        seams.Add(Straight(right, ink.Height));

        for (var i = 0; i < word.Length; i++)
        {
            var outline = Outline(seams[i], seams[i + 1], box);
            yield return new WordLetter(word[i], new Cutout
            {
                Source = source,
                Page = pageNumber,
                Outline = outline,
                Baseline = lines.Baseline,
                XLine = lines.XLine,
            });
        }
    }

    // One pass over all cuts at once: every letter is scored by how well its width suits its character and
    // every cut by the ink it crosses, so a capital of unexpected width cannot push the small letters aside.
    private static int[] Segment(double[] body, int inkLeft, int inkRight, string word, int xHeight, int bandHeight)
    {
        var count = word.Length;
        if (count == 1)
        {
            return [];
        }

        var unit = Math.Max(4.0, xHeight * 0.8);
        var span = inkRight - inkLeft;
        var minimum = Math.Max(1, (int)(unit * 0.25));
        double LetterCost(char c, int width)
        {
            if (SmallWidth(c) is not { } expected)
            {
                return width < unit * 0.5 ? 50 : 0;
            }

            var deviation = (width - (expected * xHeight)) / unit;
            return WidthWeight * deviation * deviation;
        }

        double CutCost(int x) => InkWeight * body[x] / Math.Max(1, bandHeight);

        // best[k][x]: lowest cost with letter k ending at column inkLeft + x.
        var best = new double[count][];
        var from = new int[count][];
        for (var k = 0; k < count; k++)
        {
            best[k] = Enumerable.Repeat(Unreachable, span + 1).ToArray();
            from[k] = new int[span + 1];
        }

        for (var x = minimum; x <= span; x++)
        {
            best[0][x] = LetterCost(word[0], x) + (x < span ? CutCost(inkLeft + x) : 0);
        }

        for (var k = 1; k < count; k++)
        {
            for (var x = minimum * (k + 1); x <= span; x++)
            {
                if (k < count - 1 && x == span)
                {
                    continue;
                }

                var cut = x < span ? CutCost(inkLeft + x) : 0;
                for (var p = minimum * k; p <= x - minimum; p++)
                {
                    if (best[k - 1][p] >= Unreachable)
                    {
                        continue;
                    }

                    var total = best[k - 1][p] + LetterCost(word[k], x - p) + cut;
                    if (total < best[k][x])
                    {
                        best[k][x] = total;
                        from[k][x] = p;
                    }
                }
            }
        }

        var cuts = new int[count - 1];
        var end = span;
        for (var k = count - 1; k >= 1; k--)
        {
            end = from[k][end];
            cuts[k - 1] = inkLeft + end;
        }

        return cuts;
    }

    private static double? SmallWidth(char c) => c switch
    {
        'i' or 'l' or 'j' or 't' or 'f' or 'r' or 'ſ' => 0.45,
        '.' or ',' or '\'' or ':' or ';' or '!' => 0.3,
        'm' or 'w' => 1.25,
        _ when char.IsDigit(c) => 0.7,
        _ when char.IsUpper(c) => null,
        _ => 0.8,
    };

    // A top-to-bottom path through least ink, moving at most one column per row and staying near the cut.
    private static int[] Seam(BitGrid ink, int cut, int radius)
    {
        int width = (2 * radius) + 1, height = ink.Height;
        var cost = Enumerable.Range(0, height).Select(_ => new double[width]).ToArray();
        var step = Enumerable.Range(0, height).Select(_ => new int[width]).ToArray();
        for (var y = 0; y < height; y++)
        {
            for (var i = 0; i < width; i++)
            {
                var x = cut - radius + i;
                var here = (ink[x, y] ? 1.0 : 0.0) + (0.02 * Math.Abs(i - radius));
                if (y == 0)
                {
                    cost[y][i] = here;
                    continue;
                }

                var bestPrevious = double.MaxValue;
                for (var d = -1; d <= 1; d++)
                {
                    var p = i + d;
                    if (p >= 0 && p < width && cost[y - 1][p] < bestPrevious)
                    {
                        bestPrevious = cost[y - 1][p];
                        step[y][i] = p;
                    }
                }

                cost[y][i] = bestPrevious + here;
            }
        }

        var path = new int[height];
        var column = 0;
        for (var i = 1; i < width; i++)
        {
            if (cost[height - 1][i] < cost[height - 1][column])
            {
                column = i;
            }
        }

        for (var y = height - 1; y >= 0; y--)
        {
            path[y] = cut - radius + column;
            column = step[y][column];
        }

        return path;
    }

    private static int[] Straight(int x, int height) => [.. Enumerable.Repeat(x, height)];

    private static PixelPoint[] Outline(int[] leftSeam, int[] rightSeam, PixelRect box)
    {
        var height = leftSeam.Length;
        var rows = Enumerable.Range(0, OutlineSamples + 1).Select(i => Math.Min(height - 1, i * (height - 1) / OutlineSamples)).Distinct().ToList();
        var left = rows.Select(y => new PixelPoint(box.Left + leftSeam[y], box.Top + y));
        var right = rows.AsEnumerable().Reverse().Select(y => new PixelPoint(box.Left + rightSeam[y] + 1, box.Top + y));
        var points = left.Concat(right).ToList();
        points[0] = points[0] with { Y = box.Top };
        points[rows.Count - 1] = points[rows.Count - 1] with { Y = box.Bottom };
        points[rows.Count] = points[rows.Count] with { Y = box.Bottom };
        points[^1] = points[^1] with { Y = box.Top };
        return [.. points];
    }

    private static double[] Columns(BitGrid ink, int top, int bottom)
    {
        var columns = new double[ink.Width];
        for (var x = 0; x < ink.Width; x++)
        {
            for (var y = top; y < bottom; y++)
            {
                if (ink[x, y])
                {
                    columns[x]++;
                }
            }
        }

        return columns;
    }

    private static (int Left, int Right) Extent(double[] columns, int left, int right)
    {
        var first = left;
        while (first < right - 1 && columns[first] == 0)
        {
            first++;
        }

        var last = right - 1;
        while (last > first && columns[last] == 0)
        {
            last--;
        }

        return (first, last + 1);
    }
}
