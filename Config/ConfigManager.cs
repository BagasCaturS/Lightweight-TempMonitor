using System.Text.Json;

namespace TempMonitor.Config;

public static class ConfigManager
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static AppConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            var defaults = new AppConfig();
            Save(path, defaults);
            return defaults;
        }

        try
        {
            var cfg = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(path), Options);
            return cfg ?? new AppConfig();
        }
        catch (JsonException)
        {
            return new AppConfig();
        }
    }

    public static void Save(string path, AppConfig cfg)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(cfg, Options));
    }
}