// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Building;
using Otis.Core.Projects;
using Otis.Infrastructure.Fonts;
using Otis.Infrastructure.Outlines;
using Otis.Infrastructure.Pages;
using Otis.Infrastructure.Storage;

namespace Otis.Infrastructure;

// The one place that knows how the parts fit together.
public sealed class OtisWorkspace(TimeProvider time, PageCache pages, FontInstaller installer, SettingsStore settings)
{
    public ProjectStore Projects { get; } = new(time);

    public SettingsStore Settings { get; } = settings;

    public PageCache Pages { get; } = pages;

    public static OtisWorkspace CreateDefault(TimeProvider time)
    {
        var data = DataDirectory.Resolve();
        return new OtisWorkspace(time, new PageCache(Path.Combine(data, "pages")), FontInstaller.ForCurrentUser(), new SettingsStore(data));
    }

    public ProjectPages PagesFor(string projectPath, OtisProject project) => new(projectPath, project.PageResolution, this.Pages);

    public async Task<BuildOutcome> BuildAsync(string projectPath, OtisProject project, FontVersion version)
    {
        if (VersionPolicy.Refusal(project, version) is { } refusal)
        {
            throw new InvalidOperationException(refusal);
        }

        var builtAt = time.GetUtcNow();
        var projectPages = this.PagesFor(projectPath, project);
        var build = await Task.Run(() => FontBuilder.Build(project, version, builtAt, projectPages, new SkiaOverlapRemover()));

        var face = WindowsFontCheck.FaceForWesternText(build.FontFile.Span, project.Font.FamilyName);
        if (!string.Equals(face, project.Font.FamilyName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Windows would replace this font with a fallback for Western text.");
        }

        var record = FontOutput.Write(projectPath, project, build, builtAt);
        var updated = VersionPolicy.Record(project, record);
        this.Projects.Save(projectPath, updated);
        return new BuildOutcome(
            updated, build, record, FontOutput.FontPath(projectPath, updated, record), FontOutput.SpecimenPath(projectPath, updated, record));
    }

    public FontInstallation Installed(OtisProject project) => installer.Find(project.Font.FamilyName);

    public OtisProject Install(string projectPath, OtisProject project, BuildRecord build)
    {
        installer.Install(FontOutput.FontPath(projectPath, project, build), project.Font.FamilyName);
        var updated = project with
        {
            Builds = [.. project.Builds.Select(b => b with { Installed = b.Version == build.Version })],
        };
        this.Projects.Save(projectPath, updated);
        return updated;
    }

    public OtisProject Uninstall(string projectPath, OtisProject project)
    {
        installer.Uninstall(project.Font.FamilyName);
        var updated = project with { Builds = [.. project.Builds.Select(b => b with { Installed = false })] };
        this.Projects.Save(projectPath, updated);
        return updated;
    }
}
