// SPDX-License-Identifier: GPL-3.0-or-later
using Velopack;
using Velopack.Sources;

namespace Otis.Infrastructure.Updates;

public sealed class VelopackUpdates(Uri repository)
{
    private UpdateManager? manager;
    private UpdateInfo? pending;

    // Returns the version that will be installed when Otis exits, or null when there is nothing to install.
    public async Task<string?> PrepareAsync(bool includeNightly, CancellationToken cancellationToken)
    {
        var current = new UpdateManager(new GithubSource(repository.ToString(), accessToken: null, prerelease: includeNightly));
        if (!current.IsInstalled)
        {
            return null;
        }

        var update = await current.CheckForUpdatesAsync();
        if (update is null)
        {
            return this.pending?.TargetFullRelease.Version.ToString();
        }

        if (this.pending?.TargetFullRelease.Version != update.TargetFullRelease.Version)
        {
            await current.DownloadUpdatesAsync(update, progress: null, cancelToken: cancellationToken);
            this.manager = current;
            this.pending = update;
            current.WaitExitThenApplyUpdates(update.TargetFullRelease, silent: true, restart: false);
        }

        return update.TargetFullRelease.Version.ToString();
    }

    public void ApplyAndRestart()
    {
        if (this.manager is not null && this.pending is not null)
        {
            this.manager.ApplyUpdatesAndRestart(this.pending.TargetFullRelease);
        }
    }
}
