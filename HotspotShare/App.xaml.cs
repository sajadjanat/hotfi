using System.Windows;

namespace HotspotShare;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        base.OnStartup(e);

        if (InstallerService.EnsureInstalledAndRelaunchIfNeeded(e.Args))
        {
            Shutdown();
            return;
        }

        new MainWindow().Show();
    }
}

