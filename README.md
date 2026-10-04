<div align="center">

# HOTFI

**Your Windows hotspot, one click away.**

Turn Wi-Fi sharing on or off, connect with a QR code, and keep an eye on your connected devices — all in a small desktop app.

![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011-0078D4?logo=windows&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet&logoColor=white)
![WPF](https://img.shields.io/badge/UI-WPF-8B5CF6)
[![Release](https://img.shields.io/github/v/release/sajadjanat/hotfi)](https://github.com/sajadjanat/hotfi/releases/latest)

[Download](https://github.com/sajadjanat/hotfi/releases/latest) · [Features](#features) · [Quick start](#quick-start) · [Build from source](#build-from-source)

</div>

## A closer look

| Home | Connected devices |
| :---: | :---: |
| ![HOTFI home: hotspot status and live traffic](screenshots/home.png) | ![HOTFI connected devices: names, addresses, and connection times](screenshots/devices.png) |

| Network settings | Connect with QR |
| :---: | :---: |
| ![HOTFI settings: network name and password](screenshots/settings.png) | ![HOTFI QR code for joining a sample Wi-Fi network](screenshots/qr.png) |

*The native WPF interface rendered with fictional devices and a sample network. No personal network credentials are shown.*

## Features

- **One-click hotspot control:** turn Windows Mobile Hotspot on or off.
- **Network settings:** update the network name (SSID) and password.
- **QR connection:** let a phone scan a Wi-Fi QR code instead of typing credentials.
- **Connected devices:** view discovered names, IP and MAC addresses, and the time each device was first seen.
- **Live traffic:** see current download/upload rates and session totals for the hotspot adapter.
- **English interface:** left-to-right layout, Segoe UI typography, English messages, and consistent number formatting.
- **Galaxy theme:** dark surfaces, stars, and violet/cyan accents.
- **Single-file installation:** install under your Windows user account with Desktop and Start Menu shortcuts.

## Download and install

1. Download **`HOTFI-Setup.exe`** from the [latest release](https://github.com/sajadjanat/hotfi/releases/latest).
2. Run the file. HOTFI installs to `%LOCALAPPDATA%\Programs\HOTFI` and creates shortcuts.
3. Open HOTFI from your Desktop or Start Menu.

The published Windows x64 build includes the .NET runtime. You do not need to install the SDK to use it.

**Requirements:** Windows 10 or 11, an active internet connection, and a Wi-Fi adapter that supports Windows Mobile Hotspot. HOTFI uses Windows' built-in sharing capability; availability depends on the adapter, drivers, and system policy.

## Quick start

1. Open **Settings**, enter a network name and a password, then select **Save settings**. The SSID accepts 1–32 characters; the password accepts 8–63.
2. Go to **Home** and select **Turn on hotspot**.
3. Open **QR** and scan the code with a compatible phone camera to join the network.
4. Check **Devices** for discovered clients and **Home** for traffic rates and session totals.
5. Select **Turn off hotspot** when you are done.

Device and traffic information refreshes every two seconds. Device names are resolved through DNS/NetBIOS when available; otherwise, the app displays **Unknown device**. Connection times indicate when HOTFI first discovered a device, and traffic totals represent the hotspot adapter rather than separate per-device usage.

## Build from source

Use Windows and the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
git clone https://github.com/sajadjanat/hotfi.git
cd hotfi
dotnet build HotspotShare/HotspotShare.csproj -c Release
```

Create the self-contained Windows x64 release:

```powershell
dotnet publish HotspotShare/HotspotShare.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -o publish
```

The output executable is `publish/HOTFI.exe`. Distribute it as `HOTFI-Setup.exe` to use the built-in per-user installation flow. Running the app outside its installed folder starts this installation flow.

### Recreate the screenshots

```powershell
dotnet run --project tools/Hotfi.Screenshots -c Release -- screenshots
```

The documentation tool renders the application's actual WPF views with sample values. It does not open a window, install HOTFI, read your Wi-Fi credentials, or change the hotspot state.

## Project structure

| File | Responsibility |
| --- | --- |
| `HotspotShare/MainWindow.xaml` | English interface and galaxy theme. |
| `HotspotShare/MainWindow.xaml.cs` | Navigation, actions, and UI refresh. |
| `HotspotShare/HotspotService.cs` | Windows Mobile Hotspot API and validation. |
| `HotspotShare/NetworkMonitorService.cs` | Adapter traffic, client discovery, and device-name lookup. |
| `HotspotShare/QrService.cs` | Wi-Fi QR code generation with QRCoder. |
| `HotspotShare/InstallerService.cs` | Per-user installation and shortcuts. |
| `tools/Hotfi.Screenshots/` | Reproducible documentation images. |

## Troubleshooting

**No active internet connection:** connect the PC to the internet, then reopen HOTFI.

**Wi-Fi adapter is turned off:** enable Wi-Fi and check that Windows Mobile Hotspot is available in system settings.

**A device does not appear:** client discovery relies on the hotspot adapter's ARP entries. Give it a moment after connecting; driver and Windows behavior may affect detection.

**Traffic remains at zero:** confirm that the hotspot is on and a connected device is transferring data. Counters depend on the Windows virtual adapter's statistics.

## Contributing

[Report a bug or suggest a feature](https://github.com/sajadjanat/hotfi/issues). Keep interface copy and documentation in English. Include your Windows version and Wi-Fi adapter details when reporting hotspot issues, and avoid sharing passwords or private network information.

---

Built by [Sajad Janat](https://github.com/sajadjanat). Powered by .NET 8, WPF, Windows Mobile Hotspot APIs, and [QRCoder](https://github.com/codebude/QRCoder).
