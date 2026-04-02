namespace WakeOnLan.Models;

public sealed class DiscoveredDevice
{
    public string HostName { get; set; } = string.Empty;

    public string IpAddress { get; set; } = string.Empty;

    public string MacAddress { get; set; } = string.Empty;

    public DateTime DiscoveredAt { get; set; } = DateTime.Now;
}
