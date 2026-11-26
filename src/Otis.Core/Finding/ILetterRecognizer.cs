// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Imaging;

namespace Otis.Core.Finding;

public interface ILetterRecognizer
{
    IReadOnlyList<RecognizedLetter> Recognize(RgbImage page, CancellationToken cancellationToken);
}
