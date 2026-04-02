using System.Net;

namespace WakeOnLan.Services;

public sealed record NetworkContext(
    string AdapterName,
    IPAddress LocalIpAddress,
    IPAddress SubnetMask,
    IPAddress BroadcastAddress,
    IReadOnlyList<IPAddress> ScanTargets);
