// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Imaging;
using Otis.Core.Ink;
using Otis.Core.Projects;

namespace Otis.Core.Finding;

public static class GuideLineFinder
{
    private const int SideWidths = 2;
    private const double ThinShare = 0.25;
    private const double WideShare = 0.35;

    // Looks at the line the box sits on, two box widths to each side, so a capital takes its neighbours' lines.
    public static GuideLines? Around(RgbImage page, PixelRect box)
    {
        var area = page.Clip(new PixelRect(
            box.Left - (SideWidths * box.Width),
            box.Top - (box.Height / 2),
            box.Right + (SideWidths * box.Width),
            box.Bottom + (box.Height / 2)));
        if (area.Width <= 0 || area.Height <= 0)
        {
            return null;
        }

        var profile = RowProfile.Of(InkDetector.Detect(page.Crop(area)));
        var from = box.Top - area.Top;
        var to = box.Bottom - area.Top;
        var run = RowProfile.DensestRun(profile, from, to);

        // Serif feet can be denser than the letter bodies; a run that thin is the feet, so look again more widely.
        if (run is not { } dense || dense.Bottom - dense.Top < box.Height * ThinShare)
        {
            run = RowProfile.DensestRun(profile, from, to, WideShare) ?? run;
        }

        return run is { } r ? new GuideLines(area.Top + r.Top, area.Top + r.Bottom) : null;
    }
}
