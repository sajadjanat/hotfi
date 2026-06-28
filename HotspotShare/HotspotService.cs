using System;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.Networking.NetworkOperators;

namespace HotspotShare;

public sealed class HotspotStateChangedEventArgs : EventArgs
{
    public bool IsOn { get; init; }
    public int ClientCount { get; init; }
}

public sealed class HotspotService : IDisposable
{
    private NetworkOperatorTetheringManager? _manager;

    public event EventHandler<HotspotStateChangedEventArgs>? StateChanged;

    public bool IsOn => _manager?.TetheringOperationalState == TetheringOperationalState.On;

    public int ClientCount => (int)(_manager?.ClientCount ?? 0);

    public string? CurrentSsid { get; private set; }
    public string? CurrentPassphrase { get; private set; }

    public bool Initialize(out string? error)
    {
        error = null;
        var profile = NetworkInformation.GetInternetConnectionProfile();
        if (profile is null)
        {
            error = "هیچ اتصال اینترنت فعالی پیدا نشد.";
            return false;
        }

        try
        {
            _manager = NetworkOperatorTetheringManager.CreateFromConnectionProfile(profile);
        }
        catch (Exception ex)
        {
            error = $"امکان دسترسی به قابلیت Mobile Hotspot نیست: {ex.Message}";
            return false;
        }

        var config = _manager.GetCurrentAccessPointConfiguration();
        CurrentSsid = config.Ssid;
        CurrentPassphrase = config.Passphrase;

        RaiseStateChanged();
        return true;
    }

    public async Task<(bool success, string? error)> StartAsync()
    {
        if (_manager is null) return (false, "سرویس مقداردهی نشده است.");

        try
        {
            var result = await _manager.StartTetheringAsync();
            if (result.Status != TetheringOperationStatus.Success)
            {
                return (false, DescribeStatus(result.Status));
            }
        }
        catch (Exception ex)
        {
            return (false, $"خطای سیستمی هنگام روشن کردن: {ex.GetType().Name}: {ex.Message}");
        }

        RaiseStateChanged();
        return (true, null);
    }

    public async Task<(bool success, string? error)> StopAsync()
    {
        if (_manager is null) return (false, "سرویس مقداردهی نشده است.");

        try
        {
            var result = await _manager.StopTetheringAsync();
            if (result.Status != TetheringOperationStatus.Success)
            {
                return (false, DescribeStatus(result.Status));
            }
        }
        catch (Exception ex)
        {
            return (false, $"خطای سیستمی هنگام خاموش کردن: {ex.GetType().Name}: {ex.Message}");
        }

        RaiseStateChanged();
        return (true, null);
    }

    public async Task<(bool success, string? error)> ConfigureAsync(string ssid, string passphrase)
    {
        if (_manager is null) return (false, "سرویس مقداردهی نشده است.");

        if (string.IsNullOrWhiteSpace(ssid) || ssid.Length > 32)
            return (false, "نام شبکه باید بین ۱ تا ۳۲ کاراکتر باشد.");
        if (passphrase.Length < 8 || passphrase.Length > 63)
            return (false, "رمز عبور باید بین ۸ تا ۶۳ کاراکتر باشد.");

        var config = new NetworkOperatorTetheringAccessPointConfiguration
        {
            Ssid = ssid,
            Passphrase = passphrase
        };

        try
        {
            await _manager.ConfigureAccessPointAsync(config);
        }
        catch (Exception ex)
        {
            return (false, $"تنظیم نام/رمز شبکه با خطا مواجه شد: {ex.Message}");
        }

        CurrentSsid = ssid;
        CurrentPassphrase = passphrase;
        return (true, null);
    }

    private void RaiseStateChanged()
    {
        StateChanged?.Invoke(this, new HotspotStateChangedEventArgs
        {
            IsOn = IsOn,
            ClientCount = ClientCount
        });
    }

    private static string DescribeStatus(TetheringOperationStatus status) => status switch
    {
        TetheringOperationStatus.MobileBroadbandDeviceOff => "دستگاه موبایل برادبند خاموش است.",
        TetheringOperationStatus.WiFiDeviceOff => "آداپتور Wi-Fi خاموش است.",
        TetheringOperationStatus.EntitlementCheckTimeout => "بررسی مجوز اپراتور با تایم‌اوت مواجه شد.",
        TetheringOperationStatus.EntitlementCheckFailure => "بررسی مجوز اپراتور ناموفق بود.",
        TetheringOperationStatus.OperationInProgress => "عملیات قبلی هنوز در حال انجام است.",
        TetheringOperationStatus.BluetoothDeviceOff => "بلوتوث خاموش است.",
        TetheringOperationStatus.NetworkLimitedConnectivity => "اتصال اینترنت محدود است.",
        TetheringOperationStatus.Unknown => "خطای نامشخص.",
        _ => $"خطای ناشناخته ({status})."
    };

    public void Dispose()
    {
    }
}
