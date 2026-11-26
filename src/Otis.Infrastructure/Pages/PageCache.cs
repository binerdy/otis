// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.Concurrent;
using System.Security.Cryptography;
using PDFtoImage;
using SkiaSharp;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Otis.Infrastructure.Pages;

public sealed class PageCache(string folder)
{
    private readonly ConcurrentDictionary<(string Path, long Length, long WrittenTicks), string> hashes = new();

    public static bool IsPdf(string path) => Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    public static bool IsSupported(string path) =>
        Path.GetExtension(path).ToUpperInvariant() is ".PDF" or ".PNG" or ".JPG" or ".JPEG" or ".TIF" or ".TIFF" or ".BMP";

    public static async Task<int> PageCountAsync(string path)
    {
        if (IsPdf(path))
        {
            await using var pdf = File.OpenRead(path);
            return Conversion.GetPageCount(pdf, leaveOpen: false);
        }

        using var stream = await OpenAsync(path);
        var decoder = await BitmapDecoder.CreateAsync(stream);
        return (int)decoder.FrameCount;
    }

    // Returns the cached PNG of a page, rendering it first when the source changed or was never rendered.
    public async Task<string> EnsureAsync(string path, int page, int dpi)
    {
        Directory.CreateDirectory(folder);
        var cached = Path.Combine(folder, $"{await this.HashAsync(path)}-p{page}-{dpi}dpi.png");
        if (File.Exists(cached))
        {
            return cached;
        }

        using var bitmap = IsPdf(path) ? await RenderPdfAsync(path, page, dpi) : await DecodeImageAsync(path, page);
        Bitmaps.SavePng(bitmap, cached);
        return cached;
    }

    private static async Task<SKBitmap> RenderPdfAsync(string path, int page, int dpi)
    {
        var bytes = await File.ReadAllBytesAsync(path);
        if (page < 1 || page > Conversion.GetPageCount(bytes))
        {
            throw new ArgumentOutOfRangeException(nameof(page), page, "The page does not exist.");
        }

        return Conversion.ToImage(bytes, page - 1, options: new RenderOptions { Dpi = dpi, BackgroundColor = SKColors.White });
    }

    private static async Task<SKBitmap> DecodeImageAsync(string path, int page)
    {
        using var stream = await OpenAsync(path);
        var decoder = await BitmapDecoder.CreateAsync(stream);
        if (page < 1 || page > decoder.FrameCount)
        {
            throw new ArgumentOutOfRangeException(nameof(page), page, "The page does not exist.");
        }

        var frame = await decoder.GetFrameAsync((uint)(page - 1));
        var pixels = await frame.GetPixelDataAsync(
            BitmapPixelFormat.Rgba8,
            BitmapAlphaMode.Straight,
            new BitmapTransform(),
            ExifOrientationMode.RespectExifOrientation,
            ColorManagementMode.ColorManageToSRgb);
        var data = pixels.DetachPixelData();
        var oriented = frame.OrientedPixelWidth;
        var width = (int)oriented;
        var height = data.Length / 4 / width;
        var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        data.CopyTo(bitmap.GetPixelSpan());
        return bitmap;
    }

    private static async Task<IRandomAccessStream> OpenAsync(string path)
    {
        var bytes = await File.ReadAllBytesAsync(path);
        var stream = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
        {
            writer.WriteBytes(bytes);
            await writer.StoreAsync();
            await writer.FlushAsync();
            writer.DetachStream();
        }

        return stream;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0030", Justification = "File timestamps only exist as DateTime; the ticks serve as a cache key.")]
    private static long WrittenTicks(FileInfo info) => info.LastWriteTimeUtc.Ticks;

    private async Task<string> HashAsync(string path)
    {
        var info = new FileInfo(path);
        var key = (info.FullName, info.Length, WrittenTicks(info));
        if (this.hashes.TryGetValue(key, out var known))
        {
            return known;
        }

        await using var stream = File.OpenRead(path);
        var hash = Convert.ToHexStringLower(await SHA256.HashDataAsync(stream))[..32];
        this.hashes[key] = hash;
        return hash;
    }
}
