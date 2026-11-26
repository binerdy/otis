// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Imaging;
using Otis.Core.Ink;
using Otis.Core.Projects;

namespace Otis.Core.Building;

// After Potrace's mkbitmap: even out the paper, keep the grey edge values, upsample, then threshold.
public static class GreyTracing
{
    private const int PaperRadius = 30;
    private const double PaperSigma = 10;
    private const int Context = 45;
    private const float PaperLevel = 245f;
    private const float DarkThreshold = 185f;
    private const float ColouredThreshold = 225f;
    private const float ColouredRedOverBlue = 45f;
    private const float ScoreLimit = 40f;
    private const int SelectionGrowth = 2;

    public static BitGrid Bitmap(RgbImage page, CutoutInk ink, double blurSigma)
    {
        var inner = new PixelRect(ink.Left, ink.Top, ink.Left + ink.Mask.Width, ink.Top + ink.Mask.Height);
        var outer = page.Clip(new PixelRect(inner.Left - Context, inner.Top - Context, inner.Right + Context, inner.Bottom + Context));
        var score = Score(page.Crop(outer));

        var field = new float[ink.Mask.Width * ink.Mask.Height];
        for (var y = 0; y < ink.Mask.Height; y++)
        {
            for (var x = 0; x < ink.Mask.Width; x++)
            {
                field[(y * ink.Mask.Width) + x] = score[((y + inner.Top - outer.Top) * outer.Width) + x + inner.Left - outer.Left];
            }
        }

        field = GaussianBlur.Apply(field, ink.Mask.Width, ink.Mask.Height, Math.Max(0.5, blurSigma * 0.6));
        var big = TracePreparation.Upsample(field, ink.Mask.Width, ink.Mask.Height);

        // Grey edges only count next to the selected ink, so erasures, neighbours and dropped specks stay out.
        var allowed = Morphology.Dilate(ink.Mask, SelectionGrowth);
        var strokes = ink.Strokes is null ? null : TracePreparation.Smooth(ink.Strokes, blurSigma);
        var scale = TracePreparation.Upsampling;
        var result = new BitGrid(ink.Mask.Width * scale, ink.Mask.Height * scale);
        for (var y = 0; y < result.Height; y++)
        {
            for (var x = 0; x < result.Width; x++)
            {
                var inside = big[(y * result.Width) + x] > 0 && allowed[x / scale, y / scale];
                result[x, y] = inside || (strokes?[x, y] ?? false);
            }
        }

        return result;
    }

    // Positive where the ink rule of InkDetector holds, measured against the local paper rather than pure white.
    private static float[] Score(RgbImage image)
    {
        int w = image.Width, h = image.Height, n = w * h;
        var pixels = image.Pixels;
        float[] mean = new float[n], redOverBlue = new float[n];
        for (var i = 0; i < n; i++)
        {
            float r = pixels[i * 3], g = pixels[(i * 3) + 1], b = pixels[(i * 3) + 2];
            mean[i] = (r + g + b) / 3f;
            redOverBlue[i] = r - b;
        }

        // The brightest value within a radius wider than any pen stroke follows the paper and skips the ink.
        var paper = GaussianBlur.Apply(MaxFilter.Apply(mean, w, h, PaperRadius), w, h, PaperSigma);
        var score = new float[n];
        for (var i = 0; i < n; i++)
        {
            var level = mean[i] - paper[i] + PaperLevel;
            var dark = DarkThreshold - level;
            var coloured = Math.Min(redOverBlue[i] - ColouredRedOverBlue, ColouredThreshold - level);
            score[i] = Math.Clamp(Math.Max(dark, coloured), -ScoreLimit, ScoreLimit);
        }

        return score;
    }
}
