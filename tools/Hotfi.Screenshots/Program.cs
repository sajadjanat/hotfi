using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using HotspotShare;

internal static class Program
{
    // Render the actual WPF views without opening a window, installing HOTFI,
    // reading network credentials, or changing the hotspot state.
    [STAThread]
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var window = new MainWindow();
        var content = (FrameworkElement)window.Content;
        const int width = 440;
        const int height = 640;
        var output = System.IO.Path.GetFullPath(args.FirstOrDefault() ?? "screenshots");
        Directory.CreateDirectory(output);

        T Find<T>(string name) where T : FrameworkElement => (T)window.FindName(name);
        void Text(string name, string value) => Find<TextBlock>(name).Text = value;

        // Fictional values used only in the documentation images.
        typeof(MainWindow).GetMethod("UpdateStatusUi", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(window, [true, 2]);
        Text("DownloadSpeedText", "428.6 KB/s");
        Text("UploadSpeedText", "92.4 KB/s");
        Text("TotalDownloadText", "124.8 MB");
        Text("TotalUploadText", "18.6 MB");
        Find<TextBox>("SsidBox").Text = "HOTFI-Demo";
        Find<TextBox>("PasswordBox").Text = "DocsOnly-2026";
        Text("QrSsidText", "HOTFI-Demo");
        using (var stream = new MemoryStream(QrService.GenerateWifiQrPng("HOTFI-Demo", "DocsOnly-2026")))
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            Find<Image>("QrImage").Source = bitmap;
        }

        Find<ItemsControl>("DevicesList").ItemsSource = new[]
        {
            new ConnectedDevice("192.168.137.12", "02-00-00-00-00-12", "Demo Phone", new DateTime(2026, 1, 1, 10, 24, 18)),
            new ConnectedDevice("192.168.137.24", "02-00-00-00-00-24", "Demo Laptop", new DateTime(2026, 1, 1, 10, 28, 42))
        };
        Text("DevicesHeaderText", "Connected devices (2)");
        Find<TextBlock>("NoDevicesText").Visibility = Visibility.Collapsed;

        var random = new Random(42);
        var stars = Find<Canvas>("StarCanvas");
        for (int i = 0; i < width * height / 2600; i++)
        {
            double size = random.NextDouble() * 2.2 + 0.6;
            var star = new Ellipse
            {
                Width = size, Height = size,
                Fill = new SolidColorBrush(Color.FromArgb((byte)random.Next(90, 230), 255, 255, 255)),
                Opacity = random.NextDouble() * 0.7 + 0.25
            };
            Canvas.SetLeft(star, random.NextDouble() * width);
            Canvas.SetTop(star, random.NextDouble() * height);
            stars.Children.Add(star);
        }

        foreach (var (nav, file) in new[] { ("NavHome", "home"), ("NavDevices", "devices"), ("NavSettings", "settings"), ("NavQr", "qr") })
        {
            Find<RadioButton>(nav).IsChecked = true;
            content.Measure(new Size(width, height));
            content.Arrange(new Rect(0, 0, width, height));
            content.UpdateLayout();
            var image = new RenderTargetBitmap(width * 2, height * 2, 192, 192, PixelFormats.Pbgra32);
            image.Render(content);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            using var stream = File.Create(System.IO.Path.Combine(output, file + ".png"));
            encoder.Save(stream);
            Console.WriteLine($"Rendered {file}.png using the English WPF interface.");
        }
        window.Close();
        app.Shutdown();
    }
}
