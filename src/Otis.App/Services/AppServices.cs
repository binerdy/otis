// SPDX-License-Identifier: GPL-3.0-or-later
using Microsoft.UI.Dispatching;
using Otis.Infrastructure;
using Otis.Infrastructure.Storage;
using Otis.Infrastructure.Updates;

namespace Otis.App.Services;

public sealed partial class AppServices : ObservableObject
{
    public static readonly Uri Repository = new("https://github.com/binerdy/otis", UriKind.Absolute);
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromHours(4);

    private readonly DispatcherQueue dispatcher;
    private DispatcherQueueTimer? updateTimer;

    public AppServices(DispatcherQueue dispatcher)
    {
        this.dispatcher = dispatcher;
        this.Workspace = OtisWorkspace.CreateDefault(TimeProvider.System);
        this.Settings = this.Workspace.Settings.Load();
        Strings.Initialize(this.Settings.Language);
    }

    public OtisWorkspace Workspace { get; }

    public VelopackUpdates Updates { get; } = new(Repository);

    public AppSettings Settings { get; private set; }

    [ObservableProperty]
    public partial string UpdateMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsUpdateReady { get; set; }

    public void SaveSettings(AppSettings settings)
    {
        this.Settings = settings;
        this.Workspace.Settings.Save(settings);
        Strings.Initialize(settings.Language);
    }

    public void StartUpdateChecks()
    {
        this.updateTimer = this.dispatcher.CreateTimer();
        this.updateTimer.Interval = UpdateInterval;
        this.updateTimer.Tick += async (_, _) => await this.CheckForUpdatesAsync();
        this.updateTimer.Start();
        _ = this.CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            var version = await this.Updates.PrepareAsync(this.Settings.NightlyUpdates, CancellationToken.None);
            this.dispatcher.TryEnqueue(() =>
            {
                this.IsUpdateReady = version is not null;
                this.UpdateMessage = version is null ? string.Empty : Strings.Format("UpdateReadyFormat", version);
            });
        }
        catch (HttpRequestException)
        {
            // Offline is normal; the next check runs in four hours.
        }
        catch (InvalidOperationException)
        {
            // Not installed through Velopack, for example when running from the IDE.
        }
    }
}
