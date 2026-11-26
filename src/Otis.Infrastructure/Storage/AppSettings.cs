// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.Infrastructure.Storage;

public sealed record AppSettings
{
    private const int KeptRecentProjects = 10;

    public AppLanguage Language { get; init; } = AppLanguage.System;

    public bool NightlyUpdates { get; init; }

    public IReadOnlyList<string> RecentProjects { get; init; } = [];

    public AppSettings Opened(string projectPath) => this with
    {
        RecentProjects = [.. this.RecentProjects.Prepend(projectPath).Distinct(StringComparer.OrdinalIgnoreCase).Take(KeptRecentProjects)],
    };
}
