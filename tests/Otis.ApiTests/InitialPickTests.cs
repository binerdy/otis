// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Finding;
using Otis.Core.Projects;

namespace Otis.ApiTests;

public sealed class InitialPickTests
{
    [Fact]
    public void OnlyMissingCharactersAreWanted()
    {
        var project = OtisProject.Create("Kanzlei Fraktur")
            .WithCutout("a", Cutout.Rectangle("p.png", 1, new PixelRect(0, 0, 10, 10), 9, 4))
            .WithCutout("dotaccent.src", Cutout.Rectangle("p.png", 1, new PixelRect(0, 0, 10, 10), 9, 4));

        var wanted = InitialPick.Wanted(project);

        Assert.DoesNotContain("a", wanted);
        Assert.DoesNotContain("adieresis", wanted);
        Assert.Contains("b", wanted);
        Assert.Contains("s.alt", wanted);
    }

    [Theory]
    [InlineData("a", "a")]
    [InlineData("ſ", "s.alt")]
    [InlineData("7", "seven")]
    [InlineData("€", null)]
    [InlineData("ch", null)]
    public void RecognisedTextMapsToGlyphs(string text, string? glyph) => Assert.Equal(glyph, InitialPick.GlyphOf(text));
}
