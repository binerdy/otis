// SPDX-License-Identifier: GPL-3.0-or-later
using System.Buffers.Binary;
using System.Text;

namespace Otis.ApiTests.Support;

internal sealed class FontTables(byte[] font)
{
    private readonly byte[] bytes = font;

    public ReadOnlySpan<byte> Table(string tag)
    {
        var count = BinaryPrimitives.ReadUInt16BigEndian(this.bytes.AsSpan(4));
        for (var i = 0; i < count; i++)
        {
            var record = 12 + (16 * i);
            if (Encoding.ASCII.GetString(this.bytes, record, 4) == tag)
            {
                var offset = (int)BinaryPrimitives.ReadUInt32BigEndian(this.bytes.AsSpan(record + 8));
                var length = (int)BinaryPrimitives.ReadUInt32BigEndian(this.bytes.AsSpan(record + 12));
                return this.bytes.AsSpan(offset, length);
            }
        }

        throw new KeyNotFoundException(tag);
    }

    public IEnumerable<string> Tags()
    {
        var count = BinaryPrimitives.ReadUInt16BigEndian(this.bytes.AsSpan(4));
        return Enumerable.Range(0, count).Select(i => Encoding.ASCII.GetString(this.bytes, 12 + (16 * i), 4));
    }

    public string Name(int id)
    {
        var name = this.Table("name");
        var count = BinaryPrimitives.ReadUInt16BigEndian(name[2..]);
        var stringOffset = BinaryPrimitives.ReadUInt16BigEndian(name[4..]);
        for (var i = 0; i < count; i++)
        {
            var r = name.Slice(6 + (12 * i), 12);
            if (BinaryPrimitives.ReadUInt16BigEndian(r) == 3 && BinaryPrimitives.ReadUInt16BigEndian(r[6..]) == id)
            {
                var length = BinaryPrimitives.ReadUInt16BigEndian(r[8..]);
                var offset = BinaryPrimitives.ReadUInt16BigEndian(r[10..]);
                return Encoding.BigEndianUnicode.GetString(name.Slice(stringOffset + offset, length));
            }
        }

        throw new KeyNotFoundException($"name {id}");
    }

    public ushort Os2(int offset) => BinaryPrimitives.ReadUInt16BigEndian(this.Table("OS/2")[offset..]);

    public uint Os2Long(int offset) => BinaryPrimitives.ReadUInt32BigEndian(this.Table("OS/2")[offset..]);
}
