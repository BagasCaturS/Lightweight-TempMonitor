using LibreHardwareMonitor.Hardware;
using TempMonitor.Config;
using TempMonitor.Core;
using TempMonitor.Ui;

namespace TempMonitor;

static class Program
{
    [STAThread]
    static void Main()
    {
        bool created;
        using var mutex = new Mutex(true, @"Local\TempMonitorMutex", out created);
        if (!created) return;

        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
        {
            try
            {
                File.AppendAllText(
                    Path.Combine(AppContext.BaseDirectory, "error.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {e.Exception}\n\n");
            }
            catch
            {
            }
        };

        string cfgPath = Path.Combine(AppContext.BaseDirectory, "settings.json");
        var cfg = ConfigManager.Load(cfgPath);

        var computer = new Computer
        {
            IsCpuEnabled = cfg.Groups.Cpu,
            IsGpuEnabled = cfg.Groups.Gpu,
            IsStorageEnabled = cfg.Groups.Storage,
            IsMotherboardEnabled = cfg.Groups.Motherboard,
            IsControllerEnabled = cfg.Groups.Controller
        };

        SensorCatalog catalog;
        try
        {
            computer.Open();
            catalog = new SensorCatalog(computer);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Failed to initialize the sensor driver.\n\nTry running this program as administrator.\n\n" + ex.Message,
                "Temp Monitor",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        var alerts = new AlertEngine();
        alerts.ApplyConfig(cfg);

        using var engine = new PollingEngine(computer, catalog, alerts);
        using var tray = new TrayApp(cfgPath, cfg, engine, catalog, alerts);
        Application.Run(tray);
    }
}