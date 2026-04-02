using System.Net;
using System.Net.Sockets;

namespace WakeOnLan.Services;

public sealed class WakeOnLanService
{
    public async Task WakeAsync(string macAddress, IPAddress broadcastAddress, CancellationToken cancellationToken = default)
    {
        var macBytes = ParseMac(macAddress);
        var payload = BuildMagicPacket(macBytes);

        using var client = new UdpClient();
        client.EnableBroadcast = true;

        var endpoint = new IPEndPoint(broadcastAddress, 9);
        await client.SendAsync(payload, payload.Length, endpoint).WaitAsync(cancellationToken);
    }

    private static byte[] ParseMac(string macAddress)
    {
        var sanitized = macAddress.Replace("-", string.Empty).Replace(":", string.Empty).Trim();
        if (sanitized.Length != 12)
        {
            throw new FormatException("Invalid MAC address format.");
        }

        var bytes = new byte[6];
        for (var index = 0; index < 6; index++)
        {
            bytes[index] = Convert.ToByte(sanitized.Substring(index * 2, 2), 16);
        }

        return bytes;
    }

    private static byte[] BuildMagicPacket(byte[] macBytes)
    {
        var packet = new byte[102];
        for (var index = 0; index < 6; index++)
        {
            packet[index] = 0xFF;
        }

        for (var index = 1; index <= 16; index++)
        {
            Buffer.BlockCopy(macBytes, 0, packet, index * 6, macBytes.Length);
        }

        return packet;
    }
}
