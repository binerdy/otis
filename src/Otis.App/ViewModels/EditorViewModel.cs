// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.ObjectModel;
using System.Diagnostics;
using Otis.App.Controls;
using Otis.App.Services;
using Otis.Core.Building;
using Otis.Core.Glyphs;
using Otis.Core.Ink;
using Otis.Core.Projects;
using Otis.Infrastructure;
using Otis.Infrastructure.Fonts;
using Otis.Infrastructure.Pages;
using Otis.Infrastructure.Storage;
using SkiaSharp;

namespace Otis.App.ViewModels;

public sealed partial class EditorViewModel : ObservableObject, IDisposable
{
    private static readonly TimeSpan PreviewDelay = TimeSpan.FromMilliseconds(300);

    private readonly OtisWorkspace workspace;
    private readonly Dictionary<string, Cutout?> drafts = [with(StringComparer.Ordinal)];
    private readonly Dictionary<string, CharacterCellViewModel> cells = [with(StringComparer.Ordinal)];
    private CancellationTokenSource? previewDelay;
    private CutoutAdjustments pending = CutoutAdjustments.None;
    private List<bool> strokeOrder = [];

    public EditorViewModel(OtisWorkspace workspace, string projectPath, LoadedProject loaded)
    {
        this.workspace = workspace;
        this.ProjectPath = projectPath;
        this.Project = loaded.Project;
        this.IsReadOnly = loaded.IsNewerFormat;
        this.SampleText = this.Project.Font.SampleText;
        foreach (var group in CharacterSet.Groups)
        {
            var groupViewModel = new CharacterGroupViewModel(Strings.Get("Group" + group.Kind));
            foreach (var glyph in group.Glyphs)
            {
                var cell = new CharacterCellViewModel(glyph, c => this.Select(c.Glyph));
                this.cells[glyph] = cell;
                groupViewModel.Cells.Add(cell);
            }

            this.Groups.Add(groupViewModel);
        }

        this.RefreshCells();
        this.RefreshBuildPane();
    }

    public event EventHandler? PageImageChanged;

    public event EventHandler<CanvasEventArgs<bool>>? CutoutsChanged;

    public event EventHandler? StrokesChanged;

    public string ProjectPath { get; }

    public OtisProject Project { get; private set; }

    public bool IsReadOnly { get; }

    public string Title => $"{this.Project.Font.FamilyName} — {Path.GetFileName(this.ProjectPath)}";

    public ObservableCollection<CharacterGroupViewModel> Groups { get; } = [];

    public ObservableCollection<SourcePageViewModel> Pages { get; } = [];

    public ObservableCollection<BuildRecordViewModel> History { get; } = [];

    public SKImage? PageImage { get; private set; }

    public string? SelectedGlyph { get; private set; }

    public bool HasUnsavedChanges => this.drafts.Count > 0;

    public bool HasSelection => this.SelectedGlyph is not null;

    public Cutout? SelectedCutout => this.SelectedGlyph is { } glyph ? this.CutoutOf(glyph) : null;

    [ObservableProperty]
    public partial SourcePageViewModel? CurrentPage { get; set; }

    [ObservableProperty]
    public partial CanvasMode RequestedMode { get; set; }

    [ObservableProperty]
    public partial string SelectionTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Status { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ImageSource? Preview { get; set; }

    [ObservableProperty]
    public partial ImageSource? Specimen { get; set; }

    [ObservableProperty]
    public partial string VersionText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RebuildText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool CanRebuild { get; set; }

    [ObservableProperty]
    public partial string InstalledText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BuildReport { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SampleText { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial double BrushWidth { get; set; } = 8;

    [ObservableProperty]
    public partial ImageSource? AppliedOutline { get; set; }

    [ObservableProperty]
    public partial ImageSource? AdjustedOutline { get; set; }

    public CutoutAdjustments Applied => this.SelectedCutout?.Adjustments ?? CutoutAdjustments.None;

    public bool CanAdjust => this.SelectedCutout is not null && !this.IsReadOnly;

    public bool HasPendingAdjustments => this.SelectedCutout is not null && this.pending != this.Applied;

    public string StrokeSummary => Strings.Format("StrokeCountFormat", this.pending.InkStrokes.Count, this.pending.EraseStrokes.Count);

    public bool PendingGreyTracing
    {
        get => this.pending.GreyTracing;
        set => this.SetPending(this.pending with { GreyTracing = value });
    }

    public int PendingSmoothing
    {
        get => (int)this.pending.Smoothing;
        set => this.SetPending(this.pending with { Smoothing = (SmoothingLevel)Math.Clamp(value, 0, 3) });
    }

    public double PendingCloseGaps
    {
        get => this.pending.CloseGaps;
        set => this.SetPending(this.pending with { CloseGaps = (int)Math.Round(value) });
    }

    public IReadOnlyList<BrushStroke> PendingStrokes() =>
        this.pending.InkStrokes.Select((s, i) => new BrushStroke(s, Erases: false, i))
            .Concat(this.pending.EraseStrokes.Select((s, i) => new BrushStroke(s, Erases: true, i)))
            .ToArray();

    public async Task LoadAsync()
    {
        await this.LoadPagesAsync();
        if (this.Pages.Count > 0)
        {
            this.CurrentPage = this.Pages[0];
        }

        await this.LoadThumbnailsAsync(this.Project.Cutouts.Keys.ToList());
    }

    public void Select(string glyph)
    {
        if (this.SelectedGlyph is { } previous && this.cells.TryGetValue(previous, out var previousCell))
        {
            previousCell.IsSelected = false;
        }

        this.SelectedGlyph = glyph;
        this.cells[glyph].IsSelected = true;
        var display = GlyphNames.Display(glyph);
        var derivation = this.Project.Derivations.Describe(glyph);
        this.SelectionTitle = derivation is null ? $"{display}  ({glyph})" : $"{display}  ({glyph})  {derivation}";
        this.OnPropertyChanged(nameof(this.HasSelection));
        this.ResetPending();

        if (this.CutoutOf(glyph) is { } cutout)
        {
            this.RequestedMode = CanvasMode.Edit;
            this.Status = string.Empty;
            var page = this.Pages.FirstOrDefault(p => p.Source == cutout.Source && p.Page == cutout.Page);
            if (page is not null && page != this.CurrentPage)
            {
                this.CurrentPage = page;
            }
            else
            {
                this.CutoutsChanged?.Invoke(this, new(true));
            }
        }
        else
        {
            this.RequestedMode = CanvasMode.DrawBox;
            this.Status = derivation is null ? Strings.Format("MissingHintFormat", display) : Strings.Format("DerivedHintFormat", display, derivation);
            this.CutoutsChanged?.Invoke(this, new(false));
        }

        this.SchedulePreview();
    }

    public IReadOnlyList<NeighbourCutout> Neighbours() =>
        this.CurrentPage is not { } page
            ? []
            : this.AllCutouts()
                .Where(c => c.Key != this.SelectedGlyph && c.Value.Source == page.Source && c.Value.Page == page.Page)
                .Select(c => new NeighbourCutout(c.Key, GlyphNames.Display(c.Key), c.Value))
                .ToArray();

    public void UpdateDraft(Cutout cutout)
    {
        if (this.SelectedGlyph is not { } glyph || this.IsReadOnly)
        {
            return;
        }

        var inSync = this.pending == this.Applied;
        this.drafts[glyph] = cutout;
        this.MarkDirty(glyph);
        if (inSync)
        {
            this.ResetPending();
        }

        this.SchedulePreview();
    }

    // A new box keeps the current guide lines when they cross it; otherwise it starts with lines the user then adjusts.
    public void CreateFromBox(PixelRect box)
    {
        if (this.SelectedGlyph is null || this.CurrentPage is not { } page)
        {
            return;
        }

        var previous = this.SelectedCutout;
        var keepLines = previous is not null && previous.Source == page.Source && previous.Page == page.Page
            && previous.Baseline > box.Top && previous.XLine < box.Bottom;
        var cutout = Cutout.Rectangle(
            page.Source,
            page.Page,
            box,
            keepLines ? previous!.Baseline : box.Top + (int)(box.Height * 0.8),
            keepLines ? previous!.XLine : box.Top + (int)(box.Height * 0.3));
        this.Status = keepLines ? string.Empty : Strings.Get("AdjustLinesHint");
        this.RequestedMode = CanvasMode.Edit;
        this.UpdateDraft(cutout);
        this.CutoutsChanged?.Invoke(this, new(false));
    }

    public void SetLines(int? baseline, int? xLine)
    {
        if (this.SelectedCutout is { } cutout)
        {
            this.UpdateDraft(cutout with { Baseline = baseline ?? cutout.Baseline, XLine = xLine ?? cutout.XLine });
            this.CutoutsChanged?.Invoke(this, new(false));
        }
    }

    public void Nudge(int dx, int dy)
    {
        if (this.SelectedCutout is { } cutout)
        {
            this.UpdateDraft(cutout.MoveBy(dx, dy));
            this.CutoutsChanged?.Invoke(this, new(false));
        }
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (!this.HasUnsavedChanges && this.SampleText == this.Project.Font.SampleText)
        {
            return;
        }

        var project = this.Project with { Font = this.Project.Font with { SampleText = this.SampleText } };
        foreach (var (glyph, cutout) in this.drafts)
        {
            project = project.WithCutout(glyph, cutout);
        }

        this.workspace.Projects.Save(this.ProjectPath, project);
        this.Project = project;
        var changed = this.drafts.Keys.ToList();
        this.drafts.Clear();
        this.RefreshCells();
        this.Status = Strings.Get("SavedHint");
        this.OnPropertyChanged(nameof(this.HasUnsavedChanges));
        await this.LoadThumbnailsAsync(changed.Where(this.Project.Cutouts.ContainsKey).ToList());
    }

    [RelayCommand]
    public void Revert()
    {
        if (this.SelectedGlyph is { } glyph && this.drafts.Remove(glyph))
        {
            this.cells[glyph].IsDirty = false;
            this.OnPropertyChanged(nameof(this.HasUnsavedChanges));
            this.CutoutsChanged?.Invoke(this, new(false));
            this.SchedulePreview();
        }
    }

    public void Remove()
    {
        if (this.SelectedGlyph is { } glyph)
        {
            this.drafts[glyph] = null;
            this.MarkDirty(glyph);
            this.CutoutsChanged?.Invoke(this, new(false));
            this.SchedulePreview();
        }
    }

    public void AddStroke(BrushStroke brush)
    {
        this.strokeOrder.Add(brush.Erases);
        this.SetPending(brush.Erases
            ? this.pending with { EraseStrokes = this.pending.EraseStrokes.Append(brush.Stroke).ToArray() }
            : this.pending with { InkStrokes = this.pending.InkStrokes.Append(brush.Stroke).ToArray() });
    }

    public void RemoveStroke(BrushStroke brush)
    {
        var position = this.strokeOrder.Select((erases, i) => (erases, i)).Where(o => o.erases == brush.Erases).Select(o => o.i).ElementAtOrDefault(brush.Index);
        if (position < this.strokeOrder.Count)
        {
            this.strokeOrder.RemoveAt(position);
        }

        this.SetPending(brush.Erases
            ? this.pending with { EraseStrokes = this.pending.EraseStrokes.Where((_, i) => i != brush.Index).ToArray() }
            : this.pending with { InkStrokes = this.pending.InkStrokes.Where((_, i) => i != brush.Index).ToArray() });
    }

    [RelayCommand]
    public void UndoGreyTracing() => this.SetPending(this.pending with { GreyTracing = this.Applied.GreyTracing });

    [RelayCommand]
    public void UndoSmoothing() => this.SetPending(this.pending with { Smoothing = this.Applied.Smoothing });

    [RelayCommand]
    public void UndoRepair()
    {
        var applied = this.Applied;
        this.strokeOrder = StrokeOrderOf(applied);
        this.SetPending(this.pending with { CloseGaps = applied.CloseGaps, InkStrokes = applied.InkStrokes, EraseStrokes = applied.EraseStrokes });
    }

    [RelayCommand]
    public void UndoLastStroke()
    {
        if (this.strokeOrder.Count == 0)
        {
            return;
        }

        var erases = this.strokeOrder[^1];
        var count = erases ? this.pending.EraseStrokes.Count : this.pending.InkStrokes.Count;
        if (count > 0)
        {
            this.RemoveStroke(new BrushStroke(new InkStroke(0, []), erases, count - 1));
        }
        else
        {
            this.strokeOrder.RemoveAt(this.strokeOrder.Count - 1);
        }
    }

    [RelayCommand]
    public void ApplyAdjustments()
    {
        if (this.SelectedCutout is { } cutout && this.HasPendingAdjustments)
        {
            var adjusted = cutout.WithAdjustments(this.pending);
            this.drafts[this.SelectedGlyph!] = adjusted;
            this.MarkDirty(this.SelectedGlyph!);
            this.ResetPending();
            this.SchedulePreview();
            this.CutoutsChanged?.Invoke(this, new(false));
        }
    }

    [RelayCommand]
    public void DiscardAdjustments()
    {
        this.ResetPending();
        this.SchedulePreview();
    }

    [RelayCommand]
    public void StartInkBrush() => this.RequestedMode = this.CanAdjust ? CanvasMode.DrawInk : CanvasMode.Edit;

    [RelayCommand]
    public void StartEraser() => this.RequestedMode = this.CanAdjust ? CanvasMode.DrawEraser : CanvasMode.Edit;

    [RelayCommand]
    public void StartBox() => this.RequestedMode = CanvasMode.DrawBox;

    [RelayCommand]
    public void StartErasure() => this.RequestedMode = this.SelectedCutout is null ? CanvasMode.Edit : CanvasMode.DrawErasure;

    [RelayCommand]
    public void NextPage() => this.MovePage(1);

    [RelayCommand]
    public void PreviousPage() => this.MovePage(-1);

    [RelayCommand]
    public async Task AddSourcesAsync()
    {
        var files = await FileDialogs.OpenSourcesAsync();
        var added = files.Select(f => ProjectStore.RelativeSource(this.ProjectPath, f)).Where(s => !this.Project.Sources.Contains(s)).ToList();
        if (added.Count == 0)
        {
            return;
        }

        this.Project = this.Project with { Sources = this.Project.Sources.Concat(added).ToArray() };
        this.workspace.Projects.Save(this.ProjectPath, this.Project);
        await this.LoadPagesAsync();
        this.CurrentPage ??= this.Pages.FirstOrDefault();
    }

    [RelayCommand]
    public async Task ExportPagesAsync()
    {
        var target = await FileDialogs.SaveAsync(this.Project.Font.FamilyName + " pages", "PDF", ".pdf");
        if (target is null)
        {
            return;
        }

        await this.RunAsync(async () =>
        {
            var sources = this.Project.Sources.Select(s => ProjectStore.SourcePath(this.ProjectPath, s)).Where(File.Exists).ToList();
            await PagesPdf.ExportAsync(sources, this.Project.PageResolution, this.workspace.Pages, target);
            this.Status = Strings.Format("ExportedFormat", target);
        });
    }

    public Task BuildAsync(bool rebuild) => this.RunAsync(async () =>
    {
        var version = rebuild && VersionPolicy.Rebuildable(this.Project) is { } latest ? latest : ParseVersion(this.VersionText);
        var outcome = await this.workspace.BuildAsync(this.ProjectPath, this.Project, version);
        this.Project = outcome.Project;
        var missing = string.Join(" ", outcome.Build.Coverage.MissingGlyphs.Select(GlyphNames.Display));
        var problems = string.Join(Environment.NewLine, outcome.Build.Problems);
        this.BuildReport = Strings.Format("BuildReportFormat", outcome.Record.FileName, missing) + (problems.Length > 0 ? Environment.NewLine + problems : string.Empty);
        this.Specimen = await Images.FromFileAsync(outcome.SpecimenPath);
        this.RefreshBuildPane();
    });

    public FontInstallation Installed() => this.workspace.Installed(this.Project);

    public Task InstallAsync(BuildRecord build) => this.RunAsync(() =>
    {
        this.Project = this.workspace.Install(this.ProjectPath, this.Project, build);
        this.Status = Strings.Get("RestartWordHint");
        this.RefreshBuildPane();
        return Task.CompletedTask;
    });

    [RelayCommand]
    public Task UninstallAsync() => this.RunAsync(() =>
    {
        this.Project = this.workspace.Uninstall(this.ProjectPath, this.Project);
        this.RefreshBuildPane();
        return Task.CompletedTask;
    });

    [RelayCommand]
    public void OpenOutputFolder()
    {
        var folder = FontOutput.Folder(this.ProjectPath, this.Project);
        Directory.CreateDirectory(folder);
        using var process = Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
    }

    public void Dispose()
    {
        this.previewDelay?.Cancel();
        this.previewDelay?.Dispose();
        this.PageImage?.Dispose();
    }

    private static List<bool> StrokeOrderOf(CutoutAdjustments adjustments) =>
        [.. Enumerable.Repeat(false, adjustments.InkStrokes.Count), .. Enumerable.Repeat(true, adjustments.EraseStrokes.Count)];

    private static FontVersion ParseVersion(string text) =>
        FontVersion.TryParse(text, out var version) ? version : throw new InvalidOperationException(Strings.Format("VersionFormatHint", text));

    private Dictionary<string, Cutout> AllCutouts()
    {
        var all = new Dictionary<string, Cutout>(this.Project.Cutouts, StringComparer.Ordinal);
        foreach (var (glyph, cutout) in this.drafts)
        {
            if (cutout is null)
            {
                all.Remove(glyph);
            }
            else
            {
                all[glyph] = cutout;
            }
        }

        return all;
    }

    private Cutout? CutoutOf(string glyph) =>
        this.drafts.TryGetValue(glyph, out var draft) ? draft : this.Project.Cutouts.GetValueOrDefault(glyph);

    private void MarkDirty(string glyph)
    {
        this.cells[glyph].IsDirty = true;
        this.OnPropertyChanged(nameof(this.HasUnsavedChanges));
    }

    private void RefreshCells()
    {
        var coverage = Coverage.Of(this.Project);
        foreach (var (glyph, cell) in this.cells)
        {
            var hasCutout = this.Project.Cutouts.ContainsKey(glyph);
            var blockState = hasCutout ? CoverageState.CutOut : CoverageState.Missing;
            cell.State = CharacterSet.IsBuildingBlock(glyph) ? blockState : coverage[glyph];
            cell.Tooltip = $"{cell.Display}  ({glyph})  {this.Project.Derivations.Describe(glyph)}".TrimEnd();
            cell.IsDirty = this.drafts.ContainsKey(glyph);
            if (!this.Project.Cutouts.ContainsKey(glyph))
            {
                cell.Thumbnail = null;
            }
        }
    }

    private void RefreshBuildPane()
    {
        this.VersionText = VersionPolicy.ProposeNext(this.Project).ToString();
        var rebuildable = VersionPolicy.Rebuildable(this.Project);
        this.CanRebuild = rebuildable is not null;
        this.RebuildText = rebuildable is null ? string.Empty : Strings.Format("RebuildFormat", rebuildable);
        this.History.Clear();
        foreach (var record in this.Project.Builds.OrderByDescending(b => b.Version))
        {
            this.History.Add(new BuildRecordViewModel(record, this.InstallAsync));
        }

        var installed = this.Installed();
        this.InstalledText = installed.Scope switch
        {
            InstallScope.None => Strings.Get("NotInstalled"),
            InstallScope.AllUsers => Strings.Get("InstalledForAllUsers"),
            _ => installed.Version is { } v ? Strings.Format("InstalledFormat", v) : Strings.Get("InstalledUnknownVersion"),
        };
    }

    private async Task LoadPagesAsync()
    {
        this.Pages.Clear();
        foreach (var source in this.Project.Sources)
        {
            var path = ProjectStore.SourcePath(this.ProjectPath, source);
            if (!File.Exists(path))
            {
                this.Status = Strings.Format("MissingSourceFormat", source);
                continue;
            }

            var count = await PageCache.PageCountAsync(path);
            for (var page = 1; page <= count; page++)
            {
                this.Pages.Add(new SourcePageViewModel(source, page, count == 1 ? source : $"{source} · {page}"));
            }
        }
    }

    private void MovePage(int step)
    {
        if (this.CurrentPage is { } page && this.Pages.IndexOf(page) is var index and >= 0)
        {
            this.CurrentPage = this.Pages[Math.Clamp(index + step, 0, this.Pages.Count - 1)];
        }
    }

    partial void OnCurrentPageChanged(SourcePageViewModel? value) => _ = this.ShowPageAsync(value);

    private async Task ShowPageAsync(SourcePageViewModel? page)
    {
        if (page is null)
        {
            return;
        }

        this.Status = Strings.Get("LoadingPage");
        var png = await this.workspace.Pages.EnsureAsync(ProjectStore.SourcePath(this.ProjectPath, page.Source), page.Page, this.Project.PageResolution);
        var image = await Task.Run(() => SKImage.FromEncodedData(png), CancellationToken.None);
        if (this.CurrentPage != page)
        {
            image.Dispose();
            return;
        }

        var previous = this.PageImage;
        this.PageImage = image;
        this.Status = string.Empty;
        this.PageImageChanged?.Invoke(this, EventArgs.Empty);
        previous?.Dispose();
        this.CutoutsChanged?.Invoke(this, new(true));
    }

    private void ResetPending()
    {
        var applied = this.Applied;
        this.strokeOrder = StrokeOrderOf(applied);
        this.SetPending(applied, preview: false);
    }

    private void SetPending(CutoutAdjustments adjustments, bool preview = true)
    {
        this.pending = adjustments with { CloseGaps = Math.Clamp(adjustments.CloseGaps, 0, CutoutAdjustments.MaxCloseGaps) };
        string[] changed =
        [
            nameof(this.PendingGreyTracing), nameof(this.PendingSmoothing), nameof(this.PendingCloseGaps),
            nameof(this.StrokeSummary), nameof(this.HasPendingAdjustments), nameof(this.CanAdjust),
        ];
        foreach (var name in changed)
        {
            this.OnPropertyChanged(name);
        }

        this.StrokesChanged?.Invoke(this, EventArgs.Empty);
        if (preview)
        {
            this.SchedulePreview();
        }
    }

    private void SchedulePreview()
    {
        this.previewDelay?.Cancel();
        this.previewDelay?.Dispose();
        this.previewDelay = new CancellationTokenSource();
        _ = this.PreviewAfterDelayAsync(this.previewDelay.Token);
    }

    private async Task PreviewAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(PreviewDelay, cancellationToken);
            if (this.SelectedCutout is not { } cutout)
            {
                this.Preview = this.AppliedOutline = this.AdjustedOutline = null;
                return;
            }

            var adjusted = cutout.WithAdjustments(this.pending);
            var pages = this.workspace.PagesFor(this.ProjectPath, this.Project);
            var (inkPng, appliedPng, adjustedPng) = await Task.Run(
                async () =>
                {
                    var page = await pages.PageAsync(cutout.Source, cutout.Page);
                    var ink = CutoutExtractor.Extract(page, cutout);
                    var applied = Images.OutlinePng(CutoutTracer.Trace(page, cutout), cutout);
                    var changed = adjusted.Adjustments == cutout.Adjustments ? applied : Images.OutlinePng(CutoutTracer.Trace(page, adjusted), adjusted);
                    return (Images.InkPng(ink.Mask, cutout.Baseline - ink.Top, cutout.XLine - ink.Top), applied, changed);
                },
                cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
            {
                this.Preview = await Images.FromPngAsync(inkPng);
                this.AppliedOutline = await Images.FromPngAsync(appliedPng);
                this.AdjustedOutline = await Images.FromPngAsync(adjustedPng);
            }
        }
        catch (OperationCanceledException)
        {
            // A newer change replaced this preview.
        }
        catch (FileNotFoundException e)
        {
            this.Status = e.Message;
        }
    }

    private async Task LoadThumbnailsAsync(IReadOnlyList<string> glyphs)
    {
        var pages = this.workspace.PagesFor(this.ProjectPath, this.Project);
        foreach (var glyph in glyphs.OrderBy(g => (this.Project.Cutouts[g].Source, this.Project.Cutouts[g].Page)))
        {
            var cutout = this.Project.Cutouts[glyph];
            try
            {
                var png = await Task.Run(
                    async () =>
                    {
                        var page = await pages.PageAsync(cutout.Source, cutout.Page);
                        return Images.InkPng(CutoutExtractor.Extract(page, cutout).Mask);
                    },
                    CancellationToken.None);
                this.cells[glyph].Thumbnail = await Images.FromPngAsync(png);
            }
            catch (FileNotFoundException)
            {
                this.cells[glyph].Tooltip = $"{this.cells[glyph].Display}  ({glyph})  {Strings.Get("SourceUnavailable")}";
            }
        }
    }

    private async Task RunAsync(Func<Task> work)
    {
        this.IsBusy = true;
        try
        {
            await work();
        }
        catch (InvalidOperationException e)
        {
            this.Status = e.Message;
        }
        finally
        {
            this.IsBusy = false;
        }
    }
}
