using TempMonitor.Config;
using TempMonitor.Core;

namespace TempMonitor.Ui;

public sealed class SettingsForm : Form
{
    private readonly string _cfgPath;
    private readonly AppConfig _cfg;
    private readonly SensorCatalog _catalog;
    private readonly PollingEngine _engine;
    private readonly AlertEngine _alerts;
    private readonly Dictionary<string, float> _overrides;

    private readonly NumericUpDown _cpu = NewNum(20, 120, 1);
    private readonly NumericUpDown _gpu = NewNum(20, 120, 1);
    private readonly NumericUpDown _ssd = NewNum(20, 120, 1);
    private readonly NumericUpDown _mb = NewNum(20, 120, 1);
    private readonly NumericUpDown _other = NewNum(20, 120, 1);
    private readonly NumericUpDown _warnMargin = NewNum(0, 50, 1);
    private readonly NumericUpDown _hysteresis = NewNum(0, 50, 1);
    private readonly NumericUpDown _cooldown = NewNum(0, 120, 1);
    private readonly NumericUpDown _interval = NewNum(250, 60000, 250);
    private readonly CheckBox _beep = new() { Text = "Play sound when an alert fires", AutoSize = true };
    private readonly ComboBox _sensorCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 320 };
    private readonly NumericUpDown _sensorOverride = NewNum(0, 120, 1);
    private readonly Label _overrideStatus = new() { AutoSize = true, ForeColor = Color.DimGray };

    private SensorEntry[] _sensors = Array.Empty<SensorEntry>();

    public SettingsForm(string cfgPath, AppConfig cfg, SensorCatalog catalog, PollingEngine engine, AlertEngine alerts)
    {
        _cfgPath = cfgPath;
        _cfg = cfg;
        _catalog = catalog;
        _engine = engine;
        _alerts = alerts;
        _overrides = new Dictionary<string, float>(cfg.Thresholds.PerSensor);

        Text = "Temp Monitor - Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);

        var grid = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            ColumnStyles = { new ColumnStyle(SizeType.AutoSize), new ColumnStyle(SizeType.Absolute, 90) }
        };

        AddRow(grid, "CPU threshold (°C)", _cpu);
        AddRow(grid, "GPU threshold (°C)", _gpu);
        AddRow(grid, "SSD threshold (°C)", _ssd);
        AddRow(grid, "Motherboard threshold (°C)", _mb);
        AddRow(grid, "Other threshold (°C)", _other);
        AddRow(grid, "Warn margin (°C)", _warnMargin);
        AddRow(grid, "Hysteresis (°C)", _hysteresis);
        AddRow(grid, "Alert cooldown (min)", _cooldown);
        AddRow(grid, "Poll interval (ms)", _interval);
        grid.Controls.Add(_beep, 0, grid.RowCount++);
        grid.SetColumnSpan(_beep, 2);

        var sensorBox = new GroupBox
        {
            Text = "Per-sensor threshold override",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(8)
        };
        var sensorLayout = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            FlowDirection = FlowDirection.TopDown
        };
        var row1 = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        var setBtn = new Button { Text = "Set", Width = 60 };
        var clearBtn = new Button { Text = "Clear", Width = 60 };
        setBtn.Click += (_, _) => SetOverride();
        clearBtn.Click += (_, _) => ClearOverride();
        row1.Controls.Add(_sensorCombo);
        row1.Controls.Add(_sensorOverride);
        row1.Controls.Add(setBtn);
        row1.Controls.Add(clearBtn);
        sensorLayout.Controls.Add(row1);
        sensorLayout.Controls.Add(_overrideStatus);
        sensorBox.Controls.Add(sensorLayout);
        _sensorCombo.SelectedIndexChanged += (_, _) => UpdateOverrideStatus();

        var save = new Button { Text = "Save", Width = 80, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancel", Width = 80, DialogResult = DialogResult.Cancel };
        save.Click += (_, _) => SaveAndApply();

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Padding(0, 8, 0, 0)
        };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(save);

        var root = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        root.Controls.Add(grid);
        root.Controls.Add(sensorBox);
        root.Controls.Add(buttons);
        Controls.Add(root);

        LoadValues();
        LoadSensors();
        AcceptButton = save;
        CancelButton = cancel;
    }

    private void LoadValues()
    {
        _cpu.Value = Clamp(_cfg.Thresholds.Cpu, _cpu);
        _gpu.Value = Clamp(_cfg.Thresholds.Gpu, _gpu);
        _ssd.Value = Clamp(_cfg.Thresholds.Storage, _ssd);
        _mb.Value = Clamp(_cfg.Thresholds.Motherboard, _mb);
        _other.Value = Clamp(_cfg.Thresholds.Default, _other);
        _warnMargin.Value = Clamp(_cfg.WarnMarginC, _warnMargin);
        _hysteresis.Value = Clamp(_cfg.HysteresisC, _hysteresis);
        _cooldown.Value = Clamp(_cfg.AlertCooldownMinutes, _cooldown);
        _interval.Value = Clamp(_cfg.PollIntervalMs, _interval);
        _beep.Checked = _cfg.AlertBeep;
    }

    private void LoadSensors()
    {
        _sensors = _catalog.Temps;
        for (int i = 0; i < _sensors.Length; i++)
        {
            var e = _sensors[i];
            string suffix = _overrides.ContainsKey(e.Id) ? "  ★" : "";
            _sensorCombo.Items.Add($"[{e.GroupLabel}] {e.Name} ({e.Hardware}){suffix}");
        }
        if (_sensorCombo.Items.Count > 0) _sensorCombo.SelectedIndex = 0;
    }

    private void SetOverride()
    {
        if (_sensorCombo.SelectedIndex < 0) return;
        var e = _sensors[_sensorCombo.SelectedIndex];
        _overrides[e.Id] = (int)_sensorOverride.Value;
        RefreshComboItem(_sensorCombo.SelectedIndex);
        UpdateOverrideStatus();
    }

    private void ClearOverride()
    {
        if (_sensorCombo.SelectedIndex < 0) return;
        var e = _sensors[_sensorCombo.SelectedIndex];
        _overrides.Remove(e.Id);
        RefreshComboItem(_sensorCombo.SelectedIndex);
        UpdateOverrideStatus();
    }

    private void RefreshComboItem(int idx)
    {
        var e = _sensors[idx];
        string suffix = _overrides.ContainsKey(e.Id) ? "  ★" : "";
        _sensorCombo.Items[idx] = $"[{e.GroupLabel}] {e.Name} ({e.Hardware}){suffix}";
    }

    private void UpdateOverrideStatus()
    {
        if (_sensorCombo.SelectedIndex < 0)
        {
            _overrideStatus.Text = string.Empty;
            return;
        }
        var e = _sensors[_sensorCombo.SelectedIndex];
        float groupTh = _alerts.ThresholdForGroup(e.Group);
        if (_overrides.TryGetValue(e.Id, out var ov))
        {
            _sensorOverride.Value = Clamp(ov, _sensorOverride);
            _overrideStatus.Text = $"Override: {ov:0}°C  (group default {groupTh:0}°C)";
        }
        else
        {
            _sensorOverride.Value = Clamp(groupTh, _sensorOverride);
            _overrideStatus.Text = $"Group default: {groupTh:0}°C";
        }
    }

    private void SaveAndApply()
    {
        _cfg.Thresholds.Cpu = (int)_cpu.Value;
        _cfg.Thresholds.Gpu = (int)_gpu.Value;
        _cfg.Thresholds.Storage = (int)_ssd.Value;
        _cfg.Thresholds.Motherboard = (int)_mb.Value;
        _cfg.Thresholds.Default = (int)_other.Value;
        _cfg.WarnMarginC = (int)_warnMargin.Value;
        _cfg.HysteresisC = (int)_hysteresis.Value;
        _cfg.AlertCooldownMinutes = (int)_cooldown.Value;
        _cfg.PollIntervalMs = (int)_interval.Value;
        _cfg.AlertBeep = _beep.Checked;
        _cfg.Thresholds.PerSensor = new Dictionary<string, float>(_overrides);

        ConfigManager.Save(_cfgPath, _cfg);
        _alerts.ApplyConfig(_cfg);
        _engine.SetInterval(_cfg.PollIntervalMs);
    }

    private static NumericUpDown NewNum(decimal min, decimal max, decimal increment) => new()
    {
        Minimum = min,
        Maximum = max,
        Increment = increment,
        DecimalPlaces = 0,
        Width = 80
    };

    private static decimal Clamp(float v, NumericUpDown box)
        => Math.Clamp((decimal)v, box.Minimum, box.Maximum);

    private static void AddRow(TableLayoutPanel grid, string label, Control input)
    {
        var idx = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.Controls.Add(new Label { Text = label, TextAlign = ContentAlignment.MiddleLeft, AutoSize = true, Margin = new Padding(0, 4, 12, 4) }, 0, idx);
        grid.Controls.Add(input, 1, idx);
    }
}