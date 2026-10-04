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
            error = "No active internet connection was found.";
            return false;
        }

        try
        {
            _manager = NetworkOperatorTetheringManager.CreateFromConnectionProfile(profile);
        }
        catch (Exception ex)
        {
            error = $"Cannot access Windows Mobile Hotspot: {ex.Message}";
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
        if (_manager is null) return (false, "The hotspot service has not been initialized.");

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
            return (false, $"Could not turn on the hotspot: {ex.GetType().Name}: {ex.Message}");
        }

        RaiseStateChanged();
        return (true, null);
    }

    public async Task<(bool success, string? error)> StopAsync()
    {
        if (_manager is null) return (false, "The hotspot service has not been initialized.");

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
            return (false, $"Could not turn off the hotspot: {ex.GetType().Name}: {ex.Message}");
        }

        RaiseStateChanged();
        return (true, null);
    }

    public async Task<(bool success, string? error)> ConfigureAsync(string ssid, string passphrase)
    {
        if (_manager is null) return (false, "The hotspot service has not been initialized.");

        if (string.IsNullOrWhiteSpace(ssid) || ssid.Length > 32)
            return (false, "The network name must contain 1–32 characters.");
        if (passphrase.Length < 8 || passphrase.Length > 63)
            return (false, "The password must contain 8–63 characters.");

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
            return (false, $"Could not save the network name or password: {ex.Message}");
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
        TetheringOperationStatus.MobileBroadbandDeviceOff => "The mobile broadband device is turned off.",
        TetheringOperationStatus.WiFiDeviceOff => "The Wi-Fi adapter is turned off.",
        TetheringOperationStatus.EntitlementCheckTimeout => "The carrier authorization check timed out.",
        TetheringOperationStatus.EntitlementCheckFailure => "The carrier authorization check failed.",
        TetheringOperationStatus.OperationInProgress => "Another hotspot operation is still in progress.",
        TetheringOperationStatus.BluetoothDeviceOff => "Bluetooth is turned off.",
        TetheringOperationStatus.NetworkLimitedConnectivity => "The internet connection has limited connectivity.",
        TetheringOperationStatus.Unknown => "An unknown hotspot error occurred.",
        _ => $"Unknown hotspot error ({status})."
    };

    public void Dispose()
    {
    }
}
