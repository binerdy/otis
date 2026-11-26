// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.Core.Glyphs;

namespace Otis.App.ViewModels;

public sealed partial class CharacterCellViewModel(string glyph, Action<CharacterCellViewModel> selected) : ObservableObject
{
    public string Glyph { get; } = glyph;

    public string Display { get; } = GlyphNames.Display(glyph);

    [ObservableProperty]
    public partial CoverageState State { get; set; }

    [ObservableProperty]
    public partial string Tooltip { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ImageSource? Thumbnail { get; set; }

    [ObservableProperty]
    public partial bool IsDirty { get; set; }

    [ObservableProperty]
    public partial string QualityText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public bool HasThumbnail => this.Thumbnail is not null;

    [RelayCommand]
    public void Select() => selected(this);

    partial void OnThumbnailChanged(ImageSource? value) => this.OnPropertyChanged(nameof(this.HasThumbnail));
}
