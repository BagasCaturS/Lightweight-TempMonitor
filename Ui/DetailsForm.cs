using TempMonitor.Core;

namespace TempMonitor.Ui;

public sealed class DetailsForm : Form
{
    private readonly SensorCatalog _catalog;
    private readonly ListView _list;
    private readonly HistoryGraph _graph;
    private readonly System.Windows.Forms.Timer _timer;

    public DetailsForm(SensorCatalog catalog, PollingEngine engine, AlertEngine alerts)
    {
        _catalog = catalog;
        Text = "Temp Monitor - Details";
        Width = 680;
        Height = 640;
        StartPosition = FormStartPosition.CenterScreen;

        _graph = new HistoryGraph(engine, alerts)
        {
            Dock = DockStyle.Top,
            Height = 180
        };

        _list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = false
        };
        _list.Columns.Add("Group", 60);
        _list.Columns.Add("Hardware", 200);
        _list.Columns.Add("Sensor", 220);
        _list.Columns.Add("Value", 90);
        _list.Columns.Add("Min", 90);
        _list.Columns.Add("Max", 90);
        Controls.Add(_list);
        Controls.Add(_graph);

        _timer = new System.Windows.Forms.Timer { Interval = 5000 };
        _timer.Tick += (_, _) => { RefreshSensors(); _graph.Invalidate(); };

        Shown += (_, _) => _timer.Start();
        FormClosed += (_, _) => _timer.Stop();

        RefreshSensors();
    }

    private void RefreshSensors()
    {
        var all = _catalog.All;
        _list.BeginUpdate();
        try
        {
            var items = _list.Items;
            for (int i = 0; i < all.Length; i++)
            {
                var e = all[i];
                ListViewItem item;
                if (i >= items.Count)
                {
                    item = new ListViewItem(e.GroupLabel);
                    item.SubItems.Add(e.Hardware);
                    item.SubItems.Add(e.Name);
                    item.SubItems.Add("--");
                    item.SubItems.Add("--");
                    item.SubItems.Add("--");
                    item.Tag = e;
                    items.Add(item);
                }
                else
                {
                    item = items[i];
                    if (item.Tag is not AllEntry prev || prev != e)
                    {
                        item.Text = e.GroupLabel;
                        item.SubItems[1].Text = e.Hardware;
                        item.SubItems[2].Text = e.Name;
                        item.Tag = e;
                    }
                }
                item.SubItems[3].Text = Format(e.Sensor.Value, e.Sensor.SensorType);
                item.SubItems[4].Text = Format(e.Sensor.Min, e.Sensor.SensorType);
                item.SubItems[5].Text = Format(e.Sensor.Max, e.Sensor.SensorType);
            }
            while (items.Count > all.Length)
                items.RemoveAt(items.Count - 1);
        }
        finally
        {
            _list.EndUpdate();
        }
    }

    private static string Format(float? v, LibreHardwareMonitor.Hardware.SensorType type)
        => v.HasValue ? $"{v.Value:0.0} {Groups.Unit(type)}" : "--";
}