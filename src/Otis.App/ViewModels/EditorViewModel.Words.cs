// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.App.Controls;
using Otis.App.Services;
using Otis.Core.Finding;
using Otis.Core.Glyphs;
using Otis.Core.Projects;
using Otis.Infrastructure.Recognition;

namespace Otis.App.ViewModels;

public sealed partial class EditorViewModel
{
    private CancellationTokenSource? reading;

    public static bool ReadingNoticeAccepted { get; set; }

    [ObservableProperty]
    public partial bool IsReading { get; set; }

    public bool HasApiKey => this.workspace.ApiKeys.HasKey;

    [RelayCommand]
    public void StartWord() => this.RequestedMode = this.IsReadOnly ? CanvasMode.Edit : CanvasMode.DrawWord;

    public async Task TypeWordAsync(PixelRect box, string text)
    {
        this.RequestedMode = CanvasMode.Edit;
        if (this.CurrentPage is not { } page || string.IsNullOrWhiteSpace(text) || this.IsReadOnly)
        {
            return;
        }

        var image = await this.workspace.PagesFor(this.ProjectPath, this.Project).PageAsync(page.Source, page.Page);
        var wanted = this.StillWanted();
        var words = new[] { new ReadWord(text.Trim(), box) };
        var best = await Task.Run(() => InitialPick.Best(WordPick.CandidatesOn(image, page.Source, page.Page, words, wanted)), CancellationToken.None);
        var added = this.AddDrafts(best);
        var skipped = text.Select(c => InitialPick.GlyphOf(c.ToString()))
            .OfType<string>()
            .Where(g => this.cells.ContainsKey(g) && !added.Contains(g))
            .Distinct(StringComparer.Ordinal)
            .Select(GlyphNames.Display)
            .ToList();
        var parts = new List<string>();
        if (added.Count > 0)
        {
            parts.Add(Strings.Format("WordAddedFormat", Displays(added)));
        }

        if (skipped.Count > 0)
        {
            parts.Add(Strings.Format("WordSkippedFormat", string.Join(", ", skipped)));
        }

        this.Status = added.Count == 0 && skipped.Count > 0 ? Strings.Get("WordNothingNew") : string.Join(" ", parts);
    }

    public async Task ReadWithClaudeAsync(bool allPages)
    {
        if (this.workspace.ApiKeys.Load() is not { } key)
        {
            this.Status = Strings.Get("ApiKeyMissing");
            return;
        }

        this.CancelReading();
        this.reading = new CancellationTokenSource();
        var token = this.reading.Token;
        this.IsReading = true;
        var pages = allPages ? this.Pages.ToList() : this.Pages.Where(p => p == this.CurrentPage).ToList();
        var source = this.workspace.PagesFor(this.ProjectPath, this.Project);
        var reader = new ClaudeWordReader(key);
        var wanted = this.StillWanted();
        var candidates = new List<PickCandidate>();
        var wordCount = 0;
        try
        {
            for (var i = 0; i < pages.Count; i++)
            {
                this.Status = Strings.Format("ReadingPageFormat", i + 1, pages.Count);
                var page = pages[i];
                var image = await source.PageAsync(page.Source, page.Page);
                var words = await Task.Run(() => reader.ReadAsync(image, token), token);
                wordCount += words.Count;
                candidates.AddRange(await Task.Run(() => WordPick.CandidatesOn(image, page.Source, page.Page, words, wanted).ToList(), token));
            }

            var added = this.AddDrafts(InitialPick.Best(candidates));
            this.Status = added.Count == 0
                ? Strings.Format("ReadNothingNewFormat", wordCount)
                : Strings.Format("ReadAddedFormat", wordCount, Displays(added));
        }
        catch (OperationCanceledException)
        {
            this.Status = Strings.Get("ReadingCancelled");
        }
        catch (WordReadingException e)
        {
            this.Status = e.Problem == WordReadingProblem.Other
                ? Strings.Format("ReadProblemOther", e.Message)
                : Strings.Get("ReadProblem" + e.Problem);
            CrashLog.Write(e);
        }
        finally
        {
            this.IsReading = false;
        }
    }

    [RelayCommand]
    public void CancelReading()
    {
        this.reading?.Cancel();
        this.reading?.Dispose();
        this.reading = null;
    }

    private static string Displays(IEnumerable<string> glyphs) => string.Join(", ", glyphs.Select(GlyphNames.Display));

    private HashSet<string> StillWanted()
    {
        var wanted = InitialPick.Wanted(this.Project).ToHashSet(StringComparer.Ordinal);
        wanted.ExceptWith(this.drafts.Where(d => d.Value is not null).Select(d => d.Key));
        wanted.IntersectWith(this.cells.Keys);
        return wanted;
    }

    private List<string> AddDrafts(IReadOnlyDictionary<string, PickCandidate> best)
    {
        var added = new List<string>();
        foreach (var (glyph, candidate) in best.OrderBy(b => b.Key, StringComparer.Ordinal))
        {
            this.drafts[glyph] = candidate.Cutout;
            this.MarkDirty(glyph);
            added.Add(glyph);
        }

        if (added.Count > 0)
        {
            this.CutoutsChanged?.Invoke(this, new(false));
            this.SchedulePreview();
        }

        return added;
    }
}
