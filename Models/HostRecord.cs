namespace WakeOnLan.Models;

public sealed class HostRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string HostName { get; set; } = string.Empty;

    public string MacAddress { get; set; } = string.Empty;

    public string LastKnownIp { get; set; } = string.Empty;

    public string Remark { get; set; } = string.Empty;

    public DateTime? LastSeenAt { get; set; }

    public DateTime? LastWakeAt { get; set; }
}
