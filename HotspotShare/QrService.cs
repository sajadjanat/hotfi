using System.Text;
using QRCoder;

namespace HotspotShare;

public static class QrService
{
    public static byte[] GenerateWifiQrPng(string ssid, string password)
    {
        string payload = $"WIFI:T:WPA;S:{Escape(ssid)};P:{Escape(password)};H:false;;";

        using var generator = new QRCodeGenerator();
        var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(18);
    }

    private static string Escape(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (char c in value)
        {
            if (c is '\\' or ';' or ',' or ':' or '"')
                sb.Append('\\');
            sb.Append(c);
        }
        return sb.ToString();
    }
}
