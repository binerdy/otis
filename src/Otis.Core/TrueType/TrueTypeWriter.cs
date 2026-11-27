// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text;

namespace Otis.Core.TrueType;

public static class TrueTypeWriter
{
    private const uint ChecksumMagic = 0xB1B0AFBA;
    private const ushort LatinOneCodePage = 1;
    private const int RegularSelection = 0x40;
    private const int UseTypoMetricsSelection = 0x80;
    private const int GaspSmoothing = 0x000A;
    private static readonly DateTimeOffset Epoch1904 = new(1904, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // Glyph 0 must be .notdef; the character map points code points at glyph indices.
    public static byte[] Write(FontDescription font, IReadOnlyList<TrueTypeGlyph> glyphs, IReadOnlyDictionary<int, int> characterMap)
    {
        var metrics = FontMetrics.Of(font, glyphs);
        var (glyf, loca) = GlyfAndLoca(glyphs);
        var tables = new SortedDictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["OS/2"] = Os2(font, glyphs, characterMap, metrics),
            ["cmap"] = Cmap(characterMap),
            ["gasp"] = new BigEndianWriter().U16(1).U16(1).U16(0xFFFF).U16(GaspSmoothing).ToArray(),
            ["glyf"] = glyf,
            ["head"] = Head(font, metrics),
            ["hhea"] = Hhea(glyphs, metrics),
            ["hmtx"] = Hmtx(glyphs),
            ["loca"] = loca,
            ["maxp"] = Maxp(glyphs),
            ["name"] = Name(font),
            ["post"] = Post(font),
        };

        var file = Assemble(tables);
        var adjustment = ChecksumMagic - Checksum(file);
        var headOffset = TableOffset(file, "head");
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(file.AsSpan(headOffset + 8), adjustment);
        return file;
    }

    public static uint Checksum(ReadOnlySpan<byte> data)
    {
        uint sum = 0;
        for (var i = 0; i < data.Length; i += 4)
        {
            uint word = 0;
            for (var k = 0; k < 4; k++)
            {
                word = (word << 8) | (i + k < data.Length ? data[i + k] : 0u);
            }

            sum = unchecked(sum + word);
        }

        return sum;
    }

    private static byte[] Assemble(SortedDictionary<string, byte[]> tables)
    {
        var count = tables.Count;
        var entrySelector = (int)Math.Floor(Math.Log2(count));
        var searchRange = (1 << entrySelector) * 16;
        var w = new BigEndianWriter().U32(0x00010000).U16(count).U16(searchRange).U16(entrySelector).U16((count * 16) - searchRange);

        var offset = 12 + (16 * count);
        foreach (var (tag, data) in tables)
        {
            w.Tag(tag).U32(Checksum(data)).U32((uint)offset).U32((uint)data.Length);
            offset += (data.Length + 3) & ~3;
        }

        foreach (var data in tables.Values)
        {
            w.Bytes(data).PadTo4();
        }

        return w.ToArray();
    }

    private static int TableOffset(byte[] file, string tag)
    {
        var count = (file[4] << 8) | file[5];
        for (var i = 0; i < count; i++)
        {
            var record = 12 + (16 * i);
            if (Encoding.ASCII.GetString(file, record, 4) == tag)
            {
                return (int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(file.AsSpan(record + 8));
            }
        }

        throw new InvalidOperationException($"Table {tag} is missing.");
    }

    private static (byte[] Glyf, byte[] Loca) GlyfAndLoca(IReadOnlyList<TrueTypeGlyph> glyphs)
    {
        var glyf = new BigEndianWriter();
        var loca = new BigEndianWriter();
        foreach (var glyph in glyphs)
        {
            loca.U32((uint)glyf.Length);
            if (!glyph.IsEmpty)
            {
                GlyphData.Write(glyf, glyph);
                glyf.PadTo4();
            }
        }

        loca.U32((uint)glyf.Length);
        return (glyf.ToArray(), loca.ToArray());
    }

    private static byte[] Head(FontDescription font, FontMetrics m) => new BigEndianWriter()
        .U32(0x00010000)
        .U32(FixedRevision(font))
        .U32(0)
        .U32(0x5F0F3CF5)
        .U16(0x0009)
        .U16(font.UnitsPerEm)
        .I64(SecondsSince1904(font.Created))
        .I64(SecondsSince1904(font.Created))
        .I16(m.XMin).I16(m.YMin).I16(m.XMax).I16(m.YMax)
        .U16(0)
        .U16(8)
        .I16(2)
        .I16(1)
        .I16(0)
        .ToArray();

    private static uint FixedRevision(FontDescription font) =>
        (uint)Math.Round((font.Version.Major + (font.Version.Minor / 1000.0)) * 65536, MidpointRounding.AwayFromZero);

    private static long SecondsSince1904(DateTimeOffset time) => (long)(time.ToUniversalTime() - Epoch1904).TotalSeconds;

    private static byte[] Hhea(IReadOnlyList<TrueTypeGlyph> glyphs, FontMetrics m)
    {
        var drawn = glyphs.Where(g => !g.IsEmpty).ToList();
        var w = new BigEndianWriter()
            .U32(0x00010000)
            .I16(m.Ascender).I16(m.Descender).I16(0)
            .U16(glyphs.Max(g => g.AdvanceWidth))
            .I16(drawn.Count == 0 ? 0 : drawn.Min(g => g.XMin))
            .I16(drawn.Count == 0 ? 0 : drawn.Min(g => g.AdvanceWidth - g.XMax))
            .I16(drawn.Count == 0 ? 0 : drawn.Max(g => g.XMax))
            .I16(1).I16(0).I16(0);
        for (var i = 0; i < 4; i++)
        {
            w.I16(0);
        }

        return w.I16(0).U16(glyphs.Count).ToArray();
    }

    private static byte[] Hmtx(IReadOnlyList<TrueTypeGlyph> glyphs)
    {
        var w = new BigEndianWriter();
        foreach (var glyph in glyphs)
        {
            w.U16(glyph.AdvanceWidth).I16(glyph.XMin);
        }

        return w.ToArray();
    }

    private static byte[] Maxp(IReadOnlyList<TrueTypeGlyph> glyphs)
    {
        var w = new BigEndianWriter()
            .U32(0x00010000)
            .U16(glyphs.Count)
            .U16(glyphs.Max(g => g.PointCount))
            .U16(glyphs.Max(g => g.Contours.Count))
            .U16(0).U16(0)
            .U16(2);
        for (var i = 0; i < 8; i++)
        {
            w.U16(0);
        }

        return w.ToArray();
    }

    private static byte[] Os2(FontDescription font, IReadOnlyList<TrueTypeGlyph> glyphs, IReadOnlyDictionary<int, int> characterMap, FontMetrics m)
    {
        var upm = font.UnitsPerEm;
        var advances = glyphs.Where(g => g.AdvanceWidth > 0).Select(g => g.AdvanceWidth).DefaultIfEmpty(0);
        var w = new BigEndianWriter()
            .U16(4)
            .I16((int)Math.Round(advances.Average()))
            .U16(400)
            .U16(5)
            .U16(0)
            .I16(upm * 65 / 100).I16(upm * 60 / 100).I16(0).I16(upm * 7 / 100)
            .I16(upm * 65 / 100).I16(upm * 60 / 100).I16(0).I16(upm * 35 / 100)
            .I16(upm * 5 / 100).I16(font.XHeight / 2)
            .I16(0)
            .Bytes(new byte[10]);

        var (r1, r2) = UnicodeRanges(characterMap.Keys);
        w.U32(r1).U32(r2).U32(0).U32(0)
            .Tag("NONE")
            .U16(RegularSelection | UseTypoMetricsSelection)
            .U16(characterMap.Count == 0 ? 0 : Math.Min(characterMap.Keys.Min(), 0xFFFF))
            .U16(characterMap.Count == 0 ? 0 : Math.Min(characterMap.Keys.Max(), 0xFFFF))
            .I16(m.Ascender).I16(m.Descender).I16(0)
            .U16(m.WinAscent).U16(m.WinDescent)
            .U32(LatinOneCodePage).U32(0)
            .I16(font.XHeight).I16(font.CapHeight)
            .U16(0).U16(32).U16(0);
        return w.ToArray();
    }

    private static (uint Range1, uint Range2) UnicodeRanges(IEnumerable<int> codes)
    {
        uint range1 = 0;
        foreach (var code in codes)
        {
            range1 |= code switch
            {
                <= 0x7F => 1u << 0,
                <= 0xFF => 1u << 1,
                <= 0x17F => 1u << 2,
                _ => 0,
            };
        }

        return (range1, 0);
    }

    private static byte[] Cmap(IReadOnlyDictionary<int, int> characterMap)
    {
        var codes = characterMap.Keys.Where(c => c < 0xFFFF).Order().ToList();
        var segments = codes.Select(c => (Start: c, End: c, Delta: characterMap[c] - c)).ToList();
        segments.Add((0xFFFF, 0xFFFF, 1));

        var segCount = segments.Count;
        var entrySelector = (int)Math.Floor(Math.Log2(segCount));
        var searchRange = 2 * (1 << entrySelector);
        var sub = new BigEndianWriter()
            .U16(4).U16(16 + (8 * segCount)).U16(0)
            .U16(segCount * 2).U16(searchRange).U16(entrySelector).U16((segCount * 2) - searchRange);
        WriteAll(sub, segments.Select(s => s.End));
        sub.U16(0);
        WriteAll(sub, segments.Select(s => s.Start));
        WriteAll(sub, segments.Select(s => s.Delta));
        WriteAll(sub, segments.Select(_ => 0));
        var subtable = sub.ToArray();

        return new BigEndianWriter()
            .U16(0).U16(2)
            .U16(0).U16(3).U32(20)
            .U16(3).U16(1).U32(20)
            .Bytes(subtable)
            .ToArray();
    }

    private static void WriteAll(BigEndianWriter writer, IEnumerable<int> values)
    {
        foreach (var value in values)
        {
            writer.U16(value);
        }
    }

    private static byte[] Name(FontDescription font)
    {
        var records = new SortedDictionary<int, string>
        {
            [1] = font.FamilyName,
            [2] = FontDescription.SubfamilyName,
            [3] = font.UniqueId,
            [4] = font.FullName,
            [5] = font.VersionString,
            [6] = font.PostScriptName,
        };
        if (!string.IsNullOrWhiteSpace(font.Designer))
        {
            records[9] = font.Designer;
        }

        var strings = new BigEndianWriter();
        var header = new BigEndianWriter().U16(0).U16(records.Count).U16(6 + (12 * records.Count));
        foreach (var (id, text) in records)
        {
            var encoded = Encoding.BigEndianUnicode.GetBytes(text);
            header.U16(3).U16(1).U16(0x409).U16(id).U16(encoded.Length).U16(strings.Length);
            strings.Bytes(encoded);
        }

        return header.Bytes(strings.ToArray()).ToArray();
    }

    private static byte[] Post(FontDescription font)
    {
        var w = new BigEndianWriter()
            .U32(0x00030000)
            .U32(0)
            .I16(-font.UnitsPerEm / 10)
            .I16(font.UnitsPerEm / 20)
            .U32(0);
        for (var i = 0; i < 4; i++)
        {
            w.U32(0);
        }

        return w.ToArray();
    }
}
