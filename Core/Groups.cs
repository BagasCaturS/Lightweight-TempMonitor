using LibreHardwareMonitor.Hardware;

namespace TempMonitor.Core;

public enum Group : byte
{
    Cpu = 0,
    Gpu = 1,
    Storage = 2,
    Motherboard = 3,
    Other = 4
}

public enum IconLevel : byte
{
    Ok = 0,
    Warm = 1,
    Hot = 2
}

public static class Groups
{
    public const int Count = 5;

    public static Group Of(HardwareType t) => t switch
    {
        HardwareType.Cpu => Group.Cpu,
        HardwareType.GpuNvidia or HardwareType.GpuAmd => Group.Gpu,
        HardwareType.Storage => Group.Storage,
        HardwareType.Motherboard or HardwareType.SuperIO or HardwareType.EmbeddedController => Group.Motherboard,
        _ => Group.Other
    };

    public static string Label(Group g) => g switch
    {
        Group.Cpu => "CPU",
        Group.Gpu => "GPU",
        Group.Storage => "SSD",
        Group.Motherboard => "MB",
        _ => "Other"
    };

    public static string Unit(SensorType t) => t switch
    {
        SensorType.Temperature => "°C",
        SensorType.Fan => "RPM",
        SensorType.Load => "%",
        SensorType.Voltage => "V",
        SensorType.Clock => "MHz",
        SensorType.Power => "W",
        SensorType.Flow => "L/h",
        SensorType.Control or SensorType.Level => "%",
        SensorType.Throughput => "B/s",
        SensorType.TimeSpan => "s",
        SensorType.Noise => "dB",
        _ => ""
    };

    private static readonly string[] BrandTokens =
    {
        "adata", "samsung", "kingston", "western digital", "wdc", "sandisk", "crucial",
        "intel", "amd", "nvidia", "nuvoton", "toshiba", "seagate", "corsair", "patriot",
        "sk hynix", "kioxia", "hynix", "micron", "sabrent", "lexar", "gigabyte", "msi",
        "asus", "asustek", "aorus", "pny", "transcend", "mushkin", "silicon power",
        "teamgroup", "team", "addlink", "netac", "goodram", "galax", "zotac",
        "powercolor", "xfx", "sapphire", "evga", "lenovo", "dell", "gigabyte", "hp",
        "generic", "unknown", "n/a"
    };

    public static string ShortName(string hwName)
    {
        var words = hwName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return hwName;

        int start = 0;
        while (start < words.Length)
        {
            bool matched = false;
            foreach (var brand in BrandTokens)
            {
                var bt = brand.Split(' ');
                if (start + bt.Length > words.Length) continue;
                bool eq = true;
                for (int i = 0; i < bt.Length; i++)
                {
                    if (!string.Equals(words[start + i], bt[i], StringComparison.OrdinalIgnoreCase))
                    {
                        eq = false;
                        break;
                    }
                }
                if (eq)
                {
                    start += bt.Length;
                    matched = true;
                    break;
                }
            }
            if (!matched) break;
        }

        if (start >= words.Length) return hwName;
        return string.Join(' ', words, start, words.Length - start);
    }
}