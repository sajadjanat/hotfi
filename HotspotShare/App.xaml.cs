using System.Windows;

namespace HotspotShare;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (InstallerService.EnsureInstalledAndRelaunchIfNeeded(e.Args))
        {
            Shutdown();
            return;
        }

        new MainWindow().Show();
    }
}

