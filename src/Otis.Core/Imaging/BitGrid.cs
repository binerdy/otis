// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.Core.Imaging;

public sealed class BitGrid : IEquatable<BitGrid>
{
    private readonly bool[] bits;

    public BitGrid(int width, int height)
        : this(width, height, new bool[width * height])
    {
    }

    private BitGrid(int width, int height, bool[] bits)
    {
        this.Width = width;
        this.Height = height;
        this.bits = bits;
    }

    public int Width { get; }

    public int Height { get; }

    public int Count => this.bits.Count(b => b);

    public bool this[int x, int y]
    {
        get => x >= 0 && y >= 0 && x < this.Width && y < this.Height && this.bits[(y * this.Width) + x];
        set => this.bits[(y * this.Width) + x] = value;
    }

    public BitGrid Clone() => new(this.Width, this.Height, (bool[])this.bits.Clone());

    public BitGrid Or(BitGrid other) => this.Combine(other, static (a, b) => a || b);

    public BitGrid And(BitGrid other) => this.Combine(other, static (a, b) => a && b);

    public BitGrid AndNot(BitGrid other) => this.Combine(other, static (a, b) => a && !b);

    public bool Any() => Array.IndexOf(this.bits, true) >= 0;

    public (int Left, int Top, int Right, int Bottom)? Bounds()
    {
        int left = int.MaxValue, top = int.MaxValue, right = -1, bottom = -1;
        for (var y = 0; y < this.Height; y++)
        {
            for (var x = 0; x < this.Width; x++)
            {
                if (this.bits[(y * this.Width) + x])
                {
                    left = Math.Min(left, x);
                    right = Math.Max(right, x);
                    top = Math.Min(top, y);
                    bottom = Math.Max(bottom, y);
                }
            }
        }

        return right < 0 ? null : (left, top, right + 1, bottom + 1);
    }

    public bool Equals(BitGrid? other) =>
        other is not null && other.Width == this.Width && other.Height == this.Height && other.bits.AsSpan().SequenceEqual(this.bits);

    public override bool Equals(object? obj) => this.Equals(obj as BitGrid);

    public override int GetHashCode() => HashCode.Combine(this.Width, this.Height, this.Count);

    private BitGrid Combine(BitGrid other, Func<bool, bool, bool> op)
    {
        var result = new bool[this.bits.Length];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = op(this.bits[i], other.bits[i]);
        }

        return new BitGrid(this.Width, this.Height, result);
    }
}
