// SPDX-License-Identifier: GPL-3.0-or-later
// Ported from Potrace 1.16. Copyright (C) 2001-2019 Peter Selinger.
// Potrace is free software covered by the GNU General Public License, version 2 or later.
namespace Otis.Core.Tracing;

internal sealed class CurveElement
{
    public bool IsCorner { get; set; }

    public TracePoint C0 { get; set; }

    public TracePoint C1 { get; set; }

    public TracePoint C2 { get; set; }

    public TracePoint Vertex { get; set; }

    public double Alpha { get; set; }

    public double Alpha0 { get; set; }

    public double Beta { get; set; }

    public CurveElement Copy() => (CurveElement)this.MemberwiseClone();
}
