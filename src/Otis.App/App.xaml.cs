// SPDX-License-Identifier: GPL-3.0-or-later
using Otis.App.Services;
using Otis.App.ViewModels;
using Otis.App.Views;

namespace Otis.App;

public sealed partial class OtisApplication : Application
{
    private Window? window;

    public OtisApplication()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        this.window = new Window { Title = "Otis" };
        var services = new AppServices(this.window.DispatcherQueue);
        FileDialogs.WindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this.window);
        var shell = new ShellPage(new ShellViewModel(services));
        this.window.Content = shell;
        var closing = false;
        this.window.AppWindow.Closing += async (_, e) =>
        {
            if (closing)
            {
                return;
            }

            e.Cancel = true;
            if (await shell.ConfirmCloseAsync())
            {
                closing = true;
                this.window.Close();
            }
        };
        this.window.Activate();
        services.StartUpdateChecks();
    }
}
