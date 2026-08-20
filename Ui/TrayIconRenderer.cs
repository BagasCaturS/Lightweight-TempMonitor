using System.Runtime.InteropServices;
using TempMonitor.Core;

namespace TempMonitor.Ui;

public sealed class TrayIconRenderer : IDisposable
{
    public const int GrayKey = Groups.Count * 3;

    private static readonly Color[] Colors =
    {
        Color.FromArgb(0x1E, 0x9E, 0x4A),
        Color.FromArgb(0xD9, 0x9A, 0x06),
        Color.FromArgb(0xD6, 0x30, 0x31),
        Color.FromArgb(0x7F, 0x8C, 0x8D)
    };

    private readonly Icon[] _icons = new Icon[GrayKey + 1];

    public TrayIconRenderer()
    {
        for (int g = 0; g < Groups.Count; g++)
            for (int l = 0; l < 3; l++)
                _icons[g * 3 + l] = Build((Group)g, (IconLevel)l);
        _icons[GrayKey] = BuildGray();
    }

    public Icon Get(int key) => _icons[key];

    public void Dispose()
    {
        foreach (var icon in _icons)
            icon.Dispose();
    }

    private static Icon Build(Group g, IconLevel level)
    {
        using var bmp = new Bitmap(32, 32);
        using var gfx = Graphics.FromImage(bmp);
        gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        gfx.Clear(Color.Transparent);

        using (var bg = new SolidBrush(Colors[(int)level]))
            gfx.FillEllipse(bg, 1, 1, 30, 30);

        using var pen = new Pen(Color.White, 2.2f)
        {
            LineJoin = System.Drawing.Drawing2D.LineJoin.Round,
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round
        };

        switch (g)
        {
            case Group.Cpu: DrawCpu(gfx, pen); break;
            case Group.Gpu: DrawGpu(gfx, pen); break;
            case Group.Storage: DrawStorage(gfx, pen); break;
            case Group.Motherboard: DrawMotherboard(gfx, pen); break;
            default: DrawOther(gfx, pen); break;
        }

        return ToIcon(bmp);
    }

    private static Icon BuildGray()
    {
        using var bmp = new Bitmap(32, 32);
        using var gfx = Graphics.FromImage(bmp);
        gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        gfx.Clear(Color.Transparent);
        using (var bg = new SolidBrush(Colors[3]))
            gfx.FillEllipse(bg, 1, 1, 30, 30);
        using var pen = new Pen(Color.White, 2.2f)
        {
            LineJoin = System.Drawing.Drawing2D.LineJoin.Round,
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round
        };
        DrawOther(gfx, pen);
        return ToIcon(bmp);
    }

    private static void DrawCpu(Graphics g, Pen pen)
    {
        g.DrawRectangle(pen, 11, 11, 10, 10);
        for (int x = 11; x <= 21; x += 5)
        {
            g.DrawLine(pen, x, 7, x, 9);
            g.DrawLine(pen, x, 23, x, 25);
            g.DrawLine(pen, 7, x, 9, x);
            g.DrawLine(pen, 23, x, 25, x);
        }
    }

    private static void DrawGpu(Graphics g, Pen pen)
    {
        g.DrawRectangle(pen, 8, 13, 16, 6);
        g.DrawEllipse(pen, 17, 13, 9, 9);
        g.DrawArc(pen, 18, 14, 7, 7, 0, 110);
        g.DrawArc(pen, 18, 14, 7, 7, 130, 110);
        g.DrawArc(pen, 18, 14, 7, 7, 260, 110);
    }

    private static void DrawStorage(Graphics g, Pen pen)
    {
        g.DrawRectangle(pen, 6, 11, 20, 10);
        g.DrawLine(pen, 9, 16, 16, 16);
        g.DrawEllipse(pen, 22, 15, 2.5f, 2.5f);
    }

    private static void DrawMotherboard(Graphics g, Pen pen)
    {
        g.DrawRectangle(pen, 8, 8, 16, 16);
        g.DrawLine(pen, 11, 16, 16, 16);
        g.DrawRectangle(pen, 18, 12, 4, 4);
        g.DrawRectangle(pen, 18, 19, 4, 3);
    }

    private static void DrawOther(Graphics g, Pen pen)
    {
        g.DrawLine(pen, 16, 8, 16, 20);
        g.DrawLine(pen, 16, 8, 14, 11);
        g.DrawEllipse(pen, 13, 19, 6, 6);
    }

    private static Icon ToIcon(Bitmap bmp)
    {
        var h = bmp.GetHicon();
        try
        {
            return (Icon)Icon.FromHandle(h).Clone();
        }
        finally
        {
            DestroyIcon(h);
        }
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);
}