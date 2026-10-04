using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HotspotShare;

public sealed record ConnectedDevice(string IpAddress, string MacAddress, string HostName, DateTime ConnectedSince);

public sealed class NetworkMonitorService : IDisposable
{
    private readonly ConcurrentDictionary<string, string> _hostnameCache = new();
    private readonly ConcurrentDictionary<string, bool> _resolvingNow = new();
    private readonly Dictionary<string, DateTime> _firstSeen = new();

    private long _lastDownBytes;
    private long _lastUpBytes;
    private DateTime _lastSampleTime;
    private bool _rateInitialized;

    private long _downBaseline;
    private long _upBaseline;
    private bool _baselineSet;

    public NetworkInterface? FindHotspotAdapter()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .FirstOrDefault(ni =>
                ni.OperationalStatus == OperationalStatus.Up &&
                (ni.Description.Contains("Wi-Fi Direct Virtual Adapter", StringComparison.OrdinalIgnoreCase) ||
                 ni.Description.Contains("Hosted Network Virtual Adapter", StringComparison.OrdinalIgnoreCase) ||
                 ni.Description.Contains("Microsoft Wi-Fi Direct", StringComparison.OrdinalIgnoreCase)));
    }

    public List<ConnectedDevice> GetConnectedDevices()
    {
        var devices = new List<ConnectedDevice>();
        var adapter = FindHotspotAdapter();
        if (adapter is null) return devices;

        var ipv4 = adapter.GetIPProperties().UnicastAddresses
            .FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
        if (ipv4 is null) return devices;

        var hostIp = ipv4.Address.ToString();
        var seenMacs = new HashSet<string>();

        try
        {
            var psi = new ProcessStartInfo("arp", $"-a -N {hostIp}")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process is null) return devices;
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(2000);

            // Windows Mobile Hotspot registers client ARP entries as "static" (not "dynamic"
            // like a regular router), so both types must be accepted here.
            foreach (Match m in Regex.Matches(output, @"(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\s+([0-9a-fA-F-]{17})\s+(dynamic|static)"))
            {
                var ip = m.Groups[1].Value;
                var mac = m.Groups[2].Value.ToUpperInvariant();
                if (ip == hostIp) continue;
                if (mac == "FF-FF-FF-FF-FF-FF") continue;

                var lastOctet = ip.Split('.').Last();
                if (lastOctet == "255") continue; // broadcast
                var firstOctet = int.Parse(ip.Split('.')[0]);
                if (firstOctet >= 224) continue; // multicast (224-239)

                seenMacs.Add(mac);
                if (!_firstSeen.ContainsKey(mac)) _firstSeen[mac] = DateTime.Now;

                string hostName = _hostnameCache.GetValueOrDefault(ip, "Identifying device…");
                TriggerHostnameResolve(ip);

                devices.Add(new ConnectedDevice(ip, mac, hostName, _firstSeen[mac]));
            }
        }
        catch
        {
            // arp not available or failed; return what we have
        }

        // forget devices that disconnected so reconnects show a fresh "connected since" time
        foreach (var mac in _firstSeen.Keys.Where(k => !seenMacs.Contains(k)).ToList())
        {
            _firstSeen.Remove(mac);
        }

        return devices;
    }

    private void TriggerHostnameResolve(string ip)
    {
        if (_hostnameCache.ContainsKey(ip)) return;
        if (!_resolvingNow.TryAdd(ip, true)) return;

        _ = Task.Run(async () =>
        {
            string result = await ResolveHostnameAsync(ip);
            _hostnameCache[ip] = result;
            _resolvingNow.TryRemove(ip, out _);
        });
    }

    private static async Task<string> ResolveHostnameAsync(string ip)
    {
        try
        {
            var dnsTask = Dns.GetHostEntryAsync(ip);
            if (await Task.WhenAny(dnsTask, Task.Delay(800)) == dnsTask && dnsTask.IsCompletedSuccessfully)
            {
                var name = dnsTask.Result.HostName;
                if (!string.IsNullOrWhiteSpace(name) && name != ip) return name;
            }
        }
        catch
        {
            // ignore, fall through to NetBIOS attempt
        }

        try
        {
            var psi = new ProcessStartInfo("nbtstat", $"-A {ip}")
            {
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process is not null)
            {
                process.StandardInput.Close();
                string output = await process.StandardOutput.ReadToEndAsync();
                process.WaitForExit(1500);

                var match = Regex.Match(output, @"^\s*(\S+)\s+<00>\s+UNIQUE", RegexOptions.Multiline);
                if (match.Success) return match.Groups[1].Value.Trim();
            }
        }
        catch
        {
            // ignore
        }

        return "Unknown device";
    }

    /// <summary>
    /// The Mobile Hotspot virtual adapter never appears as a "Network Interface" performance
    /// counter instance on Windows, so traffic is read directly from the adapter's own IPv4
    /// statistics (cumulative byte counters) instead.
    /// </summary>
    public (double downloadKBs, double uploadKBs) GetCurrentTraffic()
    {
        var adapter = FindHotspotAdapter();
        if (adapter is null)
        {
            _rateInitialized = false;
            return (0, 0);
        }

        IPv4InterfaceStatistics stats;
        try
        {
            stats = adapter.GetIPv4Statistics();
        }
        catch
        {
            return (0, 0);
        }

        var now = DateTime.UtcNow;
        if (!_rateInitialized)
        {
            _lastDownBytes = stats.BytesReceived;
            _lastUpBytes = stats.BytesSent;
            _lastSampleTime = now;
            _rateInitialized = true;
            return (0, 0);
        }

        double elapsed = (now - _lastSampleTime).TotalSeconds;
        if (elapsed <= 0) return (0, 0);

        long downDelta = Math.Max(0, stats.BytesReceived - _lastDownBytes);
        long upDelta = Math.Max(0, stats.BytesSent - _lastUpBytes);

        _lastDownBytes = stats.BytesReceived;
        _lastUpBytes = stats.BytesSent;
        _lastSampleTime = now;

        return (downDelta / 1024.0 / elapsed, upDelta / 1024.0 / elapsed);
    }

    public void ResetSessionBaseline()
    {
        var adapter = FindHotspotAdapter();
        if (adapter is null)
        {
            _baselineSet = false;
            return;
        }

        try
        {
            var stats = adapter.GetIPv4Statistics();
            _downBaseline = stats.BytesReceived;
            _upBaseline = stats.BytesSent;
            _baselineSet = true;

            _rateInitialized = false;
        }
        catch
        {
            _baselineSet = false;
        }
    }

    public (double downloadMB, double uploadMB) GetSessionTotals()
    {
        if (!_baselineSet) return (0, 0);

        var adapter = FindHotspotAdapter();
        if (adapter is null) return (0, 0);

        try
        {
            var stats = adapter.GetIPv4Statistics();
            long down = Math.Max(0, stats.BytesReceived - _downBaseline);
            long up = Math.Max(0, stats.BytesSent - _upBaseline);
            return (down / 1024.0 / 1024.0, up / 1024.0 / 1024.0);
        }
        catch
        {
            return (0, 0);
        }
    }

    public void Dispose()
    {
    }
}
