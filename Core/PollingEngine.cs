using System.Text;
using LibreHardwareMonitor.Hardware;
using TempMonitor.Ui;

namespace TempMonitor.Core;

public sealed class PollingEngine : IDisposable
{
    public const int HistoryPoints = 300;

    private readonly Computer _computer;
    private readonly SensorCatalog _catalog;
    private readonly AlertEngine _alerts;

    private System.Threading.Timer? _timer;
    private int _ticking;
    private int _intervalMs = 2000;
    private float[] _values = Array.Empty<float>();
    private readonly float[] _groupMax = new float[Groups.Count];
    private readonly float[] _groupPeak = new float[Groups.Count];
    private readonly float[] _groupFan = new float[Groups.Count];
    private readonly float[] _groupLoad = new float[Groups.Count];
    private readonly float[] _history = new float[Groups.Count * HistoryPoints];
    private int _historyIdx;
    private int _historyCount;
    private readonly StringBuilder _sb = new(160);

    private string _lastTooltip = string.Empty;
    private int _lastIconKey = -1;

    public event Action<string, int>? UiChanged;

    public int HistoryCount => _historyCount;
    public int HistoryIndex => _historyIdx;
    public float[] History => _history;

    public PollingEngine(Computer computer, SensorCatalog catalog, AlertEngine alerts)
    {
        _computer = computer;
        _catalog = catalog;
        _alerts = alerts;
    }

    public void Start() => RestartTimer(_intervalMs);

    public void SetInterval(int intervalMs)
    {
        _intervalMs = Math.Max(250, intervalMs);
        RestartTimer(_intervalMs);
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private void RestartTimer(int intervalMs)
    {
        _timer?.Dispose();
        _timer = new System.Threading.Timer(Tick, null, intervalMs, intervalMs);
    }

    private void Tick(object? state)
    {
        if (Interlocked.CompareExchange(ref _ticking, 1, 0) != 0) return;

        try
        {
            foreach (var hw in _computer.Hardware)
                UpdateHardware(hw);

            var entries = _catalog.Temps;
            int n = entries.Length;

            if (_values.Length < n) _values = new float[n];
            for (int i = 0; i < n; i++)
            {
                float v = entries[i].Sensor.Value ?? float.NaN;
                _values[i] = v > 0 ? v : float.NaN;
            }

            Array.Fill(_groupMax, float.NaN);
            Array.Fill(_groupPeak, float.NaN);
            bool any = false;
            for (int i = 0; i < n; i++)
            {
                float v = _values[i];
                if (float.IsNaN(v)) continue;
                any = true;
                int gi = (int)entries[i].Group;
                if (float.IsNaN(_groupMax[gi]) || v > _groupMax[gi]) _groupMax[gi] = v;
            }

            for (int i = 0; i < n; i++)
            {
                float p = entries[i].Sensor.Max ?? float.NaN;
                if (p <= 0 || float.IsNaN(p)) continue;
                int gi = (int)entries[i].Group;
                if (float.IsNaN(_groupPeak[gi]) || p > _groupPeak[gi]) _groupPeak[gi] = p;
            }

            FillGroupMax(_groupFan, _catalog.Fans);
            FillGroupMax(_groupLoad, _catalog.Loads);

            if (!any)
            {
                Publish(TrayIconRenderer.GrayKey, "No temperature sensors found");
                return;
            }

            PushHistory();

            _alerts.Evaluate(entries, _values, n);

            int hotGroup = (int)entries[_alerts.HottestIndex].Group;
            var level = _alerts.AnyAlerting ? IconLevel.Hot : _alerts.AnyWarm ? IconLevel.Warm : IconLevel.Ok;
            int key = hotGroup * 3 + (int)level;

            _sb.Clear();
            for (int gi = 0; gi < Groups.Count; gi++)
            {
                float v = _groupMax[gi];
                if (float.IsNaN(v)) continue;
                _sb.Append(Groups.Label((Group)gi)).Append("  ").Append((int)v).Append("°C");
                float pk = _groupPeak[gi];
                if (!float.IsNaN(pk) && pk > v) _sb.Append(" ↑").Append((int)pk);
                float ld = _groupLoad[gi];
                if (!float.IsNaN(ld)) _sb.Append("  ").Append((int)ld).Append('%');
                float fn = _groupFan[gi];
                if (!float.IsNaN(fn)) _sb.Append("  ").Append((int)fn).Append("RPM");
                _sb.Append('\n');
            }
            if (_sb.Length > 0) _sb.Length--;

            if (_sb.Length > 115)
            {
                _sb.Clear();
                for (int gi = 0; gi < Groups.Count; gi++)
                {
                    float v = _groupMax[gi];
                    if (float.IsNaN(v)) continue;
                    _sb.Append(Groups.Label((Group)gi)).Append("  ").Append((int)v).Append("°C").Append('\n');
                }
                if (_sb.Length > 0) _sb.Length--;
            }

            Publish(key, _sb.ToString());
        }
        catch (Exception)
        {
            Publish(TrayIconRenderer.GrayKey, "Sensor read error");
        }
        finally
        {
            Interlocked.Exchange(ref _ticking, 0);
        }
    }

    private void FillGroupMax(float[] target, SensorEntry[] entries)
    {
        Array.Fill(target, float.NaN);
        for (int i = 0; i < entries.Length; i++)
        {
            float v = entries[i].Sensor.Value ?? float.NaN;
            if (float.IsNaN(v)) continue;
            int gi = (int)entries[i].Group;
            if (float.IsNaN(target[gi]) || v > target[gi]) target[gi] = v;
        }
    }

    private void PushHistory()
    {
        int h = _historyIdx;
        _historyIdx = (h + 1) % HistoryPoints;
        if (_historyCount < HistoryPoints) _historyCount++;
        for (int gi = 0; gi < Groups.Count; gi++)
            _history[gi * HistoryPoints + h] = _groupMax[gi];
    }

    private static void UpdateHardware(IHardware hw)
    {
        hw.Update();
        foreach (var sub in hw.SubHardware)
            UpdateHardware(sub);
    }

    private void Publish(int key, string tooltip)
    {
        if (key == _lastIconKey && tooltip == _lastTooltip) return;
        _lastIconKey = key;
        _lastTooltip = tooltip;
        UiChanged?.Invoke(tooltip, key);
    }
}