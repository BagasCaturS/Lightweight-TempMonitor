using TempMonitor.Config;

namespace TempMonitor.Core;

public readonly record struct AlertInfo(string Id, string Name, string GroupLabel, float Temp, float Threshold);

public sealed class AlertEngine
{
    private sealed class SensorState
    {
        public bool Alerting;
        public long LastNotifiedMs;
    }

    private float _def = 85;
    private float _cpu = 85;
    private float _gpu = 85;
    private float _storage = 60;
    private float _mb = 70;
    private float _hysteresis = 5;
    private float _warnMargin = 10;
    private long _cooldownMs = 600_000;

    private readonly Dictionary<string, SensorState> _states = new();
    private readonly Dictionary<string, float> _perSensor = new();

    public bool AnyAlerting { get; private set; }
    public bool AnyWarm { get; private set; }
    public int HottestIndex { get; private set; } = -1;
    public float HottestTemp { get; private set; } = float.NaN;

    public event Action<AlertInfo>? AlertRaised;
    public event Action<AlertInfo>? AlertCleared;

    public void ApplyConfig(AppConfig cfg)
    {
        _def = cfg.Thresholds.Default;
        _cpu = cfg.Thresholds.Cpu;
        _gpu = cfg.Thresholds.Gpu;
        _storage = cfg.Thresholds.Storage;
        _mb = cfg.Thresholds.Motherboard;
        _hysteresis = cfg.HysteresisC;
        _warnMargin = cfg.WarnMarginC;
        _cooldownMs = (long)cfg.AlertCooldownMinutes * 60_000;

        _perSensor.Clear();
        foreach (var kv in cfg.Thresholds.PerSensor)
            _perSensor[kv.Key] = kv.Value;
    }

    public void Evaluate(SensorEntry[] entries, float[] values, int count)
    {
        AnyAlerting = false;
        AnyWarm = false;
        float hot = float.NaN;
        int hotIdx = -1;
        long now = Environment.TickCount64;

        for (int i = 0; i < count; i++)
        {
            float v = values[i];
            if (float.IsNaN(v)) continue;

            if (hotIdx < 0 || v > hot)
            {
                hot = v;
                hotIdx = i;
            }

            var e = entries[i];
            float th = ThresholdFor(e);

            if (v >= th - _warnMargin) AnyWarm = true;

            var st = GetState(e.Id);
            if (st.Alerting)
            {
                if (v <= th - _hysteresis)
                {
                    st.Alerting = false;
                    st.LastNotifiedMs = 0;
                    AlertCleared?.Invoke(new AlertInfo(e.Id, e.Name, e.GroupLabel, v, th));
                }
                if (v >= th) AnyAlerting = true;
            }
            else if (v >= th)
            {
                st.Alerting = true;
                if (now - st.LastNotifiedMs >= _cooldownMs)
                {
                    st.LastNotifiedMs = now;
                    AlertRaised?.Invoke(new AlertInfo(e.Id, e.Name, e.GroupLabel, v, th));
                }
                AnyAlerting = true;
            }
        }

        HottestIndex = hotIdx;
        HottestTemp = hot;
    }

    private float ThresholdFor(SensorEntry e)
    {
        if (_perSensor.TryGetValue(e.Id, out var t)) return t;
        return e.Group switch
        {
            Group.Cpu => _cpu,
            Group.Gpu => _gpu,
            Group.Storage => _storage,
            Group.Motherboard => _mb,
            _ => _def
        };
    }

    public float ThresholdForGroup(Group g) => g switch
    {
        Group.Cpu => _cpu,
        Group.Gpu => _gpu,
        Group.Storage => _storage,
        Group.Motherboard => _mb,
        _ => _def
    };

    private SensorState GetState(string id)
    {
        if (!_states.TryGetValue(id, out var st))
        {
            st = new SensorState();
            _states[id] = st;
        }
        return st;
    }
}