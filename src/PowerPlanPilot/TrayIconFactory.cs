using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace PowerPlanPilot;

internal static class TrayIconFactory
{
    public static Icon CreateIcon(int size = 64)
    {
        using var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var backgroundBrush = new LinearGradientBrush(
            new Rectangle(0, 0, size, size),
            Color.FromArgb(24, 119, 242),
            Color.FromArgb(0, 201, 167),
            LinearGradientMode.ForwardDiagonal);

        var inset = Scale(size, 6);
        using var backgroundPath = RoundedRectangle(
            new Rectangle(inset, inset, size - (inset * 2), size - (inset * 2)),
            Scale(size, 14));
        graphics.FillPath(backgroundBrush, backgroundPath);

        using var highlightPen = new Pen(Color.FromArgb(115, Color.White), Scale(size, 2));
        graphics.DrawPath(highlightPen, backgroundPath);

        DrawBolt(graphics, size);
        DrawGaugeArc(graphics, size);

        var handle = bitmap.GetHicon();
        try
        {
            using var sourceIcon = Icon.FromHandle(handle);
            return (Icon)sourceIcon.Clone();
        }
        finally
        {
            _ = DestroyIcon(handle);
        }
    }

    private static void DrawBolt(Graphics graphics, int size)
    {
        PointF[] bolt =
        [
            new(Scale(size, 35), Scale(size, 12)),
            new(Scale(size, 19), Scale(size, 35)),
            new(Scale(size, 31), Scale(size, 35)),
            new(Scale(size, 26), Scale(size, 52)),
            new(Scale(size, 46), Scale(size, 27)),
            new(Scale(size, 34), Scale(size, 27)),
        ];

        using var shadowBrush = new SolidBrush(Color.FromArgb(80, 0, 31, 57));
        using var shadowPath = new GraphicsPath();
        shadowPath.AddPolygon(bolt
            .Select(point => new PointF(point.X + Scale(size, 1.5f), point.Y + Scale(size, 2f)))
            .ToArray());
        graphics.FillPath(shadowBrush, shadowPath);

        using var boltBrush = new LinearGradientBrush(
            new Rectangle(Scale(size, 17), Scale(size, 10), Scale(size, 32), Scale(size, 44)),
            Color.White,
            Color.FromArgb(255, 221, 72),
            LinearGradientMode.Vertical);
        using var boltPath = new GraphicsPath();
        boltPath.AddPolygon(bolt);
        graphics.FillPath(boltBrush, boltPath);
    }

    private static void DrawGaugeArc(Graphics graphics, int size)
    {
        var arcBounds = new Rectangle(Scale(size, 15), Scale(size, 16), Scale(size, 34), Scale(size, 34));
        using var arcPen = new Pen(Color.FromArgb(210, Color.White), Scale(size, 3))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };

        graphics.DrawArc(arcPen, arcBounds, 205, 130);
    }

    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static int Scale(int size, float value) => (int)MathF.Round(size * value / 64f);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(nint hIcon);
}
