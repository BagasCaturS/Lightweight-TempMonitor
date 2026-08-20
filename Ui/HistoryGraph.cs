using System.Drawing.Drawing2D;
using TempMonitor.Core;

namespace TempMonitor.Ui;

public sealed class HistoryGraph : Control
{
    private static readonly Color[] GroupColors =
    {
        Color.FromArgb(0x2E, 0x86, 0xDE),
        Color.FromArgb(0x9B, 0x59, 0xB6),
        Color.FromArgb(0x1A, 0xBC, 0x9C),
        Color.FromArgb(0xF3, 0x9C, 0x12),
        Color.FromArgb(0x95, 0xA5, 0xA6)
    };

    private const float LeftPad = 38f;
    private const float EdgePad = 8f;

    private readonly PollingEngine _engine;
    private readonly AlertEngine _alerts;
    private readonly Font _labelFont;

    public HistoryGraph(PollingEngine engine, AlertEngine alerts)
    {
        _engine = engine;
        _alerts = alerts;
        _labelFont = new Font(Font.FontFamily, 8f);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.White;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _labelFont.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var hist = _engine.History;
        int count = _engine.HistoryCount;
        int pts = PollingEngine.HistoryPoints;
        int idx = _engine.HistoryIndex;

        if (hist == null || count < 2 || Width < 60 || Height < 30)
        {
            TextRenderer.DrawText(g, "collecting data...", Font, new Point((int)LeftPad, Height / 2 - 10), Color.Gray);
            return;
        }

        float min = float.MaxValue, max = float.MinValue;
        var hasData = new bool[Groups.Count];
        for (int gi = 0; gi < Groups.Count; gi++)
        {
            for (int k = 0; k < count; k++)
            {
                float v = hist[gi * pts + ((idx - count + 1 + k + pts) % pts)];
                if (float.IsNaN(v)) continue;
                hasData[gi] = true;
                if (v < min) min = v;
                if (v > max) max = v;
            }
        }

        if (max <= min)
        {
            min = 20;
            max = 80;
        }

        min = Math.Max(0, min - 5);
        max += 5;

        float plotW = Width - LeftPad - EdgePad;
        float plotH = Height - 2 * EdgePad;

        float Y(float v) => Height - EdgePad - (v - min) / (max - min) * plotH;
        float X(int i) => LeftPad + (float)i / (count - 1) * plotW;

        var ticks = ComputeTicks(min, max, 6);
        if (ticks != null)
        {
            using var gridPen = new Pen(Color.FromArgb(0xE2, 0xE2, 0xE2));
            for (int i = 0; i < ticks.Length; i++)
            {
                float ty = Y(ticks[i]);
                g.DrawLine(gridPen, LeftPad, ty, Width - EdgePad, ty);
                string label = ticks[i] >= 1
                    ? $"{Math.Round(ticks[i]):0}°C"
                    : $"{ticks[i]:0.0}°C";
                TextRenderer.DrawText(g, label, _labelFont, new Point(2, (int)ty - 8), Color.Gray);
            }
        }

        for (int gi = 0; gi < Groups.Count; gi++)
        {
            if (!hasData[gi]) continue;
            float th = _alerts.ThresholdForGroup((Group)gi);
            using var pen = new Pen(Color.FromArgb(80, GroupColors[gi]))
            {
                DashStyle = DashStyle.Dash,
                DashPattern = new float[] { 4f, 3f }
            };
            float ty = Y(th);
            g.DrawLine(pen, LeftPad, ty, Width - EdgePad, ty);
        }

        var legendLabels = new List<string>();
        var legendColors = new List<Color>();

        for (int gi = 0; gi < Groups.Count; gi++)
        {
            if (!hasData[gi]) continue;
            var points = new List<PointF>(count);
            for (int k = 0; k < count; k++)
            {
                float v = hist[gi * pts + ((idx - count + 1 + k + pts) % pts)];
                if (float.IsNaN(v)) continue;
                points.Add(new PointF(X(k), Y(v)));
            }
            if (points.Count < 2) continue;
            using var pen = new Pen(GroupColors[gi], 1.6f);
            g.DrawLines(pen, points.ToArray());
            legendLabels.Add(Groups.Label((Group)gi));
            legendColors.Add(GroupColors[gi]);
        }

        float lx = LeftPad;
        for (int i = 0; i < legendLabels.Count; i++)
        {
            var size = TextRenderer.MeasureText(legendLabels[i], Font);
            using var brush = new SolidBrush(legendColors[i]);
            g.FillRectangle(brush, lx, EdgePad - 2, 10, 10);
            TextRenderer.DrawText(g, legendLabels[i], Font, new Point((int)lx + 14, (int)EdgePad - 5), Color.DimGray);
            lx += 14 + size.Width + 14;
        }
    }

    private static float[]? ComputeTicks(float min, float max, int target)
    {
        float raw = (max - min) / target;
        if (raw <= 0 || float.IsInfinity(raw)) return null;

        float mag = MathF.Pow(10, MathF.Floor(MathF.Log10(raw)));
        float norm = raw / mag;
        float nice = norm < 1.5f ? 1f : norm < 3f ? 2f : norm < 7f ? 5f : 10f;
        float step = nice * mag;

        float first = MathF.Ceiling(min / step) * step;
        int count = (int)((max - first) / step) + 1;
        if (count <= 0 || count > 20) return null;

        var ticks = new float[count];
        for (int i = 0; i < count; i++)
            ticks[i] = first + i * step;
        return ticks;
    }
}