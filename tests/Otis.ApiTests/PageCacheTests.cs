// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.ApiTests.Support;
using Otis.Infrastructure.Pages;
using SkiaSharp;

namespace Otis.ApiTests;

public sealed class PageCacheTests
{
    [Fact]
    public async Task PdfPagesRenderAtTheProjectResolution()
    {
        using var temp = new TempDirectory();
        var pdf = temp.File("pages.pdf");
        WritePdf(pdf, pages: 2);
        var cache = new PageCache(temp.File("cache"));

        var count = await PageCache.PageCountAsync(pdf);
        using var page = SKBitmap.Decode(await cache.EnsureAsync(pdf, 2, 144));

        Assert.Equal(2, count);
        Assert.Equal((400, 600), (page.Width, page.Height));
    }

    [Fact]
    public async Task ImagesKeepTheirNativeSize()
    {
        using var temp = new TempDirectory();
        using var page = new TestPage(321, 123);
        page.Rect(10, 10, 50, 50);
        var png = temp.File("scan.png");
        page.SavePng(png);
        var cache = new PageCache(temp.File("cache"));

        using var rendered = SKBitmap.Decode(await cache.EnsureAsync(png, 1, 400));

        Assert.Equal((321, 123), (rendered.Width, rendered.Height));
        Assert.True(rendered.GetPixel(30, 30).Red < 60);
    }

    [Fact]
    public async Task SecondOpeningComesFromTheCache()
    {
        using var temp = new TempDirectory();
        using var page = new TestPage(100, 100);
        var png = temp.File("scan.png");
        page.SavePng(png);
        var cache = new PageCache(temp.File("cache"));
        var first = await cache.EnsureAsync(png, 1, 400);
        await File.WriteAllTextAsync(first, "cached", TestContext.Current.CancellationToken);

        var second = await new PageCache(temp.File("cache")).EnsureAsync(png, 1, 400);

        Assert.Equal(first, second);
        Assert.Equal("cached", await File.ReadAllTextAsync(second, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ChangedSourceIsRenderedAgain()
    {
        using var temp = new TempDirectory();
        var png = temp.File("scan.png");
        using (var page = new TestPage(100, 100))
        {
            page.SavePng(png);
        }

        var cache = new PageCache(temp.File("cache"));
        var before = await cache.EnsureAsync(png, 1, 400);
        using (var page = new TestPage(100, 100))
        {
            page.Rect(0, 0, 50, 50);
            page.SavePng(png);
        }

        Assert.NotEqual(before, await cache.EnsureAsync(png, 1, 400));
    }

    [Fact]
    public async Task MissingPageIsReported()
    {
        using var temp = new TempDirectory();
        var pdf = temp.File("pages.pdf");
        WritePdf(pdf, pages: 1);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => new PageCache(temp.File("cache")).EnsureAsync(pdf, 3, 72));
    }

    [Fact]
    public async Task PagesExportAsOnePdfInOrder()
    {
        using var temp = new TempDirectory();
        var pdf = temp.File("pages.pdf");
        WritePdf(pdf, pages: 2);
        using var page = new TestPage(200, 300);
        var png = temp.File("extra.png");
        page.SavePng(png);
        var target = temp.File("all.pdf");

        await PagesPdf.ExportAsync([pdf, png], 72, new PageCache(temp.File("cache")), target);

        Assert.Equal(3, await PageCache.PageCountAsync(target));
    }

    private static void WritePdf(string path, int pages)
    {
        using var stream = File.Create(path);
        using var document = SKDocument.CreatePdf(stream);
        using var paint = new SKPaint { Color = SKColors.Black };
        for (var i = 0; i < pages; i++)
        {
            var canvas = document.BeginPage(200, 300);
            canvas.DrawRect(20, 20, 40, 40, paint);
            document.EndPage();
        }

        document.Close();
    }
}
