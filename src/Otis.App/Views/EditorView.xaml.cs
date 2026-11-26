// SPDX-License-Identifier: GPL-3.0-or-later
using System.ComponentModel;
using Otis.App.Controls;
using Otis.App.Services;
using Otis.App.ViewModels;
using Otis.Core.Glyphs;
using Otis.Infrastructure.Fonts;
using Windows.System;

namespace Otis.App.Views;

public sealed partial class EditorView : UserControl
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel), typeof(EditorViewModel), typeof(EditorView), new PropertyMetadata(null, OnViewModelChanged));

    private bool updatingLines;

    public EditorView()
    {
        this.InitializeComponent();
        this.Canvas.CutoutChanged += (_, e) =>
        {
            this.ViewModel?.UpdateDraft(e.Value);
            this.ShowLines();
        };
        this.Canvas.BoxDrawn += (_, e) => this.ViewModel?.CreateFromBox(e.Value);
        this.Canvas.NeighbourSelected += (_, e) => this.ViewModel?.Select(e.Value);
    }

    public EditorViewModel? ViewModel
    {
        get => (EditorViewModel?)this.GetValue(ViewModelProperty);
        set => this.SetValue(ViewModelProperty, value);
    }

    public static Brush StateBrush(CoverageState state) => Resource(state switch
    {
        CoverageState.CutOut => "CutOutBrush",
        CoverageState.Derived => "DerivedBrush",
        _ => "MissingBrush",
    });

    public static Brush CellBorder(bool selected) => Resource(selected ? "AccentBrush" : "PanelBorderBrush");

    public static Visibility Hidden(bool value) => value ? Visibility.Collapsed : Visibility.Visible;

    public static Visibility Shown(string? text) => string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;

    public static bool Not(bool value) => !value;

    private static Brush Resource(string key) => (Brush)Application.Current.Resources[key];

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (EditorView)d;
        if (e.OldValue is EditorViewModel old)
        {
            old.PageImageChanged -= view.OnPageImageChanged;
            old.CutoutsChanged -= view.OnCutoutsChanged;
            old.PropertyChanged -= view.OnViewModelPropertyChanged;
        }

        if (e.NewValue is EditorViewModel current)
        {
            current.PageImageChanged += view.OnPageImageChanged;
            current.CutoutsChanged += view.OnCutoutsChanged;
            current.PropertyChanged += view.OnViewModelPropertyChanged;
        }

        view.Canvas.ShowPage(null);
        view.Canvas.ShowCutouts(null, []);
    }

    private void OnPageImageChanged(object? sender, EventArgs e) => this.Canvas.ShowPage(this.ViewModel?.PageImage);

    private void OnCutoutsChanged(object? sender, CanvasEventArgs<bool> e)
    {
        if (this.ViewModel is not { } vm)
        {
            return;
        }

        this.Canvas.ShowCutouts(vm.SelectedCutout, vm.Neighbours());
        if (e.Value)
        {
            this.Canvas.ZoomToCutout();
        }

        this.ShowLines();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorViewModel.RequestedMode) && this.ViewModel is { } vm)
        {
            this.Canvas.Mode = vm.RequestedMode;
            this.Canvas.Focus(FocusState.Programmatic);
        }
    }

    private void ShowLines()
    {
        if (this.ViewModel?.SelectedCutout is not { } cutout)
        {
            return;
        }

        this.updatingLines = true;
        this.BaselineBox.Value = cutout.Baseline;
        this.XLineBox.Value = cutout.XLine;
        this.updatingLines = false;
    }

    private void OnLineValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!this.updatingLines && !double.IsNaN(args.NewValue))
        {
            this.ViewModel?.SetLines(
                sender == this.BaselineBox ? (int)args.NewValue : null,
                sender == this.XLineBox ? (int)args.NewValue : null);
        }
    }

    private void OnCellClicked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is CharacterCellViewModel cell)
        {
            this.ViewModel?.Select(cell.Glyph);
        }
    }

    private void OnCanvasKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (this.ViewModel is not { } vm)
        {
            return;
        }

        var shift = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        var step = shift ? 10 : 1;
        e.Handled = true;
        switch (e.Key)
        {
            case VirtualKey.B:
                vm.StartBox();
                break;
            case VirtualKey.E:
                vm.StartErasure();
                break;
            case VirtualKey.Escape:
                vm.RequestedMode = CanvasMode.Edit;
                break;
            case VirtualKey.Left:
                vm.Nudge(-step, 0);
                break;
            case VirtualKey.Right:
                vm.Nudge(step, 0);
                break;
            case VirtualKey.Up:
                vm.Nudge(0, -step);
                break;
            case VirtualKey.Down:
                vm.Nudge(0, step);
                break;
            default:
                e.Handled = false;
                break;
        }
    }

    private async void OnSaveInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        if (this.ViewModel is { } vm)
        {
            await vm.SaveAsync();
        }
    }

    private void OnWholePageClicked(object sender, RoutedEventArgs e) => this.Canvas.FitPage();

    private void OnZoomClicked(object sender, RoutedEventArgs e) => this.Canvas.ZoomToCutout();

    private async void OnRemoveClicked(object sender, RoutedEventArgs e)
    {
        if (this.ViewModel is { SelectedGlyph: { } glyph } vm
            && await this.ConfirmAsync(Strings.Format("RemoveConfirmFormat", GlyphNames.Display(glyph)), Strings.Get("Remove")))
        {
            vm.Remove();
        }
    }

    private async void OnBuildClicked(object sender, RoutedEventArgs e) => await this.BuildAsync(rebuild: false);

    private async void OnRebuildClicked(object sender, RoutedEventArgs e) => await this.BuildAsync(rebuild: true);

    private async Task BuildAsync(bool rebuild)
    {
        if (this.ViewModel is not { } vm)
        {
            return;
        }

        if (vm.HasUnsavedChanges)
        {
            if (!await this.ConfirmAsync(Strings.Get("SaveBeforeBuild"), Strings.Get("Save")))
            {
                return;
            }

            await vm.SaveAsync();
        }

        await vm.BuildAsync(rebuild);
    }

    private async void OnInstallClicked(object sender, RoutedEventArgs e)
    {
        if (this.ViewModel is not { } vm || sender is not FrameworkElement { Tag: BuildRecordViewModel build })
        {
            return;
        }

        var installed = vm.Installed();
        if (installed.Scope == InstallScope.AllUsers)
        {
            await this.ConfirmAsync(Strings.Get("InstalledForAllUsersHint"), primary: null);
            return;
        }

        var downgrade = installed.Version is { } current && current > build.Record.Version;
        var unknown = installed.Scope == InstallScope.CurrentUser && installed.Version is null;
        if (downgrade && !await this.ConfirmAsync(Strings.Format("DowngradeConfirmFormat", installed.Version!, build.Version), Strings.Get("Install")))
        {
            return;
        }

        if (unknown && !await this.ConfirmAsync(Strings.Get("ReplaceUnknownConfirm"), Strings.Get("Install")))
        {
            return;
        }

        await vm.InstallAsync(build.Record);
    }

    private async Task<bool> ConfirmAsync(string text, string? primary)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap },
            PrimaryButtonText = primary ?? string.Empty,
            CloseButtonText = primary is null ? Strings.Get("Ok") : Strings.Get("Cancel"),
            DefaultButton = primary is null ? ContentDialogButton.Close : ContentDialogButton.Primary,
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }
}
