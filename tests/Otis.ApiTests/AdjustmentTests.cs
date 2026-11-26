// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.ApiTests.Support;
using Otis.Core.Building;
using Otis.Core.Imaging;
using Otis.Core.Ink;
using Otis.Core.Projects;
using Otis.Core.Tracing;
using SkiaSharp;

namespace Otis.ApiTests;

public sealed class AdjustmentTests
{
    [Fact]
    public void CloseGapsBridgesABrokenHairline()
    {
        using var page = new TestPage();
        page.Rect(100, 200, 160, 206).Rect(164, 200, 220, 206);
        var cutout = Cutout.Rectangle("page.png", 1, new PixelRect(90, 150, 230, 250), 240, 200);

        Assert.Equal(2, Parts(CutoutExtractor.Extract(page.ToImage(), cutout.WithAdjustments(CutoutAdjustments.None with { CloseGaps = 0 }))));
        Assert.Equal(1, Parts(CutoutExtractor.Extract(page.ToImage(), cutout.WithAdjustments(CutoutAdjustments.None with { CloseGaps = 3 }))));
    }

    [Fact]
    public void InkStrokeRepairsABreak()
    {
        using var page = new TestPage();
        page.Rect(100, 200, 150, 206).Rect(180, 200, 230, 206);
        var cutout = Cutout.Rectangle("page.png", 1, new PixelRect(90, 150, 240, 250), 240, 200);
        var stroke = new InkStroke(6, [new(148, 203), new(182, 203)]);

        var repaired = CutoutExtractor.Extract(page.ToImage(), cutout.WithAdjustments(CutoutAdjustments.None with { InkStrokes = [stroke] }));

        Assert.Equal(1, Parts(repaired));
        Assert.NotNull(repaired.Strokes);
    }

    [Fact]
    public void StrokesOutsideTheBoxAreIgnored()
    {
        using var page = new TestPage();
        page.Rect(100, 200, 150, 260);
        var cutout = Cutout.Rectangle("page.png", 1, new PixelRect(90, 150, 160, 270), 260, 200);
        var outside = new InkStroke(10, [new(300, 210), new(340, 210)]);

        var ink = CutoutExtractor.Extract(page.ToImage(), cutout.WithAdjustments(CutoutAdjustments.None with { InkStrokes = [outside] }));

        Assert.Equal(1, Parts(ink));
        Assert.InRange(ink.Mask.Bounds()!.Value.Bottom - ink.Mask.Bounds()!.Value.Top, 60, 62);
    }

    [Fact]
    public void StrokesMoveWithTheBox()
    {
        var cutout = Cutout.Rectangle("page.png", 1, new PixelRect(0, 0, 50, 50), 40, 20)
            .WithAdjustments(CutoutAdjustments.None with { InkStrokes = [new InkStroke(4, [new(10, 10)])] });

        Assert.Equal(new PixelPoint(15, 7), cutout.MoveBy(5, -3).InkStrokes![0].Points[0]);
    }

    [Fact]
    public void LettersWithoutAdjustmentsKeepTheirFileContent()
    {
        var plain = OtisProject.Create("Kanzlei Fraktur").WithCutout("a", Cutout.Rectangle("page.png", 1, new PixelRect(0, 0, 50, 50), 40, 20));
        var touched = plain.WithCutout("a", plain.Cutouts["a"].WithAdjustments(CutoutAdjustments.None));

        Assert.Equal(ProjectJson.Write(plain), ProjectJson.Write(touched));
    }

    [Fact]
    public void AdjustmentsSurviveSaving()
    {
        var adjustments = new CutoutAdjustments(GreyTracing: true, SmoothingLevel.Medium, CloseGaps: 4, [new InkStroke(5, [new(1, 2), new(3, 4)])]);
        var project = OtisProject.Create("Kanzlei Fraktur")
            .WithCutout("a", Cutout.Rectangle("page.png", 1, new PixelRect(0, 0, 50, 50), 40, 20).WithAdjustments(adjustments));

        var json = ProjectJson.Write(project);

        Assert.Equal(adjustments, ProjectJson.Read(json).Project.Cutouts["a"].Adjustments);
        Assert.Contains("\"smoothing\": \"Medium\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void UnevenPaperIsNotTracedFromTheGreyScan()
    {
        using var page = new TestPage(400, 300);
        for (var x = 0; x < 400; x++)
        {
            var level = (byte)(245 - (85 * x / 399));
            page.Rect(x, 0, x + 1, 300, new SKColor(level, level, (byte)(level - 6)));
        }

        page.Rect(180, 100, 200, 220);
        var cutout = Cutout.Rectangle("page.png", 1, new PixelRect(60, 60, 380, 260), 220, 150);

        var plain = CutoutTracer.Trace(page.ToImage(), cutout);
        var grey = CutoutTracer.Trace(page.ToImage(), cutout.WithAdjustments(CutoutAdjustments.None with { GreyTracing = true }));

        Assert.True(Width(plain) > 100);
        Assert.InRange(Width(grey), 18, 26);
    }

    [Fact]
    public void GreyTracingFollowsTheRoundEdgeMoreClosely()
    {
        using var page = new TestPage(200, 200);
        page.Circle(100, 100, 40);
        var cutout = Cutout.Rectangle("page.png", 1, new PixelRect(50, 50, 150, 150), 140, 80);

        var plain = RadiusSpread(CutoutTracer.Trace(page.ToImage(), cutout));
        var grey = RadiusSpread(CutoutTracer.Trace(page.ToImage(), cutout.WithAdjustments(CutoutAdjustments.None with { GreyTracing = true })));

        Assert.True(grey < plain, $"grey {grey:F3} should be below plain {plain:F3}");
    }

    [Fact]
    public void StrongSmoothingCalmsAFrayedEdge()
    {
        using var page = new TestPage(240, 240);
        page.Circle(120, 120, 60);
        for (var i = 0; i < 90; i++)
        {
            var angle = i * Math.PI * 2 / 90;
            var wobble = i * 37;
            var size = i * 11;
            var r = 57 + (wobble % 7);
            page.Circle(120 + (int)(Math.Cos(angle) * r), 120 + (int)(Math.Sin(angle) * r), 1 + (size % 3));
        }

        var cutout = Cutout.Rectangle("page.png", 1, new PixelRect(40, 40, 200, 200), 180, 100);

        var off = CutoutTracer.Trace(page.ToImage(), cutout);
        var strong = CutoutTracer.Trace(page.ToImage(), cutout.WithAdjustments(CutoutAdjustments.None with { Smoothing = SmoothingLevel.Strong }));

        var calm = Assert.Single(strong.Contours);
        Assert.True(calm.Segments.Count < off.Contours.Sum(c => c.Segments.Count));
    }

    private static int Parts(CutoutInk ink)
    {
        ConnectedParts.Label(ink.Mask, out var count);
        return count;
    }

    private static double Width(TracedCutout traced)
    {
        var xs = traced.Contours.Where(c => !c.IsHole).SelectMany(c => c.Segments).Select(s => traced.PagePoint(s.End).X).ToList();
        return xs.Max() - xs.Min();
    }

    // Samples each curve and measures how far its distance from the centre varies.
    private static double RadiusSpread(TracedCutout traced)
    {
        var contour = Assert.Single(traced.Contours);
        var radii = new List<double>();
        var previous = traced.PagePoint(contour.Start);
        foreach (var s in contour.Segments)
        {
            var (c1, c2, end) = (traced.PagePoint(s.C1), traced.PagePoint(s.C2), traced.PagePoint(s.End));
            for (var k = 1; k <= 8; k++)
            {
                var t = k / 8.0;
                var p = s.IsCorner ? Lerp(previous, end, t) : Bezier(previous, c1, c2, end, t);
                radii.Add(Math.Sqrt(((p.X - 100) * (p.X - 100)) + ((p.Y - 100) * (p.Y - 100))));
            }

            previous = end;
        }

        var average = radii.Average();
        return Math.Sqrt(radii.Average(r => (r - average) * (r - average)));
    }

    private static TracePoint Lerp(TracePoint a, TracePoint b, double t) => new(a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t));

    private static TracePoint Bezier(TracePoint p0, TracePoint p1, TracePoint p2, TracePoint p3, double t)
    {
        var u = 1 - t;
        return new TracePoint(
            (u * u * u * p0.X) + (3 * u * u * t * p1.X) + (3 * u * t * t * p2.X) + (t * t * t * p3.X),
            (u * u * u * p0.Y) + (3 * u * u * t * p1.Y) + (3 * u * t * t * p2.Y) + (t * t * t * p3.Y));
    }
}
