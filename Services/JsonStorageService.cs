using System.Text.Json;
using WakeOnLan.Models;

namespace WakeOnLan.Services;

public sealed class JsonStorageService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public JsonStorageService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WakeOnLan");

        Directory.CreateDirectory(appDataPath);
        _filePath = Path.Combine(appDataPath, "hosts.json");
    }

    public IReadOnlyList<HostRecord> LoadHosts()
    {
        if (!File.Exists(_filePath))
        {
            return Array.Empty<HostRecord>();
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            var store = JsonSerializer.Deserialize<HostStore>(json, SerializerOptions);
            return store?.Hosts is { } hosts
                ? hosts
                : Array.Empty<HostRecord>();
        }
        catch
        {
            return Array.Empty<HostRecord>();
        }
    }

    public void SaveHosts(IEnumerable<HostRecord> hosts)
    {
        var store = new HostStore
        {
            Hosts = hosts.OrderBy(x => x.Name).ThenBy(x => x.HostName).ToList()
        };

        var json = JsonSerializer.Serialize(store, SerializerOptions);
        File.WriteAllText(_filePath, json);
    }

    public string GetStoragePath()
    {
        return _filePath;
    }
}
