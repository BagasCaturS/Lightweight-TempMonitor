namespace TempMonitor.Config;

public sealed class AppConfig
{
    public int PollIntervalMs { get; set; } = 2000;
    public int AlertCooldownMinutes { get; set; } = 10;
    public float HysteresisC { get; set; } = 5;
    public float WarnMarginC { get; set; } = 10;
    public bool AlertBeep { get; set; } = true;
    public GroupFlags Groups { get; set; } = new();
    public ThresholdConfig Thresholds { get; set; } = new();
}

public sealed class GroupFlags
{
    public bool Cpu { get; set; } = true;
    public bool Gpu { get; set; } = true;
    public bool Storage { get; set; } = true;
    public bool Motherboard { get; set; } = true;
    public bool Controller { get; set; } = true;
}

public sealed class ThresholdConfig
{
    public float Default { get; set; } = 85;
    public float Cpu { get; set; } = 85;
    public float Gpu { get; set; } = 85;
    public float Storage { get; set; } = 60;
    public float Motherboard { get; set; } = 70;
    public Dictionary<string, float> PerSensor { get; set; } = new();
}