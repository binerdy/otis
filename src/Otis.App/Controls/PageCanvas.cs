// SPDX-License-Identifier: GPL-3.0-or-later
using Microsoft.UI.Input;
using Otis.Core.Projects;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using Windows.Foundation;

namespace Otis.App.Controls;

public sealed partial class PageCanvas : Grid
{
    private const float HandleRadius = 6;
    private const float GrabDistance = 9;
    private const float LineOverhang = 160;

    private readonly SKXamlCanvas canvas = new();
    private SKImage? page;
    private Cutout? current;
    private IReadOnlyList<NeighbourCutout> neighbours = [];
    private float scale = 0.2f;
    private float offsetX;
    private float offsetY;
    private bool userMovedView;
    private Drag? drag;

    public PageCanvas()
    {
        this.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 154, 148, 139));
        this.canvas.PaintSurface += this.OnPaintSurface;
        this.Children.Add(this.canvas);
        this.PointerPressed += this.OnPointerPressed;
        this.PointerMoved += this.OnPointerMoved;
        this.PointerReleased += this.OnPointerReleased;
        this.PointerWheelChanged += this.OnPointerWheelChanged;
        this.DoubleTapped += this.OnDoubleTapped;
        this.SizeChanged += (_, _) =>
        {
            if (!this.userMovedView)
            {
                this.FitCurrent();
            }
        };
    }

    public event EventHandler<CanvasEventArgs<Cutout>>? CutoutChanged;

    public event EventHandler<CanvasEventArgs<string>>? NeighbourSelected;

    public event EventHandler<CanvasEventArgs<PixelRect>>? BoxDrawn;

    public CanvasMode Mode { get; set; }

    public void ShowPage(SKImage? image)
    {
        this.page = image;
        this.userMovedView = false;
        this.FitCurrent();
    }

    public void ShowCutouts(Cutout? selected, IReadOnlyList<NeighbourCutout> others)
    {
        this.current = selected;
        this.neighbours = others;
        this.canvas.Invalidate();
    }

    public void FitPage()
    {
        if (this.page is null || this.ActualWidth <= 0)
        {
            return;
        }

        this.userMovedView = false;
        this.FitRectangle(0, 0, this.page.Width, this.page.Height, margin: 0.98f);
    }

    public void ZoomToCutout()
    {
        if (this.current is null)
        {
            this.FitPage();
            return;
        }

        this.userMovedView = false;
        var b = this.current.Bounds;
        this.FitRectangle(b.Left - 130, b.Top - 130, b.Right + 130, b.Bottom + 130, margin: 1);
    }

    private static float Distance(SKPoint a, SKPoint b) => SKPoint.Distance(a, b);

    private static SKPoint ToSk(PixelPoint p) => new(p.X, p.Y);

    private static float SegmentDistance(SKPoint p, SKPoint a, SKPoint b)
    {
        var d = b - a;
        var length = (d.X * d.X) + (d.Y * d.Y);
        var t = length == 0 ? 0 : Math.Clamp((((p.X - a.X) * d.X) + ((p.Y - a.Y) * d.Y)) / length, 0, 1);
        return Distance(p, new SKPoint(a.X + (t * d.X), a.Y + (t * d.Y)));
    }

    private static bool Inside(IReadOnlyList<PixelPoint> polygon, SKPoint p)
    {
        var inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var (a, b) = (polygon[i], polygon[j]);
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < ((b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y)) + a.X)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static SKPath Polygon(IReadOnlyList<PixelPoint> points)
    {
        using var builder = new SKPathBuilder();
        builder.MoveTo(ToSk(points[0]));
        foreach (var p in points.Skip(1))
        {
            builder.LineTo(ToSk(p));
        }

        builder.Close();
        return builder.Detach();
    }

    private static void DrawCurrent(SKCanvas surface, Cutout cutout, float unit)
    {
        var b = cutout.Bounds;
        foreach (var (row, color) in new[] { (cutout.Baseline, new SKColor(0, 160, 0)), (cutout.XLine, new SKColor(42, 90, 255)) })
        {
            using var line = new SKPaint { Color = color, StrokeWidth = 2 * unit, IsAntialias = true };
            surface.DrawLine(b.Left - LineOverhang, row, b.Right + LineOverhang, row, line);
            surface.DrawRect(b.Right + LineOverhang - (10 * unit), row - (5 * unit), 10 * unit, 10 * unit, line);
        }

        using var eraseFill = new SKPaint { Color = new SKColor(255, 0, 0, 50) };
        using var eraseStroke = new SKPaint { Color = new SKColor(210, 0, 0), Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f * unit };
        foreach (var e in cutout.Erasures)
        {
            var rect = new SKRect(e.Left, e.Top, e.Right, e.Bottom);
            surface.DrawRect(rect, eraseFill);
            surface.DrawRect(rect, eraseStroke);
        }

        using var path = Polygon(cutout.Outline);
        using var fill = new SKPaint { Color = new SKColor(255, 165, 0, 34) };
        using var stroke = new SKPaint { Color = new SKColor(224, 123, 0), Style = SKPaintStyle.Stroke, StrokeWidth = 2.5f * unit, IsAntialias = true };
        surface.DrawPath(path, fill);
        surface.DrawPath(path, stroke);

        using var handleFill = new SKPaint { Color = SKColors.White, IsAntialias = true };
        foreach (var p in cutout.Outline)
        {
            surface.DrawCircle(p.X, p.Y, HandleRadius * unit, handleFill);
            surface.DrawCircle(p.X, p.Y, HandleRadius * unit, stroke);
        }
    }

    private static InputSystemCursorShape CursorFor(Drag? hit) => hit?.Kind switch
    {
        DragKind.Baseline or DragKind.XLine => InputSystemCursorShape.SizeNorthSouth,
        DragKind.Corner or DragKind.Whole or DragKind.Erasure => InputSystemCursorShape.SizeAll,
        _ => InputSystemCursorShape.Hand,
    };

    private void FitCurrent()
    {
        if (this.current is not null)
        {
            this.ZoomToCutout();
        }
        else
        {
            this.FitPage();
        }
    }

    private void FitRectangle(float left, float top, float right, float bottom, float margin)
    {
        if (this.ActualWidth <= 0 || this.ActualHeight <= 0)
        {
            return;
        }

        var width = (float)this.ActualWidth;
        var height = (float)this.ActualHeight;
        this.scale = Math.Min(width / (right - left), height / (bottom - top)) * margin;
        this.offsetX = (width / 2) - ((left + right) / 2 * this.scale);
        this.offsetY = (height / 2) - ((top + bottom) / 2 * this.scale);
        this.canvas.Invalidate();
    }

    private SKPoint ToPage(Point screen) => new((float)((screen.X - this.offsetX) / this.scale), (float)((screen.Y - this.offsetY) / this.scale));

    // The surface has device pixels; drawing happens in view units so that pointer positions need no DPI factor.
    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var surface = e.Surface.Canvas;
        surface.Clear(new SKColor(154, 148, 139));
        if (this.page is null || this.ActualWidth <= 0)
        {
            return;
        }

        surface.Scale((float)(e.Info.Width / this.ActualWidth));
        surface.Translate(this.offsetX, this.offsetY);
        surface.Scale(this.scale);
        surface.DrawImage(this.page, 0, 0, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));

        var unit = 1 / this.scale;
        this.DrawNeighbours(surface, unit);
        if (this.current is not null)
        {
            DrawCurrent(surface, this.current, unit);
        }

        if (this.drag is { Kind: DragKind.NewBox or DragKind.NewErasure } d)
        {
            using var dashed = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2 * unit,
                Color = d.Kind == DragKind.NewErasure ? SKColors.Red : new SKColor(224, 123, 0),
                PathEffect = SKPathEffect.CreateDash([8 * unit, 4 * unit], 0),
                IsAntialias = true,
            };
            surface.DrawRect(SKRect.Create(d.Start, new SKSize(d.Last.X - d.Start.X, d.Last.Y - d.Start.Y)).Standardized, dashed);
        }
    }

    private void DrawNeighbours(SKCanvas surface, float unit)
    {
        using var outline = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f * unit,
            Color = new SKColor(0, 0, 0, 110),
            PathEffect = SKPathEffect.CreateDash([6 * unit, 4 * unit], 0),
            IsAntialias = true,
        };
        using var label = new SKPaint { Color = new SKColor(0, 0, 0, 170), IsAntialias = true };
        using var font = new SKFont(SKTypeface.Default, 13 * unit);
        foreach (var neighbour in this.neighbours)
        {
            using var path = Polygon(neighbour.Cutout.Outline);
            surface.DrawPath(path, outline);
            var b = neighbour.Cutout.Bounds;
            surface.DrawText(neighbour.Display, b.Left + (3 * unit), b.Top + (14 * unit), SKTextAlign.Left, font, label);
        }
    }

    private Drag? HitTest(SKPoint p)
    {
        var tolerance = GrabDistance / this.scale;
        if (this.current is { } c)
        {
            for (var i = 0; i < c.Outline.Count; i++)
            {
                if (Distance(ToSk(c.Outline[i]), p) < tolerance)
                {
                    return new Drag(DragKind.Corner, p, c, i);
                }
            }

            var b = c.Bounds;
            var alongLines = p.X > b.Left - LineOverhang && p.X < b.Right + LineOverhang;
            if (alongLines && Math.Abs(p.Y - c.Baseline) < tolerance * 0.8f)
            {
                return new Drag(DragKind.Baseline, p, c);
            }

            if (alongLines && Math.Abs(p.Y - c.XLine) < tolerance * 0.8f)
            {
                return new Drag(DragKind.XLine, p, c);
            }

            for (var i = 0; i < c.Erasures.Count; i++)
            {
                var e = c.Erasures[i];
                if (p.X >= e.Left && p.X <= e.Right && p.Y >= e.Top && p.Y <= e.Bottom)
                {
                    return new Drag(DragKind.Erasure, p, c, i);
                }
            }

            if (Inside(c.Outline, p))
            {
                return new Drag(DragKind.Whole, p, c);
            }
        }

        return null;
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        this.Focus(FocusState.Pointer);
        var point = e.GetCurrentPoint(this);
        var p = this.ToPage(point.Position);
        if (point.Properties.IsRightButtonPressed)
        {
            this.RemoveAt(p);
            return;
        }

        if (this.Mode is CanvasMode.DrawBox or CanvasMode.DrawErasure)
        {
            this.drag = new Drag(this.Mode == CanvasMode.DrawBox ? DragKind.NewBox : DragKind.NewErasure, p, this.current);
        }
        else if (this.HitTest(p) is { } hit)
        {
            this.drag = hit;
        }
        else if (this.neighbours.FirstOrDefault(n => Inside(n.Cutout.Outline, p)) is { } neighbour)
        {
            this.NeighbourSelected?.Invoke(this, new(neighbour.Glyph));
            return;
        }
        else
        {
            this.drag = new Drag(DragKind.Pan, new SKPoint((float)point.Position.X, (float)point.Position.Y), null) { ViewX = this.offsetX, ViewY = this.offsetY };
        }

        this.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var position = e.GetCurrentPoint(this).Position;
        if (this.drag is not { } d)
        {
            this.ProtectedCursor = InputSystemCursor.Create(this.Mode == CanvasMode.Edit ? CursorFor(this.HitTest(this.ToPage(position))) : InputSystemCursorShape.Cross);
            return;
        }

        if (d.Kind == DragKind.Pan)
        {
            this.offsetX = d.ViewX + (float)position.X - d.Start.X;
            this.offsetY = d.ViewY + (float)position.Y - d.Start.Y;
            this.userMovedView = true;
            this.canvas.Invalidate();
            return;
        }

        var p = this.ToPage(position);
        d.Last = p;
        if (d.Original is { } original && d.Kind is not (DragKind.NewBox or DragKind.NewErasure))
        {
            var dx = (int)Math.Round(p.X - d.Start.X);
            var dy = (int)Math.Round(p.Y - d.Start.Y);
            this.current = d.Kind switch
            {
                DragKind.Corner => original with { Outline = original.Outline.Select((q, i) => i == d.Index ? new PixelPoint(q.X + dx, q.Y + dy) : q).ToArray() },
                DragKind.Baseline => original with { Baseline = original.Baseline + dy },
                DragKind.XLine => original with { XLine = original.XLine + dy },
                DragKind.Erasure => original with { Erasures = original.Erasures.Select((r, i) => i == d.Index ? r.Offset(dx, dy) : r).ToArray() },
                _ => original.MoveBy(dx, dy),
            };
            this.CutoutChanged?.Invoke(this, new(this.current));
        }

        this.canvas.Invalidate();
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (this.drag is { Kind: DragKind.NewBox or DragKind.NewErasure } d)
        {
            var box = new PixelRect(
                (int)Math.Round(Math.Min(d.Start.X, d.Last.X)),
                (int)Math.Round(Math.Min(d.Start.Y, d.Last.Y)),
                (int)Math.Round(Math.Max(d.Start.X, d.Last.X)),
                (int)Math.Round(Math.Max(d.Start.Y, d.Last.Y)));
            if (box.Width > 6 && box.Height > 6)
            {
                if (d.Kind == DragKind.NewBox)
                {
                    this.BoxDrawn?.Invoke(this, new(box));
                }
                else if (this.current is { } c)
                {
                    this.current = c with { Erasures = c.Erasures.Append(box).ToArray() };
                    this.CutoutChanged?.Invoke(this, new(this.current));
                }

                this.Mode = CanvasMode.Edit;
            }
        }

        this.drag = null;
        this.ReleasePointerCapture(e.Pointer);
        this.canvas.Invalidate();
    }

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        var factor = MathF.Exp(point.Properties.MouseWheelDelta * 0.0015f);
        var next = Math.Clamp(this.scale * factor, 0.03f, 8f);
        var x = (float)point.Position.X;
        var y = (float)point.Position.Y;
        this.offsetX = x - ((x - this.offsetX) * next / this.scale);
        this.offsetY = y - ((y - this.offsetY) * next / this.scale);
        this.scale = next;
        this.userMovedView = true;
        this.canvas.Invalidate();
        e.Handled = true;
    }

    private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (this.current is not { } c)
        {
            return;
        }

        var p = this.ToPage(e.GetPosition(this));
        var best = -1;
        var bestDistance = 12 / this.scale;
        for (var i = 0; i < c.Outline.Count; i++)
        {
            var distance = SegmentDistance(p, ToSk(c.Outline[i]), ToSk(c.Outline[(i + 1) % c.Outline.Count]));
            if (distance < bestDistance)
            {
                (best, bestDistance) = (i, distance);
            }
        }

        if (best >= 0)
        {
            var outline = c.Outline.ToList();
            outline.Insert(best + 1, new PixelPoint((int)Math.Round(p.X), (int)Math.Round(p.Y)));
            this.current = c with { Outline = outline };
            this.CutoutChanged?.Invoke(this, new(this.current));
            this.canvas.Invalidate();
        }
    }

    private void RemoveAt(SKPoint p)
    {
        if (this.HitTest(p) is not { Original: { } c } hit)
        {
            return;
        }

        if (hit.Kind == DragKind.Corner && c.Outline.Count > 3)
        {
            this.current = c with { Outline = c.Outline.Where((_, i) => i != hit.Index).ToArray() };
        }
        else if (hit.Kind == DragKind.Erasure)
        {
            this.current = c with { Erasures = c.Erasures.Where((_, i) => i != hit.Index).ToArray() };
        }
        else
        {
            return;
        }

        this.CutoutChanged?.Invoke(this, new(this.current));
        this.canvas.Invalidate();
    }
}
