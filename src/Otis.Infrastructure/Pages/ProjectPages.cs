// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Building;
using Otis.Core.Imaging;
using Otis.Infrastructure.Storage;
using SkiaSharp;

namespace Otis.Infrastructure.Pages;

// A page at 400 dpi is about 45 MB as RGB, so only the few most recent pages stay in memory.
public sealed class ProjectPages(string projectPath, int dpi, PageCache cache) : IPageSource
{
    private const int KeptPages = 4;
    private readonly LinkedList<((string Source, int Page) Key, RgbImage Image)> recent = new();
    private readonly Lock gate = new();

    public async Task<RgbImage> PageAsync(string source, int page)
    {
        lock (this.gate)
        {
            foreach (var (key, cached) in this.recent)
            {
                if (key == (source, page))
                {
                    return cached;
                }
            }
        }

        var path = ProjectStore.SourcePath(projectPath, source);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"{source} is missing.", path);
        }

        var png = await cache.EnsureAsync(path, page, dpi);
        using var bitmap = SKBitmap.Decode(png);
        var image = Bitmaps.ToRgb(bitmap);
        lock (this.gate)
        {
            this.recent.AddFirst(((source, page), image));
            while (this.recent.Count > KeptPages)
            {
                this.recent.RemoveLast();
            }
        }

        return image;
    }

    public RgbImage Page(string source, int page) => Task.Run(() => this.PageAsync(source, page)).GetAwaiter().GetResult();
}
