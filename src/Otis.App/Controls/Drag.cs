// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Projects;
using SkiaSharp;

namespace Otis.App.Controls;

internal sealed record Drag(DragKind Kind, SKPoint Start, Cutout? Original, int Index = -1)
{
    public SKPoint Last { get; set; } = Start;

    public float ViewX { get; init; }

    public float ViewY { get; init; }
}
