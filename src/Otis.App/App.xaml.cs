// SPDX-License-Identifier: GPL-3.0-or-later
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
        this.window = new Window { Title = "Otis", Content = new ShellPage() };
        this.window.Activate();
    }
}
