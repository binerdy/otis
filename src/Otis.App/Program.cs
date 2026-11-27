// SPDX-License-Identifier: GPL-3.0-or-later
using Microsoft.UI.Dispatching;
using Velopack;

namespace Otis.App;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        // Velopack must run before any UI so install/update hooks can exit the process early.
        VelopackApp.Build().Run();
        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(StartApplication);
    }

    private static void StartApplication(ApplicationInitializationCallbackParams parameters)
    {
        var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
        SynchronizationContext.SetSynchronizationContext(context);
        var app = new OtisApplication();
        GC.KeepAlive(app);
    }
}
