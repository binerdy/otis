// SPDX-License-Identifier: GPL-3.0-or-later
using System.Buffers.Binary;

namespace Otis.Core.TrueType;

internal sealed class BigEndianWriter
{
    private readonly List<byte> buffer = [];

    public int Length => this.buffer.Count;

    public BigEndianWriter U8(int value)
    {
        this.buffer.Add((byte)value);
        return this;
    }

    public BigEndianWriter U16(int value)
    {
        Span<byte> b = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(b, unchecked((ushort)value));
        this.buffer.AddRange(b);
        return this;
    }

    public BigEndianWriter I16(int value)
    {
        Span<byte> b = stackalloc byte[2];
        BinaryPrimitives.WriteInt16BigEndian(b, checked((short)value));
        this.buffer.AddRange(b);
        return this;
    }

    public BigEndianWriter U32(uint value)
    {
        Span<byte> b = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(b, value);
        this.buffer.AddRange(b);
        return this;
    }

    public BigEndianWriter I64(long value)
    {
        Span<byte> b = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(b, value);
        this.buffer.AddRange(b);
        return this;
    }

    public BigEndianWriter Tag(string tag)
    {
        foreach (var c in tag)
        {
            this.buffer.Add((byte)c);
        }

        return this;
    }

    public BigEndianWriter Bytes(IEnumerable<byte> data)
    {
        this.buffer.AddRange(data);
        return this;
    }

    public BigEndianWriter PadTo4()
    {
        while (this.buffer.Count % 4 != 0)
        {
            this.buffer.Add(0);
        }

        return this;
    }

    public byte[] ToArray() => [.. this.buffer];
}
