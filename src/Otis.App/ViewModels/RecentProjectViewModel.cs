// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.App.ViewModels;

public sealed partial class RecentProjectViewModel(string path, Func<string, Task> open) : ObservableObject
{
    public string Path { get; } = path;

    public string Name { get; } = System.IO.Path.GetFileName(path);

    public string Folder { get; } = System.IO.Path.GetDirectoryName(path) ?? string.Empty;

    [RelayCommand]
    public Task Open() => open(this.Path);
}
