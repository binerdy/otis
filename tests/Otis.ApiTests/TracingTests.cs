// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text.Json;
using Otis.Core.Imaging;
using Otis.Core.Tracing;

namespace Otis.ApiTests;

public sealed class TracingTests
{
    public static TheoryData<string> ReferenceShapes => ["ring", "star", "blobs", "diagonal"];

    [Theory]
    [MemberData(nameof(ReferenceShapes))]
    public void MatchesTheReferenceTrace(string name)
    {
        var shape = Reference(name);
        var options = new TraceOptions { TurnPolicy = TurnPolicy.Black, TurdSize = 2, AlphaMax = 1.0, OptimizeTolerance = 0.2 };

        var contours = PotraceTracer.Trace(shape.Ink, options);

        Assert.Equal(shape.Contours.Count, contours.Count);
        for (var c = 0; c < contours.Count; c++)
        {
            Assert.Equal(shape.Contours[c].Count, contours[c].Segments.Count);
            for (var s = 0; s < contours[c].Segments.Count; s++)
            {
                var expected = shape.Contours[c][s];
                var actual = contours[c].Segments[s];
                Assert.Equal(expected.IsCorner, actual.IsCorner);
                AssertNear(expected.C1, actual.C1);
                AssertNear(expected.End, actual.End);
                if (!expected.IsCorner)
                {
                    AssertNear(expected.C2, actual.C2);
                }
            }
        }
    }

    [Fact]
    public void SquareHasFourSharpCorners()
    {
        var ink = Grid(30, 30, (x, y) => x is >= 5 and < 25 && y is >= 5 and < 25);

        var contour = Assert.Single(PotraceTracer.Trace(ink));

        Assert.Equal(4, contour.Segments.Count);
        Assert.All(contour.Segments, s => Assert.True(s.IsCorner));
        Assert.Equal([5.0, 25.0], contour.Segments.Select(s => s.C1.X).Distinct().Order());
    }

    [Fact]
    public void CounterRunsTheOtherWay()
    {
        var ink = Grid(60, 60, (x, y) => Distance(x, y, 30, 30) is <= 25 and >= 10);

        var contours = PotraceTracer.Trace(ink);

        Assert.Equal(2, contours.Count);
        var outer = Assert.Single(contours, c => !c.IsHole);
        var hole = Assert.Single(contours, c => c.IsHole);
        Assert.NotEqual(Math.Sign(SignedArea(outer)), Math.Sign(SignedArea(hole)));
    }

    [Fact]
    public void DiskBecomesSmoothCurvesOnItsEdge()
    {
        var ink = Grid(100, 100, (x, y) => Distance(x, y, 50, 50) <= 40);

        var contour = Assert.Single(PotraceTracer.Trace(ink));

        Assert.All(contour.Segments, s => Assert.False(s.IsCorner));
        Assert.All(contour.Segments, s => Assert.InRange(Math.Sqrt(Square(s.End.X - 50) + Square(s.End.Y - 50)), 39, 41));
    }

    [Fact]
    public void InkIsTracedNotPaper()
    {
        var ink = Grid(50, 50, (x, y) => x is >= 10 and < 20 && y is >= 15 and < 40);

        var contour = Assert.Single(PotraceTracer.Trace(ink));

        Assert.False(contour.IsHole);
        Assert.Equal(10, contour.Segments.Min(s => s.End.X), 1);
        Assert.Equal(20, contour.Segments.Max(s => s.End.X), 1);
    }

    [Fact]
    public void SpecksBelowTheTurdSizeAreIgnored()
    {
        var ink = Grid(40, 40, (x, y) => (x is >= 5 and < 25 && y is >= 5 and < 25) || (x == 35 && y == 35));

        Assert.Single(PotraceTracer.Trace(ink, new TraceOptions { TurdSize = 2 }));
    }

    private static void AssertNear(TracePoint expected, TracePoint actual)
    {
        Assert.Equal(expected.X, actual.X, 1e-6);
        Assert.Equal(expected.Y, actual.Y, 1e-6);
    }

    private static double SignedArea(TracedContour contour)
    {
        var area = 0.0;
        var previous = contour.Start;
        foreach (var segment in contour.Segments)
        {
            area += (previous.X * segment.End.Y) - (segment.End.X * previous.Y);
            previous = segment.End;
        }

        return area / 2;
    }

    private static double Distance(int x, int y, int cx, int cy) => Math.Sqrt(Square(x - cx) + Square(y - cy));

    private static double Square(double v) => v * v;

    private static BitGrid Grid(int width, int height, Func<int, int, bool> isInk)
    {
        var grid = new BitGrid(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                grid[x, y] = isInk(x, y);
            }
        }

        return grid;
    }

    private static ReferenceShape Reference(string name)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "potrace-reference.json")));
        var shape = document.RootElement.GetProperty("shapes").EnumerateArray().Single(s => s.GetProperty("name").GetString() == name);
        var rows = shape.GetProperty("rows").EnumerateArray().Select(r => r.GetString()!).ToList();
        var ink = Grid(rows[0].Length, rows.Count, (x, y) => rows[y][x] == '1');
        var contours = shape.GetProperty("contours").EnumerateArray()
            .Select(c => (IReadOnlyList<TracedSegment>)[.. c.EnumerateArray().Select(ToSegment)])
            .ToList();
        return new ReferenceShape(ink, contours);
    }

    private static TracedSegment ToSegment(JsonElement s)
    {
        var corner = s.GetProperty("corner").GetBoolean();
        var c1 = Point(s.GetProperty("c1"));
        var c2 = corner ? c1 : Point(s.GetProperty("c2"));
        return new TracedSegment(corner, c1, c2, Point(s.GetProperty("end")));
    }

    private static TracePoint Point(JsonElement p) => new(p[0].GetDouble(), p[1].GetDouble());

    private sealed record ReferenceShape(BitGrid Ink, IReadOnlyList<IReadOnlyList<TracedSegment>> Contours);
}
