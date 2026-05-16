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

        DrawBackground(graphics, size);
        DrawMeter(graphics, size);
        DrawBolt(graphics, size);
        DrawPilotDot(graphics, size);

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

    private static void DrawBackground(Graphics graphics, int size)
    {
        var inset = Scale(size, 5);
        var bounds = new Rectangle(inset, inset, size - (inset * 2), size - (inset * 2));

        using var backgroundBrush = new LinearGradientBrush(
            bounds,
            Color.FromArgb(16, 94, 210),
            Color.FromArgb(0, 214, 170),
            LinearGradientMode.ForwardDiagonal);

        using var backgroundPath = RoundedRectangle(bounds, Scale(size, 15));
        graphics.FillPath(backgroundBrush, backgroundPath);

        using var glowBrush = new SolidBrush(Color.FromArgb(70, Color.White));
        graphics.FillEllipse(glowBrush, new Rectangle(Scale(size, 10), Scale(size, 8), Scale(size, 30), Scale(size, 18)));

        using var borderPen = new Pen(Color.FromArgb(150, Color.White), Math.Max(1, Scale(size, 1.6f)));
        graphics.DrawPath(borderPen, backgroundPath);
    }

    private static void DrawMeter(Graphics graphics, int size)
    {
        var arcBounds = new Rectangle(Scale(size, 13), Scale(size, 15), Scale(size, 38), Scale(size, 38));
        using var shadowPen = new Pen(Color.FromArgb(80, 0, 38, 70), Scale(size, 5))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };
        using var arcPen = new Pen(Color.FromArgb(225, Color.White), Scale(size, 3.5f))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };

        graphics.DrawArc(shadowPen, arcBounds, 205, 130);
        graphics.DrawArc(arcPen, arcBounds, 205, 130);
    }

    private static void DrawBolt(Graphics graphics, int size)
    {
        PointF[] bolt =
        [
            new(Scale(size, 36), Scale(size, 12)),
            new(Scale(size, 18), Scale(size, 36)),
            new(Scale(size, 31), Scale(size, 36)),
            new(Scale(size, 25), Scale(size, 53)),
            new(Scale(size, 47), Scale(size, 26)),
            new(Scale(size, 34), Scale(size, 27)),
        ];

        using var shadowBrush = new SolidBrush(Color.FromArgb(95, 0, 31, 57));
        using var shadowPath = new GraphicsPath();
        shadowPath.AddPolygon(bolt
            .Select(point => new PointF(point.X + Scale(size, 1.4f), point.Y + Scale(size, 2f)))
            .ToArray());
        graphics.FillPath(shadowBrush, shadowPath);

        using var boltBrush = new LinearGradientBrush(
            new Rectangle(Scale(size, 17), Scale(size, 10), Scale(size, 32), Scale(size, 44)),
            Color.White,
            Color.FromArgb(255, 216, 64),
            LinearGradientMode.Vertical);
        using var boltPath = new GraphicsPath();
        boltPath.AddPolygon(bolt);
        graphics.FillPath(boltBrush, boltPath);

        using var edgePen = new Pen(Color.FromArgb(180, 255, 255, 255), Math.Max(1, Scale(size, 1)));
        graphics.DrawPath(edgePen, boltPath);
    }

    private static void DrawPilotDot(Graphics graphics, int size)
    {
        using var dotBrush = new SolidBrush(Color.FromArgb(255, 31, 43, 55));
        using var dotHighlightBrush = new SolidBrush(Color.FromArgb(245, 255, 255, 255));
        graphics.FillEllipse(dotBrush, new Rectangle(Scale(size, 45), Scale(size, 44), Scale(size, 8), Scale(size, 8)));
        graphics.FillEllipse(dotHighlightBrush, new Rectangle(Scale(size, 47), Scale(size, 46), Scale(size, 3), Scale(size, 3)));
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
