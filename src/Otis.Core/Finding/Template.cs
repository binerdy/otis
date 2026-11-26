// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Projects;

namespace Otis.Core.Finding;

// The example letter as blurred ink at a normal x-height, centred for correlation.
public sealed class Template
{
    private readonly float[] centred;
    private readonly double norm;

    public Template(float[] values, int width, int height, double topOffset, Cutout reference)
    {
        this.Width = width;
        this.Height = height;
        this.TopOffset = topOffset;
        this.Reference = reference;
        this.InkAmount = values.Sum();
        var mean = this.InkAmount / values.Length;
        this.centred = [.. values.Select(v => (float)(v - mean))];
        this.norm = Math.Sqrt(this.centred.Sum(v => (double)v * v));
    }

    public int Width { get; }

    public int Height { get; }

    public double TopOffset { get; }

    public Cutout Reference { get; }

    public double InkAmount { get; }

    public double Correlate(float[] band, int bandWidth, int x, int dy)
    {
        var sum = 0.0;
        for (var v = 0; v < this.Height; v++)
        {
            var row = (v + dy) * bandWidth;
            for (var u = 0; u < this.Width; u++)
            {
                sum += band[row + x + u];
            }
        }

        var mean = sum / (this.Width * this.Height);
        double dot = 0, energy = 0;
        for (var v = 0; v < this.Height; v++)
        {
            var row = (v + dy) * bandWidth;
            for (var u = 0; u < this.Width; u++)
            {
                var b = band[row + x + u] - mean;
                dot += b * this.centred[(v * this.Width) + u];
                energy += b * b;
            }
        }

        return energy <= 0 || this.norm <= 0 ? 0 : dot / (Math.Sqrt(energy) * this.norm);
    }
}
