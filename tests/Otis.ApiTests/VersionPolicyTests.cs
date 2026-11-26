// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Building;
using Otis.Core.Projects;

namespace Otis.ApiTests;

public sealed class VersionPolicyTests
{
    [Fact]
    public void NewProjectStartsAtTheFirstVersion() =>
        Assert.Equal("1.000", VersionPolicy.ProposeNext(OtisProject.Create("Kanzlei Fraktur")).ToString());

    [Fact]
    public void NextVersionFollowsTheLastBuild() =>
        Assert.Equal("1.004", VersionPolicy.ProposeNext(WithBuilds(("1.003", false))).ToString());

    [Fact]
    public void LowerVersionIsRefused() =>
        Assert.Equal("Version 1.002 is not newer than 1.003.", VersionPolicy.Refusal(WithBuilds(("1.003", false)), FontVersion.Parse("1.002")));

    [Fact]
    public void LatestVersionMayBeRebuiltUntilInstalled()
    {
        var project = WithBuilds(("1.003", false), ("1.004", false));

        Assert.Null(VersionPolicy.Refusal(project, FontVersion.Parse("1.004")));
        Assert.Equal(FontVersion.Parse("1.004"), VersionPolicy.Rebuildable(project));
    }

    [Fact]
    public void InstalledVersionIsFrozen()
    {
        var project = WithBuilds(("1.004", true));

        Assert.NotNull(VersionPolicy.Refusal(project, FontVersion.Parse("1.004")));
        Assert.Null(VersionPolicy.Rebuildable(project));
        Assert.Null(VersionPolicy.Refusal(project, FontVersion.Parse("1.005")));
    }

    [Fact]
    public void HistoryDecidesEvenWhenTheFontsFolderIsGone() =>
        Assert.Equal("2.000", VersionPolicy.ProposeNext(WithBuilds(("1.999", true))).ToString());

    [Fact]
    public void RecordingARebuildReplacesTheEntry()
    {
        var project = VersionPolicy.Record(WithBuilds(("1.004", false)), Build("1.004", installed: false) with { Sha256 = "new" });

        var entry = Assert.Single(project.Builds);
        Assert.Equal("new", entry.Sha256);
    }

    private static OtisProject WithBuilds(params (string Version, bool Installed)[] builds) =>
        OtisProject.Create("Kanzlei Fraktur") with { Builds = [.. builds.Select(b => Build(b.Version, b.Installed))] };

    private static BuildRecord Build(string version, bool installed) => new()
    {
        Version = FontVersion.Parse(version),
        BuiltAt = new DateTimeOffset(2026, 11, 26, 9, 0, 0, TimeSpan.Zero),
        FileName = $"KanzleiFraktur-Regular-{version}.ttf",
        Sha256 = "old",
        CutOut = 40,
        Derived = 8,
        Missing = 10,
        Installed = installed,
    };
}
