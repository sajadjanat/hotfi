using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace HotspotShare;

public partial class MainWindow : Window
{
    private readonly HotspotService _hotspot = new();
    private readonly NetworkMonitorService _monitor = new();
    private readonly DispatcherTimer _refreshTimer;
    private readonly Random _random = new();

    public MainWindow()
    {
        InitializeComponent();

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _refreshTimer.Tick += RefreshTimer_Tick;

        Loaded += MainWindow_Loaded;
        SizeChanged += (_, _) => DrawStarfield();
        Closed += (_, _) => { _hotspot.Dispose(); _monitor.Dispose(); };
    }

    private void DrawStarfield()
    {
        StarCanvas.Children.Clear();
        double w = ActualWidth;
        double h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        int starCount = (int)(w * h / 2600);
        for (int i = 0; i < starCount; i++)
        {
            double size = _random.NextDouble() * 2.2 + 0.6;
            var star = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(Color.FromArgb(
                    (byte)(_random.Next(90, 230)), 255, 255, 255)),
                Opacity = _random.NextDouble() * 0.7 + 0.25
            };
            Canvas.SetLeft(star, _random.NextDouble() * w);
            Canvas.SetTop(star, _random.NextDouble() * h);
            StarCanvas.Children.Add(star);
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        DrawStarfield();

        HomePage.Visibility = Visibility.Visible;
        DevicesPage.Visibility = Visibility.Collapsed;
        SettingsPage.Visibility = Visibility.Collapsed;
        QrPage.Visibility = Visibility.Collapsed;
        NavHome.IsChecked = true;

        if (!_hotspot.Initialize(out var error))
        {
            MessageBox.Show(error, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            ToggleButton.IsEnabled = false;
            SaveConfigButton.IsEnabled = false;
            return;
        }

        SsidBox.Text = _hotspot.CurrentSsid ?? string.Empty;
        PasswordBox.Text = _hotspot.CurrentPassphrase ?? string.Empty;
        RefreshQrCode();

        _hotspot.StateChanged += Hotspot_StateChanged;
        UpdateStatusUi(_hotspot.IsOn, _hotspot.ClientCount);
        if (_hotspot.IsOn) _monitor.ResetSessionBaseline();

        _refreshTimer.Start();
    }

    private void Hotspot_StateChanged(object? sender, HotspotStateChangedEventArgs args)
    {
        Dispatcher.Invoke(() =>
        {
            bool wasOn = StatusText.Text == "وضعیت: روشن";
            UpdateStatusUi(args.IsOn, args.ClientCount);
            if (args.IsOn && !wasOn) _monitor.ResetSessionBaseline();
        });
    }

    private async void ToggleButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleButton.IsEnabled = false;
        try
        {
            var (success, error) = _hotspot.IsOn
                ? await _hotspot.StopAsync()
                : await _hotspot.StartAsync();

            if (!success)
            {
                MessageBox.Show(error, "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        finally
        {
            ToggleButton.IsEnabled = true;
        }
    }

    private async void SaveConfigButton_Click(object sender, RoutedEventArgs e)
    {
        SaveConfigButton.IsEnabled = false;
        ConfigMessage.Text = string.Empty;
        try
        {
            var (success, error) = await _hotspot.ConfigureAsync(SsidBox.Text.Trim(), PasswordBox.Text);
            ConfigMessage.Foreground = success
                ? new SolidColorBrush(Color.FromRgb(0x22, 0xD3, 0xEE))
                : new SolidColorBrush(Color.FromRgb(0xFF, 0x7A, 0xB8));
            ConfigMessage.Text = success ? "تنظیمات ذخیره شد." : error;
            if (success) RefreshQrCode();
        }
        finally
        {
            SaveConfigButton.IsEnabled = true;
        }
    }

    private void RefreshQrCode()
    {
        try
        {
            string ssid = _hotspot.CurrentSsid ?? SsidBox.Text;
            string password = _hotspot.CurrentPassphrase ?? PasswordBox.Text;
            if (string.IsNullOrWhiteSpace(ssid)) return;

            byte[] png = QrService.GenerateWifiQrPng(ssid, password);
            var bitmap = new BitmapImage();
            using var ms = new MemoryStream(png);
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = ms;
            bitmap.EndInit();
            QrImage.Source = bitmap;
            QrSsidText.Text = ssid;
        }
        catch
        {
            // non-fatal: QR is a convenience feature
        }
    }

    private void RefreshTimer_Tick(object? sender, EventArgs e)
    {
        UpdateStatusUi(_hotspot.IsOn, _hotspot.ClientCount);

        var devices = _monitor.GetConnectedDevices();
        DevicesList.ItemsSource = devices;
        NoDevicesText.Visibility = devices.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        DevicesHeaderText.Text = $"دستگاه‌های متصل ({devices.Count})";

        var (down, up) = _monitor.GetCurrentTraffic();
        DownloadSpeedText.Text = $"{down:0.0} KB/s";
        UploadSpeedText.Text = $"{up:0.0} KB/s";

        var (totalDown, totalUp) = _monitor.GetSessionTotals();
        TotalDownloadText.Text = $"{totalDown:0.0} MB";
        TotalUploadText.Text = $"{totalUp:0.0} MB";
    }

    private void UpdateStatusUi(bool isOn, int clientCount)
    {
        StatusText.Text = isOn ? "وضعیت: روشن" : "وضعیت: خاموش";
        ToggleButton.Content = isOn ? "خاموش کردن هات‌اسپات" : "روشن کردن هات‌اسپات";
        ClientCountText.Text = $"{clientCount} دستگاه متصل";
    }

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (HomePage is null) return; // guard during InitializeComponent

        HomePage.Visibility = ReferenceEquals(sender, NavHome) ? Visibility.Visible : Visibility.Collapsed;
        DevicesPage.Visibility = ReferenceEquals(sender, NavDevices) ? Visibility.Visible : Visibility.Collapsed;
        SettingsPage.Visibility = ReferenceEquals(sender, NavSettings) ? Visibility.Visible : Visibility.Collapsed;
        QrPage.Visibility = ReferenceEquals(sender, NavQr) ? Visibility.Visible : Visibility.Collapsed;
    }
}
