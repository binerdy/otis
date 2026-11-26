// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Projects;

namespace Otis.App.Controls;

public sealed record BrushStroke(InkStroke Stroke, bool Erases, int Index);
