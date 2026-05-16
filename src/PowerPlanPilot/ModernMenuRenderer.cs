using System.Drawing.Drawing2D;

namespace PowerPlanPilot;

internal sealed class ModernMenuRenderer : ToolStripProfessionalRenderer
{
    private static readonly Color Accent = Color.FromArgb(24, 119, 242);
    private static readonly Color Border = Color.FromArgb(218, 224, 232);
    private static readonly Color CheckFill = Color.FromArgb(219, 239, 255);
    private static readonly Color DisabledText = Color.FromArgb(142, 150, 160);
    private static readonly Color HoverFill = Color.FromArgb(238, 245, 255);
    private static readonly Color ImageMarginFill = Color.FromArgb(248, 250, 252);
    private static readonly Color MenuBack = Color.FromArgb(255, 255, 255);
    private static readonly Color Separator = Color.FromArgb(232, 236, 241);
    private static readonly Color Text = Color.FromArgb(28, 35, 45);

    public ModernMenuRenderer()
        : base(new ModernColorTable())
    {
        RoundedEdges = true;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        e.Graphics.Clear(MenuBack);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(Border);
        var bounds = new Rectangle(Point.Empty, e.ToolStrip.Size);
        bounds.Width -= 1;
        bounds.Height -= 1;
        e.Graphics.DrawRectangle(pen, bounds);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected || !e.Item.Enabled)
        {
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(4, 2, e.Item.Width - 8, e.Item.Height - 4);
        using var path = RoundedRectangle(bounds, 6);
        using var brush = new SolidBrush(HoverFill);
        e.Graphics.FillPath(brush, path);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(ImageMarginFill);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);

        using var pen = new Pen(Separator);
        var x = e.AffectedBounds.Right - 1;
        e.Graphics.DrawLine(pen, x, e.AffectedBounds.Top + 8, x, e.AffectedBounds.Bottom - 8);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        using var pen = new Pen(Separator);
        var y = e.Item.Height / 2;
        e.Graphics.DrawLine(pen, 36, y, e.Item.Width - 8, y);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? Text : DisabledText;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        const int checkSize = 20;
        var checkLeft = Math.Max(3, e.ImageRectangle.Left + ((e.ImageRectangle.Width - checkSize) / 2));
        var checkTop = Math.Max(2, e.ImageRectangle.Top + ((e.ImageRectangle.Height - checkSize) / 2));
        var bounds = new Rectangle(checkLeft, checkTop, checkSize, checkSize);
        using var fill = new SolidBrush(CheckFill);
        using var path = RoundedRectangle(bounds, 6);
        e.Graphics.FillPath(fill, path);

        using var pen = new Pen(Accent, 2.4F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };

        var left = bounds.Left + 5;
        var middle = bounds.Left + 9;
        var right = bounds.Left + 15;
        var top = bounds.Top + 10;
        var bottom = bounds.Top + 14;
        var checkTop = bounds.Top + 6;
        e.Graphics.DrawLines(
            pen,
            [new Point(left, top), new Point(middle, bottom), new Point(right, checkTop)]);
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

    private sealed class ModernColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => MenuBack;

        public override Color ImageMarginGradientBegin => ImageMarginFill;

        public override Color ImageMarginGradientMiddle => ImageMarginFill;

        public override Color ImageMarginGradientEnd => ImageMarginFill;

        public override Color MenuBorder => Border;

        public override Color MenuItemBorder => HoverFill;

        public override Color MenuItemSelected => HoverFill;

        public override Color MenuItemSelectedGradientBegin => HoverFill;

        public override Color MenuItemSelectedGradientEnd => HoverFill;
    }
}
