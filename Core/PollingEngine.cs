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

    private readonly float[] _hwMax;
    private readonly float[] _hwPeak;
    private readonly float[] _hwFan;
    private readonly float[] _hwLoad;
    private readonly float[] _history;
    private int _historyIdx;
    private int _historyCount;
    private readonly StringBuilder _sb = new(192);

    private string _lastTooltip = string.Empty;
    private int _lastIconKey = -1;

    public event Action<string, int>? UiChanged;

    public int HistoryCount => _historyCount;
    public int HistoryIndex => _historyIdx;
    public float[] History => _history;
    public int HardwareCount => _catalog.Hardware.Length;
    public HardwareEntry[] Hardware => _catalog.Hardware;

    public PollingEngine(Computer computer, SensorCatalog catalog, AlertEngine alerts)
    {
        _computer = computer;
        _catalog = catalog;
        _alerts = alerts;

        int hwCount = catalog.Hardware.Length;
        _hwMax = new float[hwCount];
        _hwPeak = new float[hwCount];
        _hwFan = new float[hwCount];
        _hwLoad = new float[hwCount];
        _history = new float[hwCount * HistoryPoints];
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

            int hwCount = _hwMax.Length;
            Array.Fill(_hwMax, float.NaN);
            Array.Fill(_hwPeak, float.NaN);

            bool any = false;
            for (int i = 0; i < n; i++)
            {
                float v = _values[i];
                if (float.IsNaN(v)) continue;
                any = true;
                int hi = entries[i].HardwareIndex;
                if (hi >= 0 && (float.IsNaN(_hwMax[hi]) || v > _hwMax[hi])) _hwMax[hi] = v;
            }

            for (int i = 0; i < n; i++)
            {
                float p = entries[i].Sensor.Max ?? float.NaN;
                if (p <= 0 || float.IsNaN(p)) continue;
                int hi = entries[i].HardwareIndex;
                if (hi >= 0 && (float.IsNaN(_hwPeak[hi]) || p > _hwPeak[hi])) _hwPeak[hi] = p;
            }

            FillHardwareMax(_hwFan, _catalog.Fans);
            FillHardwareMax(_hwLoad, _catalog.Loads);

            if (!any)
            {
                Publish(TrayIconRenderer.GrayKey, "No temperature sensors found");
                return;
            }

            PushHistory();

            _alerts.Evaluate(entries, _values, n);

            int hotGroup = -1;
            float hotV = float.MinValue;
            for (int i = 0; i < hwCount; i++)
            {
                float v = _hwMax[i];
                if (float.IsNaN(v) || v <= hotV) continue;
                hotV = v;
                hotGroup = (int)_catalog.Hardware[i].Group;
            }
            if (hotGroup < 0) hotGroup = (int)entries[_alerts.HottestIndex].Group;

            var level = _alerts.AnyAlerting ? IconLevel.Hot : _alerts.AnyWarm ? IconLevel.Warm : IconLevel.Ok;
            int key = hotGroup * 3 + (int)level;

            var hardware = _catalog.Hardware;
            _sb.Clear();
            for (int i = 0; i < hwCount; i++)
            {
                float v = _hwMax[i];
                if (float.IsNaN(v)) continue;
                _sb.Append(hardware[i].ShortName).Append("  ").Append((int)v).Append("°C");
                float pk = _hwPeak[i];
                if (!float.IsNaN(pk) && pk > v) _sb.Append(" ↑").Append((int)pk);
                float ld = _hwLoad[i];
                if (!float.IsNaN(ld)) _sb.Append("  ").Append((int)ld).Append('%');
                float fn = _hwFan[i];
                if (!float.IsNaN(fn)) _sb.Append("  ").Append((int)fn).Append("RPM");
                _sb.Append('\n');
            }
            if (_sb.Length > 0) _sb.Length--;

            if (_sb.Length > 115)
            {
                _sb.Clear();
                for (int i = 0; i < hwCount; i++)
                {
                    float v = _hwMax[i];
                    if (float.IsNaN(v)) continue;
                    _sb.Append(hardware[i].ShortName).Append("  ").Append((int)v).Append("°C").Append('\n');
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

    private void FillHardwareMax(float[] target, SensorEntry[] entries)
    {
        Array.Fill(target, float.NaN);
        for (int i = 0; i < entries.Length; i++)
        {
            float v = entries[i].Sensor.Value ?? float.NaN;
            if (float.IsNaN(v)) continue;
            int hi = entries[i].HardwareIndex;
            if (hi >= 0 && (float.IsNaN(target[hi]) || v > target[hi])) target[hi] = v;
        }
    }

    private void PushHistory()
    {
        int h = _historyIdx;
        _historyIdx = (h + 1) % HistoryPoints;
        if (_historyCount < HistoryPoints) _historyCount++;
        int pts = HistoryPoints;
        for (int hi = 0; hi < _hwMax.Length; hi++)
            _history[hi * pts + h] = _hwMax[hi];
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