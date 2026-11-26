// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Imaging;
using Otis.Core.Ink;
using Otis.Core.Projects;

namespace Otis.Core.Finding;

public static class QualityScore
{
    private const int EdgeBand = 3;

    public static Quality Of(RgbImage page, Cutout cutout)
    {
        var ink = CutoutExtractor.Extract(page, cutout);
        var mask = ink.Mask;
        var inkCount = mask.Count;
        if (inkCount == 0)
        {
            return new Quality(0, 0, 0, 0);
        }

        return new Quality(Isolation(page, cutout), Smoothness(mask), Completeness(mask), Contrast(page, ink));
    }

    // Ink of the raw scan along the box edge means the box cuts through a neighbour or the letter itself.
    private static double Isolation(RgbImage page, Cutout cutout)
    {
        var box = page.Clip(cutout.Bounds);
        if (box.Width <= 2 * EdgeBand || box.Height <= 2 * EdgeBand)
        {
            return 0;
        }

        var raw = InkDetector.Detect(page.Crop(box));
        var inside = PolygonMask.Fill(cutout.Outline, box.Left, box.Top, box.Width, box.Height);
        var interior = Morphology.Erode(inside, EdgeBand);
        int edge = 0, edgeInk = 0;
        for (var y = 0; y < box.Height; y++)
        {
            for (var x = 0; x < box.Width; x++)
            {
                if (inside[x, y] && !interior[x, y])
                {
                    edge++;
                    if (raw[x, y])
                    {
                        edgeInk++;
                    }
                }
            }
        }

        return edge == 0 ? 0 : Math.Clamp(1 - (3.0 * edgeInk / edge), 0, 1);
    }

    private static double Smoothness(BitGrid mask)
    {
        var calm = Morphology.Close(Morphology.Open(mask));
        int changed = 0, boundary = 0;
        for (var y = 0; y < mask.Height; y++)
        {
            for (var x = 0; x < mask.Width; x++)
            {
                if (mask[x, y] != calm[x, y])
                {
                    changed++;
                }

                if (mask[x, y] && !(mask[x - 1, y] && mask[x + 1, y] && mask[x, y - 1] && mask[x, y + 1]))
                {
                    boundary++;
                }
            }
        }

        return boundary == 0 ? 0 : Math.Clamp(1 - (2.0 * changed / boundary), 0, 1);
    }

    private static double Completeness(BitGrid mask)
    {
        ConnectedParts.Label(mask, out var parts);
        return parts <= 1 ? 1 : 1 / (1 + (0.35 * (parts - 1)));
    }

    // Dark, even ink scores high; faded or blotchy ink low.
    private static double Contrast(RgbImage page, CutoutInk ink)
    {
        var values = new List<double>();
        for (var y = 0; y < ink.Mask.Height; y++)
        {
            for (var x = 0; x < ink.Mask.Width; x++)
            {
                if (ink.Mask[x, y])
                {
                    var (r, g, b) = page[ink.Left + x, ink.Top + y];
                    values.Add((r + g + b) / 3.0);
                }
            }
        }

        var mean = values.Average();
        var spread = Math.Sqrt(values.Average(v => (v - mean) * (v - mean)));
        var darkness = Math.Clamp((200 - mean) / 160, 0, 1);
        var evenness = Math.Clamp(1 - (spread / 80), 0, 1);
        return (0.7 * darkness) + (0.3 * evenness);
    }
}
