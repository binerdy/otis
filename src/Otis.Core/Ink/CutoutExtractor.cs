// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Imaging;
using Otis.Core.Projects;

namespace Otis.Core.Ink;

public static class CutoutExtractor
{
    public const int DefaultClosing = 2;
    public const double DefaultMinimumPartShare = 0.06;
    private const int Margin = 4;

    public static CutoutInk Extract(RgbImage page, Cutout cutout)
    {
        var bounds = cutout.Bounds;
        var area = page.Clip(new PixelRect(bounds.Left - Margin, bounds.Top - Margin, bounds.Right + Margin, bounds.Bottom + Margin));
        var ink = InkDetector.Detect(page.Crop(area));

        var closing = Math.Clamp(cutout.Closing ?? DefaultClosing, 0, CutoutAdjustments.MaxCloseGaps);
        var mask = Morphology.Close(ink, closing).Or(ink)
            .And(PolygonMask.Fill(cutout.Outline, area.Left, area.Top, area.Width, area.Height));
        foreach (var erasure in cutout.Erasures)
        {
            Erase(mask, erasure.Offset(-area.Left, -area.Top));
        }

        var polygon = PolygonMask.Fill(cutout.Outline, area.Left, area.Top, area.Width, area.Height);
        var strokes = cutout.InkStrokes is { Count: > 0 } painted
            ? StrokePainter.Paint(painted, area.Left, area.Top, area.Width, area.Height).And(polygon)
            : null;
        if (strokes is not null)
        {
            mask = mask.Or(strokes);
        }

        mask = ConnectedParts.KeepLarge(mask, cutout.MinimumPartShare ?? DefaultMinimumPartShare);
        return new CutoutInk(mask, area.Left, area.Top) { Strokes = strokes };
    }

    private static void Erase(BitGrid mask, PixelRect rect)
    {
        for (var y = Math.Max(0, rect.Top); y < Math.Min(mask.Height, rect.Bottom); y++)
        {
            for (var x = Math.Max(0, rect.Left); x < Math.Min(mask.Width, rect.Right); x++)
            {
                mask[x, y] = false;
            }
        }
    }
}
