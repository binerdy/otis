// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text.Json.Serialization;

namespace Otis.Core.Projects;

[JsonConverter(typeof(PixelPointConverter))]
public readonly record struct PixelPoint(int X, int Y);
