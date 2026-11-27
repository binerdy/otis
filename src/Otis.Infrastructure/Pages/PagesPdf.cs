// SPDX-License-Identifier: GPL-3.0-or-later
using SkiaSharp;

namespace Otis.Infrastructure.Pages;

public static class PagesPdf
{
    private const float PointsPerInch = 72f;

    // Pages are written as images at the project's resolution, in source order, for reading and printing.
    public static async Task ExportAsync(IEnumerable<string> sources, int dpi, PageCache cache, string target)
    {
        await using var stream = File.Create(target);
        using var document = SKDocument.CreatePdf(stream);
        foreach (var source in sources)
        {
            var count = await PageCache.PageCountAsync(source);
            for (var page = 1; page <= count; page++)
            {
                using var bitmap = SKBitmap.Decode(await cache.EnsureAsync(source, page, dpi));
                var scale = PointsPerInch / dpi;
                var canvas = document.BeginPage(bitmap.Width * scale, bitmap.Height * scale);
                canvas.Scale(scale);
                using var image = SKImage.FromBitmap(bitmap);
                canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));
                document.EndPage();
            }
        }

        document.Close();
    }
}
