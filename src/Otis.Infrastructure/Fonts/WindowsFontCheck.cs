// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.InteropServices;

namespace Otis.Infrastructure.Fonts;

public static partial class WindowsFontCheck
{
    private const uint PrivateFont = 0x10;
    private const byte AnsiCharset = 0;

    // Word asks GDI for the family with the Western character set; a font without code page bits gets a fallback.
    public static string FaceForWesternText(ReadOnlySpan<byte> font, string family)
    {
        var path = Path.Combine(Path.GetTempPath(), $"otis-check-{Guid.NewGuid():N}.ttf");
        File.WriteAllBytes(path, font);
        try
        {
            if (AddFontResourceExW(path, PrivateFont, 0) == 0)
            {
                return string.Empty;
            }

            try
            {
                return SelectedFace(family);
            }
            finally
            {
                _ = RemoveFontResourceExW(path, PrivateFont, 0);
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string SelectedFace(string family)
    {
        var dc = GetDC(0);
        var handle = CreateFontW(-100, 0, 0, 0, 400, 0, 0, 0, AnsiCharset, 0, 0, 0, 0, family);
        var previous = SelectObject(dc, handle);
        try
        {
            var buffer = new ushort[64];
            var length = GetTextFaceW(dc, buffer.Length, buffer);
            return length <= 0 ? string.Empty : new string([.. buffer.Take(length - 1).Select(c => (char)c)]);
        }
        finally
        {
            _ = SelectObject(dc, previous);
            _ = DeleteObject(handle);
            _ = ReleaseDC(0, dc);
        }
    }

    [LibraryImport("gdi32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int AddFontResourceExW(string name, uint flags, nint reserved);

    [LibraryImport("gdi32.dll", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RemoveFontResourceExW(string name, uint flags, nint reserved);

    [LibraryImport("gdi32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint CreateFontW(
        int height, int width, int escapement, int orientation, int weight, uint italic, uint underline, uint strikeOut,
        uint charSet, uint outPrecision, uint clipPrecision, uint quality, uint pitchAndFamily, string faceName);

    [LibraryImport("gdi32.dll")]
    private static partial nint SelectObject(nint dc, nint handle);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteObject(nint handle);

    [LibraryImport("gdi32.dll")]
    private static partial int GetTextFaceW(nint dc, int count, [Out] ushort[] name);

    [LibraryImport("user32.dll")]
    private static partial nint GetDC(nint window);

    [LibraryImport("user32.dll")]
    private static partial int ReleaseDC(nint window, nint dc);
}
