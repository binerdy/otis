// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.App.Services;
using Otis.Core.Projects;

namespace Otis.App.ViewModels;

public sealed partial class BuildRecordViewModel(BuildRecord record, Func<BuildRecord, Task> install) : ObservableObject
{
    public BuildRecord Record { get; } = record;

    public string Version => this.Record.Version.ToString();

    public string Built => this.Record.BuiltAt.ToLocalTime().ToString("g", System.Globalization.CultureInfo.CurrentCulture);

    public string Coverage => Strings.Format("CoverageFormat", this.Record.CutOut, this.Record.Derived, this.Record.Missing);

    public bool IsInstalled => this.Record.Installed;

    [RelayCommand]
    public Task Install() => install(this.Record);
}
