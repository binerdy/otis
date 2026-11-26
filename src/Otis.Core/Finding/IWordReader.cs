// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Imaging;

namespace Otis.Core.Finding;

public interface IWordReader
{
    Task<IReadOnlyList<ReadWord>> ReadAsync(RgbImage page, CancellationToken cancellationToken);
}
