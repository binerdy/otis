// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.ObjectModel;
using System.Text.Json;
using Otis.App.Services;
using Otis.Core.Projects;
using Otis.Infrastructure.Storage;

namespace Otis.App.ViewModels;

public sealed partial class ShellViewModel(AppServices services) : ObservableObject
{
    public AppServices Services => services;

    public ObservableCollection<RecentProjectViewModel> RecentProjects { get; } = [];

    [ObservableProperty]
    public partial EditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;

    public bool IsStartVisible => this.Editor is null;

    public bool IsEditorVisible => this.Editor is not null;

    public bool HasRecentProjects => this.RecentProjects.Count > 0;

    public void RefreshRecent()
    {
        this.RecentProjects.Clear();
        foreach (var path in services.Settings.RecentProjects.Where(File.Exists))
        {
            this.RecentProjects.Add(new RecentProjectViewModel(path, this.OpenAsync));
        }

        this.OnPropertyChanged(nameof(this.HasRecentProjects));
    }

    [RelayCommand]
    public async Task OpenProjectAsync()
    {
        if (await FileDialogs.OpenProjectAsync() is { } path)
        {
            await this.OpenAsync(path);
        }
    }

    public async Task CreateAsync(string familyName)
    {
        var path = await FileDialogs.SaveAsync(string.Concat(familyName.Where(c => !Path.GetInvalidFileNameChars().Contains(c))), "Otis", ProjectStore.Extension);
        if (path is null)
        {
            return;
        }

        if (!path.EndsWith(ProjectStore.Extension, StringComparison.OrdinalIgnoreCase))
        {
            path = Path.ChangeExtension(path, null) + ProjectStore.Extension;
        }

        services.Workspace.Projects.Save(path, OtisProject.Create(familyName.Trim()));
        await this.OpenAsync(path);
        if (this.Editor is not null)
        {
            await this.Editor.AddSourcesAsync();
        }
    }

    public async Task OpenAsync(string path)
    {
        try
        {
            var loaded = ProjectStore.Load(path);
            this.Editor?.Dispose();
            this.Editor = new EditorViewModel(services.Workspace, path, loaded);
            services.SaveSettings(services.Settings.Opened(path));
            this.Message = loaded.IsNewerFormat ? Strings.Get("NewerFormatHint") : string.Empty;
            await this.Editor.LoadAsync();
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            this.Message = Strings.Format("OpenFailedFormat", Path.GetFileName(path), e.Message);
        }
    }

    [RelayCommand]
    public void Close()
    {
        this.Editor?.Dispose();
        this.Editor = null;
        this.RefreshRecent();
    }

    partial void OnEditorChanged(EditorViewModel? value)
    {
        this.OnPropertyChanged(nameof(this.IsStartVisible));
        this.OnPropertyChanged(nameof(this.IsEditorVisible));
    }
}
