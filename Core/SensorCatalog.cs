using LibreHardwareMonitor.Hardware;

namespace TempMonitor.Core;

public readonly record struct SensorEntry(
    ISensor Sensor,
    string Name,
    string Id,
    Group Group,
    string GroupLabel,
    string Hardware);

public readonly record struct AllEntry(
    ISensor Sensor,
    string Name,
    string GroupLabel,
    string Hardware);

public sealed class SensorCatalog
{
    public readonly SensorEntry[] Temps;
    public readonly SensorEntry[] Fans;
    public readonly SensorEntry[] Loads;
    public readonly AllEntry[] All;

    public SensorCatalog(Computer computer)
    {
        var temps = new List<SensorEntry>();
        var fans = new List<SensorEntry>();
        var loads = new List<SensorEntry>();
        var all = new List<AllEntry>();

        foreach (var hw in computer.Hardware)
            Walk(hw, temps, fans, loads, all);

        Temps = temps.ToArray();
        Fans = fans.ToArray();
        Loads = loads.ToArray();
        All = all.ToArray();
    }

    private static bool IsStaticThreshold(string name)
        => name is "Warning Temperature" or "Critical Temperature";

    private static void Walk(IHardware hw, List<SensorEntry> temps, List<SensorEntry> fans, List<SensorEntry> loads, List<AllEntry> all)
    {
        var group = Groups.Of(hw.HardwareType);
        var label = Groups.Label(group);
        var hwName = hw.Name ?? string.Empty;

        foreach (var s in hw.Sensors)
        {
            var name = s.Name ?? s.Identifier?.ToString() ?? string.Empty;
            var entry = new SensorEntry(s, name, s.Identifier?.ToString() ?? string.Empty, group, label, hwName);

            if (s.SensorType == SensorType.Temperature && !IsStaticThreshold(name))
                temps.Add(entry);
            else if (s.SensorType == SensorType.Fan)
                fans.Add(entry);
            else if (s.SensorType == SensorType.Load)
                loads.Add(entry);

            all.Add(new AllEntry(s, name, label, hwName));
        }

        foreach (var sub in hw.SubHardware)
            Walk(sub, temps, fans, loads, all);
    }
}