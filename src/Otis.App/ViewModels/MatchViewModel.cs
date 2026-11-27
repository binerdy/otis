// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;
using Otis.App.Services;
using Otis.Core.Finding;

namespace Otis.App.ViewModels;

public sealed partial class MatchViewModel(SimilarMatch match, Quality quality, string pageLabel, Action<MatchViewModel> show) : ObservableObject
{
    public SimilarMatch Match { get; } = match;

    public Quality Quality { get; } = quality;

    public string Label { get; } = string.Create(CultureInfo.CurrentCulture, $"{pageLabel} · {Math.Round(match.Similarity * 100)} %");

    public string QualityText => Strings.Format("QualityFormat", this.Quality.Score);

    public string Tooltip => QualityTexts.Describe(this.Quality);

    [ObservableProperty]
    public partial ImageSource? Thumbnail { get; set; }

    [ObservableProperty]
    public partial bool IsShown { get; set; }

    [RelayCommand]
    public void Show() => show(this);
}
