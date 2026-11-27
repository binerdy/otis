// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.App.Services;
using Otis.App.ViewModels;
using Otis.Infrastructure.Storage;

namespace Otis.App.Views;

public sealed partial class ShellPage : Page
{
    public ShellPage(ShellViewModel viewModel)
    {
        this.ViewModel = viewModel;
        this.InitializeComponent();
        viewModel.RefreshRecent();
    }

    public ShellViewModel ViewModel { get; }

    public async Task<bool> ConfirmCloseAsync()
    {
        if (this.ViewModel.Editor is not { HasUnsavedChanges: true } editor)
        {
            return true;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Content = new TextBlock { Text = Strings.Get("UnsavedOnClose"), TextWrapping = TextWrapping.Wrap },
            PrimaryButtonText = Strings.Get("Save"),
            SecondaryButtonText = Strings.Get("Discard"),
            CloseButtonText = Strings.Get("Cancel"),
            DefaultButton = ContentDialogButton.Primary,
        };
        var answer = await dialog.ShowAsync();
        if (answer == ContentDialogResult.Primary)
        {
            await editor.SaveAsync();
        }

        return answer != ContentDialogResult.None;
    }

    private async void OnNewProjectClicked(object sender, RoutedEventArgs e)
    {
        var name = new TextBox { PlaceholderText = Strings.Get("FamilyNamePlaceholder") };
        var dialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = Strings.Get("NewProject"),
            Content = new StackPanel
            {
                Spacing = 8,
                Children = { new TextBlock { Text = Strings.Get("FamilyNameHint"), TextWrapping = TextWrapping.Wrap }, name },
            },
            PrimaryButtonText = Strings.Get("Create"),
            CloseButtonText = Strings.Get("Cancel"),
            DefaultButton = ContentDialogButton.Primary,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(name.Text))
        {
            await this.ViewModel.CreateAsync(name.Text);
        }
    }

    private async void OnDeleteProjectClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string path })
        {
            return;
        }

        var (projectFiles, fontFiles) = ShellViewModel.FilesToDelete(path);
        var fonts = new CheckBox { Content = Strings.Format("AlsoDeleteFontsFormat", fontFiles), IsEnabled = fontFiles > 0 };
        var dialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = Strings.Get("DeleteProject"),
            Content = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = Strings.Format("DeleteProjectHintFormat", System.IO.Path.GetFileName(path), projectFiles), TextWrapping = TextWrapping.Wrap },
                    fonts,
                },
            },
            PrimaryButtonText = Strings.Get("MoveToRecycleBin"),
            CloseButtonText = Strings.Get("Cancel"),
            DefaultButton = ContentDialogButton.Close,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            this.ViewModel.Delete(path, fonts.IsChecked == true);
        }
    }

    private async void OnSettingsClicked(object sender, RoutedEventArgs e)
    {
        var settings = this.ViewModel.Services.Settings;
        var languages = new ComboBox
        {
            Header = Strings.Get("Language"),
            ItemsSource = new[] { Strings.Get("LanguageSystem"), "English", "Deutsch (Schweiz)" },
            SelectedIndex = (int)settings.Language,
        };
        var nightly = new ToggleSwitch { Header = Strings.Get("NightlyUpdates"), IsOn = settings.NightlyUpdates };
        var keys = this.ViewModel.Services.Workspace.ApiKeys;
        var key = new PasswordBox
        {
            Header = Strings.Get("ApiKey"),
            PlaceholderText = keys.HasKey ? Strings.Get("ApiKeySaved") : "sk-ant-…",
        };
        var removeKey = new CheckBox { Content = Strings.Get("RemoveApiKey"), Visibility = keys.HasKey ? Visibility.Visible : Visibility.Collapsed };
        var keyHint = new TextBlock { Text = Strings.Get("ApiKeyHint"), TextWrapping = TextWrapping.Wrap, FontSize = 12, Opacity = 0.7 };
        var dialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = Strings.Get("Settings"),
            Content = new StackPanel { Spacing = 16, Children = { languages, nightly, new StackPanel { Spacing = 6, Children = { key, keyHint, removeKey } } } },
            PrimaryButtonText = Strings.Get("SaveSettings"),
            CloseButtonText = Strings.Get("Cancel"),
            DefaultButton = ContentDialogButton.Primary,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            this.ViewModel.Services.SaveSettings(settings with { Language = (AppLanguage)languages.SelectedIndex, NightlyUpdates = nightly.IsOn });
            if (!string.IsNullOrWhiteSpace(key.Password))
            {
                keys.Save(key.Password);
            }
            else if (removeKey.IsChecked == true)
            {
                keys.Remove();
            }
        }
    }

    private void OnRestartClicked(object sender, RoutedEventArgs e) => this.ViewModel.Services.Updates.ApplyAndRestart();
}
