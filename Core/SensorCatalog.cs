using LibreHardwareMonitor.Hardware;

namespace TempMonitor.Core;

public readonly record struct SensorEntry(
    ISensor Sensor,
    string Name,
    string Id,
    Group Group,
    string GroupLabel,
    string Hardware,
    int HardwareIndex);

public readonly record struct AllEntry(
    ISensor Sensor,
    string Name,
    string GroupLabel,
    string Hardware);

public sealed record HardwareEntry(string Name, string ShortName, Group Group, string GroupLabel);

public sealed class SensorCatalog
{
    public readonly SensorEntry[] Temps;
    public readonly SensorEntry[] Fans;
    public readonly SensorEntry[] Loads;
    public readonly AllEntry[] All;
    public readonly HardwareEntry[] Hardware;

    public SensorCatalog(Computer computer)
    {
        var hwList = new List<HardwareEntry>();
        var hwMap = new Dictionary<IHardware, int>();
        foreach (var hw in computer.Hardware)
            CollectHardware(hw, hwList, hwMap);

        var temps = new List<SensorEntry>();
        var fans = new List<SensorEntry>();
        var loads = new List<SensorEntry>();
        var all = new List<AllEntry>();

        foreach (var hw in computer.Hardware)
            Walk(hw, temps, fans, loads, all, hwMap);

        Temps = temps.ToArray();
        Fans = fans.ToArray();
        Loads = loads.ToArray();
        All = all.ToArray();
        Hardware = hwList.ToArray();
    }

    private static bool IsStaticThreshold(string name)
        => name is "Warning Temperature" or "Critical Temperature";

    private static bool CollectHardware(IHardware hw, List<HardwareEntry> list, Dictionary<IHardware, int> map)
    {
        bool selfHasTemp = false;
        foreach (var s in hw.Sensors)
        {
            if (s.SensorType == SensorType.Temperature && !IsStaticThreshold(s.Name ?? string.Empty))
            {
                selfHasTemp = true;
                break;
            }
        }

        int? index = null;
        if (selfHasTemp)
        {
            index = list.Count;
            map[hw] = index.Value;
            list.Add(MakeEntry(hw));
        }

        bool subHasTemp = false;
        foreach (var sub in hw.SubHardware)
            subHasTemp |= CollectHardware(sub, list, map);

        if (!selfHasTemp && subHasTemp)
        {
            index = list.Count;
            map[hw] = index.Value;
            list.Add(MakeEntry(hw));
        }

        return selfHasTemp || subHasTemp;
    }

    private static HardwareEntry MakeEntry(IHardware hw)
    {
        var group = Groups.Of(hw.HardwareType);
        var name = hw.Name ?? string.Empty;
        return new HardwareEntry(name, Groups.ShortName(name), group, Groups.Label(group));
    }

    private static void Walk(
        IHardware hw,
        List<SensorEntry> temps,
        List<SensorEntry> fans,
        List<SensorEntry> loads,
        List<AllEntry> all,
        Dictionary<IHardware, int> hwMap)
    {
        var group = Groups.Of(hw.HardwareType);
        var label = Groups.Label(group);
        var hwName = hw.Name ?? string.Empty;
        int hwIndex = hwMap.TryGetValue(hw, out var i) ? i : -1;

        foreach (var s in hw.Sensors)
        {
            var name = s.Name ?? s.Identifier?.ToString() ?? string.Empty;
            var entry = new SensorEntry(s, name, s.Identifier?.ToString() ?? string.Empty, group, label, hwName, hwIndex);

            if (s.SensorType == SensorType.Temperature && !IsStaticThreshold(name))
                temps.Add(entry);
            else if (s.SensorType == SensorType.Fan)
                fans.Add(entry);
            else if (s.SensorType == SensorType.Load)
                loads.Add(entry);

            all.Add(new AllEntry(s, name, label, hwName));
        }

        foreach (var sub in hw.SubHardware)
            Walk(sub, temps, fans, loads, all, hwMap);
    }
}