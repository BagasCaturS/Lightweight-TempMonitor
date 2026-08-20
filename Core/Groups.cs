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
}