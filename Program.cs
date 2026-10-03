using System.Security.Principal;
using LibreHardwareMonitor.Hardware;
using TempMonitor.Config;
using TempMonitor.Core;
using TempMonitor.Ui;

namespace TempMonitor;

static class Program
{
    private static void Log(string msg)
    {
        try
        {
            File.AppendAllText(
                Path.Combine(AppContext.BaseDirectory, "startup.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}\n");
        }
        catch { }
    }

    private static bool IsElevated()
    {
        try
        {
            return new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }

    [STAThread]
    static void Main()
    {
        Log($"starting from {AppContext.BaseDirectory}, elevated={IsElevated()}");

        bool created;
        using var mutex = new Mutex(true, @"Local\TempMonitorMutex", out created);
        if (!created)
        {
            Log("another instance running, exiting");
            return;
        }

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
            catch { }
        };

        string cfgPath = Path.Combine(AppContext.BaseDirectory, "settings.json");
        var cfg = ConfigManager.Load(cfgPath);

        // Log current Run value for diagnostic
        try
        {
            using var runKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            var val = runKey?.GetValue("TempMonitor") as string;
            if (!string.IsNullOrEmpty(val))
                Log($"run value = {val}");
        }
        catch { }

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
            Log($"sensors ok, {catalog.Temps.Length} temp sensors");
        }
        catch (Exception ex)
        {
            Log($"init failed: {ex.Message}");
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