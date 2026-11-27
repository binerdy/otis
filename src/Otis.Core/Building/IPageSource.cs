// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Imaging;

namespace Otis.Core.Building;

public interface IPageSource
{
    RgbImage Page(string source, int page);
}
