// SPDX-License-Identifier: GPL-3.0-or-later
namespace Otis.App.ViewModels;

public sealed record SourcePageViewModel(string Source, int Page, string Label)
{
    public override string ToString() => this.Label;
}
