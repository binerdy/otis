// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Building;
using Otis.Core.Imaging;

namespace Otis.ApiTests.Support;

internal sealed class MemoryPageSource(Dictionary<(string Source, int Page), RgbImage> pages) : IPageSource
{
    public RgbImage Page(string source, int page) =>
        pages.TryGetValue((source, page), out var image) ? image : throw new FileNotFoundException(source);
}
