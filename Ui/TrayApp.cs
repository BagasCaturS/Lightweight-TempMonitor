using System.Media;
using Microsoft.Win32;
using TempMonitor.Config;
using TempMonitor.Core;

namespace TempMonitor.Ui;

public sealed class TrayApp : ApplicationContext
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValue = "TempMonitor";

    private readonly NotifyIcon _icon;
    private readonly TrayIconRenderer _renderer;
    private readonly SynchronizationContext _ui;
    private readonly string _cfgPath;
    private readonly AppConfig _cfg;
    private readonly PollingEngine _engine;
    private readonly SensorCatalog _catalog;
    private readonly AlertEngine _alerts;
    private readonly ToolStripMenuItem _startupItem;
    private DetailsForm? _details;

    public TrayApp(string cfgPath, AppConfig cfg, PollingEngine engine, SensorCatalog catalog, AlertEngine alerts)
    {
        _cfgPath = cfgPath;
        _cfg = cfg;
        _engine = engine;
        _catalog = catalog;
        _alerts = alerts;
        _ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _renderer = new TrayIconRenderer();

        engine.UiChanged += OnUiChanged;
        alerts.AlertRaised += OnAlertRaised;
        alerts.AlertCleared += OnAlertCleared;

        _icon = new NotifyIcon
        {
            Icon = _renderer.Get(TrayIconRenderer.GrayKey),
            Text = "Temp Monitor",
            Visible = true
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Settings...", null, (_, _) => ShowSettings());
        menu.Items.Add("Details", null, (_, _) => ShowDetails());
        _startupItem = new ToolStripMenuItem("Run at startup", null, (_, _) => ToggleStartup())
        {
            Checked = IsStartupEnabled()
        };
        menu.Items.Add(_startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quit", null, (_, _) => ExitThread());

        _icon.ContextMenuStrip = menu;
        _icon.DoubleClick += (_, _) => ShowDetails();

        _engine.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _engine.Dispose();
            _icon.Visible = false;
            _icon.Dispose();
            _renderer.Dispose();
            _details?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void OnUiChanged(string tooltip, int iconKey)
    {
        _ui.Post(_ =>
        {
            _icon.Icon = _renderer.Get(iconKey);
            _icon.Text = tooltip.Length > 127 ? tooltip[..127] : tooltip;
        }, null);
    }

    private void OnAlertRaised(AlertInfo a)
    {
        _ui.Post(_ =>
        {
            if (_cfg.AlertBeep) SystemSounds.Exclamation.Play();
            _icon.ShowBalloonTip(
                5000,
                "Temperature Alert",
                $"{a.GroupLabel}: {a.Name} {a.Temp:0}°C exceeds {a.Threshold:0}°C",
                ToolTipIcon.Warning);
        }, null);
    }

    private void OnAlertCleared(AlertInfo a)
    {
        _ui.Post(_ => _icon.ShowBalloonTip(
            3000,
            "Temperature Normal",
            $"{a.GroupLabel}: {a.Name} back to {a.Temp:0}°C",
            ToolTipIcon.Info), null);
    }

    private void ShowDetails()
    {
        if (_details is { IsDisposed: false })
        {
            if (!_details.Visible) _details.Show();
            _details.Activate();
            return;
        }

        _details = new DetailsForm(_catalog, _engine, _alerts);
        _details.Show();
    }

    private void ShowSettings()
    {
        using var form = new SettingsForm(_cfgPath, _cfg, _catalog, _engine, _alerts);
        form.ShowDialog();
    }

    private void ToggleStartup()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (_startupItem.Checked)
        {
            key.DeleteValue(RunValue, false);
            _startupItem.Checked = false;
        }
        else
        {
            key.SetValue(RunValue, "\"" + Application.ExecutablePath + "\"");
            _startupItem.Checked = true;
        }
    }

    private static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(RunValue) is string;
    }
}