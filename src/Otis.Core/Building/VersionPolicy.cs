// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Projects;

namespace Otis.Core.Building;

public static class VersionPolicy
{
    public static FontVersion ProposeNext(OtisProject project) =>
        project.LatestBuild is { } latest ? latest.Version.Next() : project.Font.Version;

    public static FontVersion? Rebuildable(OtisProject project) =>
        project.LatestBuild is { Installed: false } latest ? latest.Version : null;

    // An installed version is frozen: Windows and Word treat two files with one version as the same font.
    public static string? Refusal(OtisProject project, FontVersion requested)
    {
        var latest = project.LatestBuild;
        if (latest is null || requested > latest.Version)
        {
            return null;
        }

        if (requested == latest.Version && !latest.Installed)
        {
            return null;
        }

        return requested == latest.Version
            ? $"Version {requested} is installed and cannot change. Build {latest.Version.Next()} instead."
            : $"Version {requested} is not newer than {latest.Version}.";
    }

    public static OtisProject Record(OtisProject project, BuildRecord build) => project with
    {
        Builds = [.. project.Builds.Where(b => b.Version != build.Version), build],
        Font = project.Font with { Version = build.Version },
    };
}
