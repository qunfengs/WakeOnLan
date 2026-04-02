using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using WakeOnLan.Models;

namespace WakeOnLan.Services;

public sealed class NetworkDiscoveryService
{
    private const int MaxParallelism = 32;

    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern int SendARP(int destinationIp, int sourceIp, byte[] macAddress, ref int physicalAddressLength);

    public NetworkContext GetCurrentNetworkContext()
    {
        var candidates = NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic =>
                nic.OperationalStatus == OperationalStatus.Up &&
                nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                nic.GetIPProperties().GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork))
            .Select(nic => new
            {
                Nic = nic,
                Unicast = nic.GetIPProperties().UnicastAddresses.FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork)
            })
            .Where(x => x.Unicast is not null && x.Unicast.IPv4Mask is not null)
            .OrderByDescending(x => x.Nic.Speed)
            .FirstOrDefault();

        if (candidates?.Unicast is null)
        {
            throw new InvalidOperationException("未找到可用的 IPv4 网络适配器。");
        }

        var ipAddress = candidates.Unicast.Address;
        var subnetMask = candidates.Unicast.IPv4Mask!;
        var broadcastAddress = CalculateBroadcast(ipAddress, subnetMask);

        return new NetworkContext(
            candidates.Nic.Name,
            ipAddress,
            subnetMask,
            broadcastAddress,
            EnumerateSubnetAddresses(ipAddress, subnetMask));
    }

    public async Task<IReadOnlyList<DiscoveredDevice>> ScanAsync(
        NetworkContext context,
        IProgress<DiscoveredDevice>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var devices = new List<DiscoveredDevice>();
        using var gate = new SemaphoreSlim(MaxParallelism);

        var tasks = context.ScanTargets.Select(async ipAddress =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                var device = await ProbeDeviceAsync(ipAddress, cancellationToken);
                if (device is not null)
                {
                    lock (devices)
                    {
                        devices.Add(device);
                    }

                    progress?.Report(device);
                }
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks);

        return devices
            .OrderBy(x => x.IpAddress, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task<DiscoveredDevice?> ProbeDeviceAsync(IPAddress ipAddress, CancellationToken cancellationToken)
    {
        using var ping = new Ping();

        try
        {
            var reply = await ping.SendPingAsync(ipAddress, 250);
            if (reply.Status != IPStatus.Success)
            {
                return null;
            }
        }
        catch
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var macAddress = TryGetMacAddress(ipAddress);
        if (string.IsNullOrWhiteSpace(macAddress))
        {
            return null;
        }

        var hostName = await TryResolveHostNameAsync(ipAddress);

        return new DiscoveredDevice
        {
            HostName = hostName,
            IpAddress = ipAddress.ToString(),
            MacAddress = macAddress,
            DiscoveredAt = DateTime.Now
        };
    }

    private static async Task<string> TryResolveHostNameAsync(IPAddress ipAddress)
    {
        try
        {
            var entry = await Dns.GetHostEntryAsync(ipAddress);
            return NormalizeHostName(entry.HostName);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string NormalizeHostName(string hostName)
    {
        if (string.IsNullOrWhiteSpace(hostName))
        {
            return string.Empty;
        }

        return hostName.EndsWith(".lan", StringComparison.OrdinalIgnoreCase)
            ? hostName[..^4]
            : hostName;
    }

    private static string TryGetMacAddress(IPAddress ipAddress)
    {
        try
        {
            var mac = new byte[6];
            var length = mac.Length;
            var addressBytes = ipAddress.GetAddressBytes();
            var destination = BitConverter.ToInt32(addressBytes, 0);
            var result = SendARP(destination, 0, mac, ref length);
            if (result != 0 || length <= 0)
            {
                return string.Empty;
            }

            return string.Join(":", mac.Take(length).Select(b => b.ToString("X2")));
        }
        catch
        {
            return string.Empty;
        }
    }

    private static IPAddress CalculateBroadcast(IPAddress address, IPAddress subnetMask)
    {
        var addressBytes = address.GetAddressBytes();
        var subnetBytes = subnetMask.GetAddressBytes();
        var broadcastBytes = new byte[addressBytes.Length];

        for (var index = 0; index < addressBytes.Length; index++)
        {
            broadcastBytes[index] = (byte)(addressBytes[index] | (subnetBytes[index] ^ byte.MaxValue));
        }

        return new IPAddress(broadcastBytes);
    }

    private static IReadOnlyList<IPAddress> EnumerateSubnetAddresses(IPAddress address, IPAddress subnetMask)
    {
        var addressValue = ToUInt32(address);
        var maskValue = ToUInt32(subnetMask);
        var networkValue = addressValue & maskValue;
        var broadcastValue = networkValue | ~maskValue;

        var addresses = new List<IPAddress>();
        for (var current = networkValue + 1; current < broadcastValue; current++)
        {
            if (current == addressValue)
            {
                continue;
            }

            addresses.Add(FromUInt32(current));
        }

        return addresses;
    }

    private static uint ToUInt32(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return BitConverter.ToUInt32(bytes, 0);
    }

    private static IPAddress FromUInt32(uint value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return new IPAddress(bytes);
    }
}
