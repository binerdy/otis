// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.Core.Finding;

public readonly record struct GuideLines(int XLine, int Baseline)
{
    public int XHeight => this.Baseline - this.XLine;
}
