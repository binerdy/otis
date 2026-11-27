// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.ApiTests.Support;
using Otis.Core.Projects;
using Otis.Infrastructure.Storage;

namespace Otis.ApiTests;

public sealed class ProjectDeleterTests
{
    [Fact]
    public void ScansSurviveAndBackupsGo()
    {
        using var temp = new TempDirectory();
        var (path, scan, font) = Setup(temp);
        var remover = new DirectRemover();

        new ProjectDeleter(remover).Delete(path, includeFonts: false);

        Assert.False(File.Exists(path));
        Assert.True(File.Exists(scan));
        Assert.False(Directory.Exists(temp.File(ProjectStore.BackupFolder)));
        Assert.True(File.Exists(font));
        Assert.DoesNotContain(remover.Removed, f => f.EndsWith(".png", StringComparison.Ordinal) && !f.Contains("fonts", StringComparison.Ordinal));
    }

    [Fact]
    public void FontsOnlyOnRequest()
    {
        using var temp = new TempDirectory();
        var (path, scan, font) = Setup(temp);

        new ProjectDeleter(new DirectRemover()).Delete(path, includeFonts: true);

        Assert.False(File.Exists(font));
        Assert.False(File.Exists(Path.ChangeExtension(font, ".png")));
        Assert.True(File.Exists(scan));
    }

    [Fact]
    public void OtherProjectsInTheFolderKeepTheirBackups()
    {
        using var temp = new TempDirectory();
        var (path, _, _) = Setup(temp);
        var other = temp.File(Path.Combine(ProjectStore.BackupFolder, "Other-20261126-090000-000.otis.json"));
        File.WriteAllText(other, "{}");

        new ProjectDeleter(new DirectRemover()).Delete(path, includeFonts: false);

        Assert.True(File.Exists(other));
    }

    private static (string Path, string Scan, string Font) Setup(TempDirectory temp)
    {
        var scan = temp.File("scan.png");
        File.WriteAllBytes(scan, [1, 2, 3]);
        Directory.CreateDirectory(temp.File("fonts"));
        var font = temp.File(Path.Combine("fonts", "KanzleiFraktur-Regular-1.000.ttf"));
        File.WriteAllBytes(font, [4]);
        File.WriteAllBytes(Path.ChangeExtension(font, ".png"), [5]);

        var time = new FakeTime(new DateTimeOffset(2026, 11, 26, 9, 0, 0, TimeSpan.Zero));
        var store = new ProjectStore(time);
        var path = temp.File("Kanzlei.otis.json");
        var project = OtisProject.Create("Kanzlei Fraktur") with
        {
            Sources = ["scan.png"],
            Builds =
            [
                new BuildRecord
                {
                    Version = FontVersion.First, BuiltAt = time.GetUtcNow(), FileName = Path.GetFileName(font), Sha256 = "x", CutOut = 1, Derived = 0, Missing = 0,
                },
            ],
        };
        store.Save(path, project);
        time.Advance(TimeSpan.FromSeconds(1));
        store.Save(path, project with { Font = project.Font with { Designer = "someone" } });
        return (path, scan, font);
    }

    private sealed class DirectRemover : IFileRemover
    {
        public List<string> Removed { get; } = [];

        public void Remove(IReadOnlyList<string> files)
        {
            foreach (var file in files)
            {
                this.Removed.Add(file);
                File.Delete(file);
            }
        }
    }
}
