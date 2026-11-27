// SPDX-License-Identifier: GPL-3.0-or-later
using System.Security.Cryptography;
using Microsoft.Win32;
using Otis.ApiTests.Support;
using Otis.Core.Projects;
using Otis.Infrastructure;
using Otis.Infrastructure.Fonts;
using Otis.Infrastructure.Pages;
using Otis.Infrastructure.Storage;

namespace Otis.ApiTests;

public sealed class WorkspaceTests : IDisposable
{
    private readonly TempDirectory temp = new();
    private readonly string registryKey = $@"Software\Otis.Tests\{Guid.NewGuid():N}";
    private readonly FakeTime time = new(new DateTimeOffset(2026, 11, 26, 9, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task BuildWritesVersionedFilesAndRecordsHistory()
    {
        var (workspace, path, project) = this.Setup();

        var outcome = await workspace.BuildAsync(path, project, FontVersion.Parse("1.000"));

        Assert.EndsWith("OtisSample-Regular-1.000.ttf", outcome.FontPath, StringComparison.Ordinal);
        Assert.True(File.Exists(outcome.FontPath));
        Assert.True(File.Exists(outcome.SpecimenPath));
        var record = Assert.Single(ProjectStore.Load(path).Project.Builds);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(outcome.FontPath, TestContext.Current.CancellationToken))), record.Sha256);
    }

    [Fact]
    public async Task EarlierVersionsStayOnDisk()
    {
        var (workspace, path, project) = this.Setup();

        var first = await workspace.BuildAsync(path, project, FontVersion.Parse("1.000"));
        var second = await workspace.BuildAsync(path, first.Project, FontVersion.Parse("1.001"));

        Assert.True(File.Exists(first.FontPath));
        Assert.True(File.Exists(second.FontPath));
        Assert.Equal(2, second.Project.Builds.Count);
    }

    [Fact]
    public async Task LowerVersionIsRefused()
    {
        var (workspace, path, project) = this.Setup();
        var built = await workspace.BuildAsync(path, project, FontVersion.Parse("1.003"));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => workspace.BuildAsync(path, built.Project, FontVersion.Parse("1.002")));

        Assert.Equal("Version 1.002 is not newer than 1.003.", error.Message);
    }

    [Fact]
    public async Task SourcesAreNeverModified()
    {
        var (workspace, path, project) = this.Setup();
        var scan = this.temp.File(SampleLettering.Source);
        var before = SHA256.HashData(await File.ReadAllBytesAsync(scan, TestContext.Current.CancellationToken));

        await workspace.BuildAsync(path, project, FontVersion.Parse("1.000"));

        Assert.Equal(before, SHA256.HashData(await File.ReadAllBytesAsync(scan, TestContext.Current.CancellationToken)));
    }

    [Fact]
    public async Task InstallingAnUpdateReplacesTheOlderVersion()
    {
        var (workspace, path, project) = this.Setup();
        var first = await workspace.BuildAsync(path, project, FontVersion.Parse("1.000"));
        project = workspace.Install(path, first.Project, first.Record);
        var second = await workspace.BuildAsync(path, project, FontVersion.Parse("1.001"));

        project = workspace.Install(path, second.Project, second.Record);

        var installed = workspace.Installed(project);
        Assert.Equal(InstallScope.CurrentUser, installed.Scope);
        Assert.Equal(FontVersion.Parse("1.001"), installed.Version);
        Assert.False(File.Exists(this.temp.File(Path.Combine("installed", first.Record.FileName))));
        Assert.Equal([false, true], project.Builds.OrderBy(b => b.Version).Select(b => b.Installed));
        using var key = Registry.CurrentUser.OpenSubKey(this.registryKey);
        Assert.Single(key!.GetValueNames());
    }

    [Fact]
    public async Task InstalledVersionCannotBeRebuilt()
    {
        var (workspace, path, project) = this.Setup();
        var built = await workspace.BuildAsync(path, project, FontVersion.Parse("1.000"));
        project = workspace.Install(path, built.Project, built.Record);

        await Assert.ThrowsAsync<InvalidOperationException>(() => workspace.BuildAsync(path, project, FontVersion.Parse("1.000")));
    }

    [Fact]
    public async Task UninstallRemovesTheRegistration()
    {
        var (workspace, path, project) = this.Setup();
        var built = await workspace.BuildAsync(path, project, FontVersion.Parse("1.000"));
        project = workspace.Install(path, built.Project, built.Record);

        project = workspace.Uninstall(path, project);

        Assert.Equal(InstallScope.None, workspace.Installed(project).Scope);
        Assert.All(project.Builds, b => Assert.False(b.Installed));
    }

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree(this.registryKey, throwOnMissingSubKey: false);
        using (var parent = Registry.CurrentUser.OpenSubKey(@"Software\Otis.Tests"))
        {
            if (parent is { SubKeyCount: 0, ValueCount: 0 })
            {
                Registry.CurrentUser.DeleteSubKey(@"Software\Otis.Tests", throwOnMissingSubKey: false);
            }
        }

        this.temp.Dispose();
    }

    private (OtisWorkspace Workspace, string Path, OtisProject Project) Setup()
    {
        var (page, project) = SampleLettering.Create("aenoJA·");
        using (var bitmap = Bitmaps.ToBitmap(page))
        {
            Bitmaps.SavePng(bitmap, this.temp.File(SampleLettering.Source));
        }

        var installer = new FontInstaller(new FontLocations(this.temp.File("installed"), this.registryKey, this.registryKey + @"\Machine", AnnounceChanges: false));
        var workspace = new OtisWorkspace(this.time, new PageCache(this.temp.File("cache")), installer, new SettingsStore(this.temp.File("data")));
        var path = this.temp.File("Sample.otis.json");
        workspace.Projects.Save(path, project);
        return (workspace, path, project);
    }
}
