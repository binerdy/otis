// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.Infrastructure.Storage;

public interface IFileRemover
{
    void Remove(IReadOnlyList<string> files);
}
