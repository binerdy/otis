// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.ApiTests.Support;
using Otis.Core.Projects;
using Otis.Infrastructure.Storage;

namespace Otis.ApiTests;

public sealed class ProjectStoreTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 26, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SavingWithoutChangesMakesNoBackup()
    {
        using var temp = new TempDirectory();
        var store = new ProjectStore(new FakeTime(Start));
        var path = temp.File("Kanzlei.otis.json");
        var project = OtisProject.Create("Kanzlei Fraktur");

        store.Save(path, project);
        store.Save(path, project);

        Assert.False(Directory.Exists(temp.File(ProjectStore.BackupFolder)));
    }

    [Fact]
    public void PreviousVersionIsKeptBeforeEachSave()
    {
        using var temp = new TempDirectory();
        var store = new ProjectStore(new FakeTime(Start));
        var path = temp.File("Kanzlei.otis.json");
        var first = OtisProject.Create("Kanzlei Fraktur");
        store.Save(path, first);

        store.Save(path, first.WithCutout("a", Cutout.Rectangle("scan.png", 1, new PixelRect(1, 1, 9, 9), 8, 4)));

        var backup = Assert.Single(Directory.GetFiles(temp.File(ProjectStore.BackupFolder)));
        Assert.Equal(ProjectJson.Write(first), File.ReadAllText(backup));
        Assert.StartsWith("Kanzlei-20261126-090000", Path.GetFileName(backup), StringComparison.Ordinal);
    }

    [Fact]
    public void OnlyTheNewestFiftyBackupsAreKept()
    {
        using var temp = new TempDirectory();
        var time = new FakeTime(Start);
        var store = new ProjectStore(time);
        var path = temp.File("Kanzlei.otis.json");
        for (var i = 0; i < 53; i++)
        {
            store.Save(path, OtisProject.Create($"Kanzlei Fraktur {i}"));
            time.Advance(TimeSpan.FromSeconds(1));
        }

        Assert.Equal(50, Directory.GetFiles(temp.File(ProjectStore.BackupFolder)).Length);
    }

    [Fact]
    public void ProjectFromANewerOtisIsNotOverwritten()
    {
        using var temp = new TempDirectory();
        var store = new ProjectStore(new FakeTime(Start));

        Assert.Throws<InvalidOperationException>(() =>
            store.Save(temp.File("New.otis.json"), OtisProject.Create("Kanzlei Fraktur") with { FormatVersion = 99 }));
    }

    [Fact]
    public void SourcesAreStoredRelativeWithForwardSlashes()
    {
        var project = Path.Combine(Path.GetTempPath(), "family", "Kanzlei.otis.json");
        var scan = Path.Combine(Path.GetTempPath(), "family", "scans", "page 1.pdf");

        var relative = ProjectStore.RelativeSource(project, scan);

        Assert.Equal("scans/page 1.pdf", relative);
        Assert.Equal(scan, ProjectStore.SourcePath(project, relative));
    }

    [Fact]
    public void SettingsRememberRecentProjectsNewestFirst()
    {
        using var temp = new TempDirectory();
        var store = new SettingsStore(temp.Path);

        store.Save(store.Load().Opened(@"C:\a.otis.json").Opened(@"C:\b.otis.json").Opened(@"C:\A.otis.json"));

        Assert.Equal([@"C:\A.otis.json", @"C:\b.otis.json"], store.Load().RecentProjects);
    }
}
