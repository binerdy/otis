// SPDX-License-Identifier: GPL-3.0-or-later
using System.Buffers.Binary;
using System.Text;
using Otis.Core.Projects;

namespace Otis.Core.TrueType;

public static class TrueTypeNames
{
    public const int Family = 1;
    public const int Subfamily = 2;
    public const int UniqueId = 3;
    public const int Version = 5;

    public static string? Read(ReadOnlySpan<byte> font, int nameId)
    {
        if (font.Length < 12)
        {
            return null;
        }

        var name = Table(font, "name");
        if (name.IsEmpty)
        {
            return null;
        }

        var count = BinaryPrimitives.ReadUInt16BigEndian(name[2..]);
        var strings = BinaryPrimitives.ReadUInt16BigEndian(name[4..]);
        for (var i = 0; i < count && 6 + (12 * (i + 1)) <= name.Length; i++)
        {
            var r = name.Slice(6 + (12 * i), 12);
            if (BinaryPrimitives.ReadUInt16BigEndian(r) == 3 && BinaryPrimitives.ReadUInt16BigEndian(r[6..]) == nameId)
            {
                var length = BinaryPrimitives.ReadUInt16BigEndian(r[8..]);
                var offset = BinaryPrimitives.ReadUInt16BigEndian(r[10..]);
                return strings + offset + length <= name.Length ? Encoding.BigEndianUnicode.GetString(name.Slice(strings + offset, length)) : null;
            }
        }

        return null;
    }

    // Only fonts built by Otis carry ";OTIS;" in their unique id, so their version can be trusted.
    public static FontVersion? OtisVersion(ReadOnlySpan<byte> font)
    {
        var unique = Read(font, UniqueId);
        if (unique is null || !unique.Contains(";OTIS;", StringComparison.Ordinal))
        {
            return null;
        }

        return FontVersion.TryParse(unique.Split(';')[0], out var version) ? version : null;
    }

    private static ReadOnlySpan<byte> Table(ReadOnlySpan<byte> font, string tag)
    {
        var count = BinaryPrimitives.ReadUInt16BigEndian(font[4..]);
        for (var i = 0; i < count && 12 + (16 * (i + 1)) <= font.Length; i++)
        {
            var record = font.Slice(12 + (16 * i), 16);
            if (Encoding.ASCII.GetString(record[..4]) == tag)
            {
                var offset = (int)BinaryPrimitives.ReadUInt32BigEndian(record[8..]);
                var length = (int)BinaryPrimitives.ReadUInt32BigEndian(record[12..]);
                return offset + length <= font.Length ? font.Slice(offset, length) : [];
            }
        }

        return [];
    }
}
