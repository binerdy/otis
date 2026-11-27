// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Glyphs;
using Otis.Core.Projects;

namespace Otis.ApiTests;

public sealed class ProjectFileTests
{
    [Fact]
    public void UnchangedProjectSavesIdentically()
    {
        var json = ProjectJson.Write(SampleProject());

        var again = ProjectJson.Write(ProjectJson.Read(json).Project);

        Assert.Equal(json, again);
    }

    [Fact]
    public void CutoutsAreWrittenInOrdinalOrderWhateverTheyWereAddedIn()
    {
        var project = OtisProject.Create("Kanzlei Fraktur")
            .WithCutout("b", Box(10))
            .WithCutout("A", Box(20))
            .WithCutout("a", Box(30));

        var json = ProjectJson.Write(project);

        Assert.True(json.IndexOf("\"A\"", StringComparison.Ordinal) < json.IndexOf("\"a\"", StringComparison.Ordinal));
        Assert.True(json.IndexOf("\"a\"", StringComparison.Ordinal) < json.IndexOf("\"b\"", StringComparison.Ordinal));
    }

    [Fact]
    public void LowercaseAndCapitalStayDistinct()
    {
        var project = ProjectJson.Read(ProjectJson.Write(SampleProject())).Project;

        Assert.Equal(700, project.Cutouts["a"].Outline[0].X);
        Assert.Equal(440, project.Cutouts["A"].Outline[0].X);
    }

    [Fact]
    public void PointsStayOnOneLine()
    {
        var json = ProjectJson.Write(SampleProject());

        Assert.Contains("[700, 820]", json, StringComparison.Ordinal);
        Assert.Contains("[730, 840, 760, 860]", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\r", json, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownFieldsSurviveASave()
    {
        var json = ProjectJson.Write(SampleProject()).Replace(
            "\"formatVersion\": 1,", "\"formatVersion\": 1,\n  \"comment\": \"kept\",", StringComparison.Ordinal);

        var again = ProjectJson.Write(ProjectJson.Read(json).Project);

        Assert.Contains("\"comment\": \"kept\"", again, StringComparison.Ordinal);
    }

    [Fact]
    public void NewerFormatIsReadOnly()
    {
        var json = ProjectJson.Write(SampleProject() with { FormatVersion = OtisProject.CurrentFormatVersion + 1 });

        Assert.True(ProjectJson.Read(json).IsNewerFormat);
    }

    [Fact]
    public void MovingACutoutMovesErasuresAndLines()
    {
        var moved = SampleProject().Cutouts["a"].MoveBy(5, -10);

        Assert.Equal(new PixelPoint(705, 810), moved.Outline[0]);
        Assert.Equal(new PixelRect(735, 830, 765, 850), moved.Erasures[0]);
        Assert.Equal((935, 835), (moved.Baseline, moved.XLine));
    }

    [Theory]
    [InlineData("1.003", "1.004")]
    [InlineData("1.009", "1.010")]
    [InlineData("1.999", "2.000")]
    public void NextVersionCountsUp(string current, string next) =>
        Assert.Equal(next, FontVersion.Parse(current).Next().ToString());

    [Theory]
    [InlineData("1.4")]
    [InlineData("0.100")]
    [InlineData("1.0040")]
    [InlineData("v1.004")]
    public void MalformedVersionsAreRejected(string text) => Assert.False(FontVersion.TryParse(text, out _));

    [Fact]
    public void VersionsCompareNumerically() => Assert.True(FontVersion.Parse("1.010") > FontVersion.Parse("1.009"));

    [Fact]
    public void CoverageSeesDerivedUmlautsAndMissingLetters()
    {
        var project = OtisProject.Create("Kanzlei Fraktur")
            .WithCutout("a", Box(1))
            .WithCutout("dotaccent.src", Box(2))
            .WithCutout("J", Box(3));

        var coverage = Coverage.Of(project);

        Assert.Equal(CoverageState.CutOut, coverage["a"]);
        Assert.Equal(CoverageState.Derived, coverage["adieresis"]);
        Assert.Equal(CoverageState.Derived, coverage["I"]);
        Assert.Equal(CoverageState.Missing, coverage["odieresis"]);
        Assert.Equal(CoverageState.Missing, coverage["germandbls"]);
        Assert.Contains("O", coverage.MissingGlyphs);
    }

    [Fact]
    public void GlyphNamesRoundTrip()
    {
        foreach (var glyph in CharacterSet.All.Where(g => !CharacterSet.IsBuildingBlock(g)))
        {
            Assert.Equal(glyph, GlyphNames.NameOf(GlyphNames.CharacterOf(glyph)!.Value));
        }
    }

    private static Cutout Box(int x) => Cutout.Rectangle("pages.pdf", 1, new PixelRect(x, 0, x + 10, 10), 8, 3);

    private static OtisProject SampleProject() => OtisProject.Create("Kanzlei Fraktur")
        .WithCutout("a", Cutout.Rectangle("pages.pdf", 1, new PixelRect(700, 820, 779, 955), 945, 845) with
        {
            Erasures = [new PixelRect(730, 840, 760, 860)],
        })
        .WithCutout("A", Cutout.Rectangle("pages.pdf", 1, new PixelRect(440, 680, 750, 1003), 945, 845)) with
    {
        Sources = ["pages.pdf"],
    };
}
