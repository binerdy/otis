// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Projects;

namespace Otis.Core.Finding;

// Letter confidence alone is unreliable on hand lettering; the word's confidence catches garbled readings.
public sealed record RecognizedLetter(string Text, PixelRect Box, double Confidence, double WordConfidence)
{
    public double Trust => Math.Min(this.Confidence, this.WordConfidence);
}
