// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.ObjectModel;

namespace Otis.App.ViewModels;

public sealed class CharacterGroupViewModel(string title)
{
    public string Title { get; } = title;

    public ObservableCollection<CharacterCellViewModel> Cells { get; } = [];
}
