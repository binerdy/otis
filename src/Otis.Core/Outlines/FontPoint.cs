// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.Core.Outlines;

public readonly record struct FontPoint(double X, double Y)
{
    public static FontPoint operator +(FontPoint a, FontPoint b) => Add(a, b);

    public static FontPoint operator -(FontPoint a, FontPoint b) => Subtract(a, b);

    public static FontPoint operator *(FontPoint a, double f) => Multiply(a, f);

    public static FontPoint Add(FontPoint a, FontPoint b) => new(a.X + b.X, a.Y + b.Y);

    public static FontPoint Subtract(FontPoint a, FontPoint b) => new(a.X - b.X, a.Y - b.Y);

    public static FontPoint Multiply(FontPoint a, double f) => new(a.X * f, a.Y * f);

    public double DistanceTo(FontPoint other) => Math.Sqrt(((this.X - other.X) * (this.X - other.X)) + ((this.Y - other.Y) * (this.Y - other.Y)));
}
