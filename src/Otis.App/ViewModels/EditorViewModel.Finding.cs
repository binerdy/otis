// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Otis.App.Services;
using Otis.Core.Finding;
using Otis.Core.Projects;
using Otis.Infrastructure.Storage;

namespace Otis.App.ViewModels;

public sealed partial class EditorViewModel
{
    private const int ParallelPages = 4;
    private readonly ConcurrentDictionary<(string Source, int Page), PageInkMap> inkMaps = new();
    private CancellationTokenSource? search;

    public event EventHandler<Controls.CanvasEventArgs<PixelRect>>? ZoomRequested;

    public ObservableCollection<MatchViewModel> Matches { get; } = [];

    [ObservableProperty]
    public partial string SearchStatus { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSearching { get; set; }

    [ObservableProperty]
    public partial MatchViewModel? ShownMatch { get; set; }

    [ObservableProperty]
    public partial string SelectedQuality { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedQualityTooltip { get; set; } = string.Empty;

    [RelayCommand]
    public async Task FindSimilarAsync()
    {
        if (this.SelectedCutout is not { } reference)
        {
            return;
        }

        this.CancelSearch();
        this.search = new CancellationTokenSource();
        var token = this.search.Token;
        this.Matches.Clear();
        this.ShownMatch = null;
        this.IsSearching = true;
        var pages = this.workspace.PagesFor(this.ProjectPath, this.Project);
        try
        {
            var template = await Task.Run(async () => SimilarFinder.TemplateOf(await pages.PageAsync(reference.Source, reference.Page), reference), token);
            var found = new ConcurrentBag<SimilarMatch>();
            var done = 0;
            var progress = new Progress<int>(n => this.SearchStatus = Strings.Format("SearchProgressFormat", n, this.Pages.Count));
            var all = this.Pages.ToList();
            await Parallel.ForEachAsync(all, new ParallelOptions { MaxDegreeOfParallelism = ParallelPages, CancellationToken = token }, async (page, ct) =>
            {
                var map = await this.InkMapAsync(page.Source, page.Page, ct);
                foreach (var match in SimilarFinder.SearchPage(template, page.Source, page.Page, map))
                {
                    found.Add(match);
                }

                ((IProgress<int>)progress).Report(Interlocked.Increment(ref done));
            });

            foreach (var match in SimilarFinder.Best(found, template))
            {
                var page = await pages.PageAsync(match.Source, match.Page);
                var quality = await Task.Run(() => QualityScore.Of(page, match.ToCutout()), token);
                var label = this.Pages.FirstOrDefault(p => p.Source == match.Source && p.Page == match.Page)?.Label ?? match.Source;
                var item = new MatchViewModel(match, quality, label, this.ShowMatch)
                {
                    Thumbnail = await Images.FromPngAsync(Images.CropPng(page, match.Box)),
                };
                this.Matches.Add(item);
            }

            this.SearchStatus = this.Matches.Count == 0 ? Strings.Get("NoMatches") : Strings.Format("MatchesFormat", this.Matches.Count);
        }
        catch (OperationCanceledException)
        {
            this.SearchStatus = Strings.Get("SearchCancelled");
        }
        finally
        {
            this.IsSearching = false;
        }
    }

    [RelayCommand]
    public void CancelSearch()
    {
        this.search?.Cancel();
        this.search?.Dispose();
        this.search = null;
    }

    [RelayCommand]
    public void UseMatch()
    {
        if (this.ShownMatch is not { } shown || this.SelectedGlyph is null)
        {
            return;
        }

        this.UpdateDraft(shown.Match.ToCutout());
        this.ResetPending();
        this.ShownMatch = null;
        this.CutoutsChanged?.Invoke(this, new(true));
    }

    [RelayCommand]
    public async Task DetectLinesAsync()
    {
        if (this.SelectedCutout is not { } cutout)
        {
            return;
        }

        var page = await this.workspace.PagesFor(this.ProjectPath, this.Project).PageAsync(cutout.Source, cutout.Page);
        if (await Task.Run(() => GuideLineFinder.Around(page, cutout.Bounds), CancellationToken.None) is { } lines)
        {
            this.SetLines(lines.Baseline, lines.XLine);
            this.Status = string.Empty;
        }
        else
        {
            this.Status = Strings.Get("NoLinesFound");
        }
    }

    private void ShowMatch(MatchViewModel match)
    {
        foreach (var other in this.Matches)
        {
            other.IsShown = other == match;
        }

        this.ShownMatch = match;
        var page = this.Pages.FirstOrDefault(p => p.Source == match.Match.Source && p.Page == match.Match.Page);
        if (page is not null && page != this.CurrentPage)
        {
            this.CurrentPage = page;
        }

        this.CutoutsChanged?.Invoke(this, new(false));
        this.ZoomRequested?.Invoke(this, new(match.Match.Box));
    }

    private async Task<PageInkMap> InkMapAsync(string source, int page, CancellationToken token)
    {
        if (this.inkMaps.TryGetValue((source, page), out var cached))
        {
            return cached;
        }

        var png = await this.workspace.Pages.EnsureAsync(ProjectStore.SourcePath(this.ProjectPath, source), page, this.Project.PageResolution);
        token.ThrowIfCancellationRequested();
        var map = await Task.Run(() => PageInkMap.Of(Images.DecodeRgb(png)), token);
        this.inkMaps[(source, page)] = map;
        return map;
    }
}
