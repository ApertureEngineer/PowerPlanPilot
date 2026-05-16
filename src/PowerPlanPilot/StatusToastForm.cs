namespace PowerPlanPilot;

internal sealed class StatusToastForm : Form
{
    private static readonly Color Border = Color.FromArgb(68, 76, 89);
    private static readonly Color Surface = Color.FromArgb(31, 35, 42);
    private static readonly Color PrimaryText = Color.White;
    private static readonly Color SecondaryText = Color.FromArgb(226, 232, 240);

    private readonly System.Windows.Forms.Timer _closeTimer = new()
    {
        Interval = 2200,
    };

    private readonly Image _iconImage;

    public StatusToastForm(Icon icon, string message)
    {
        _iconImage = icon.ToBitmap();

        AutoScaleMode = AutoScaleMode.Dpi;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = Surface;
        Font = SystemFonts.MessageBoxFont;
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(420, 118);
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;

        Controls.Add(CreateContent(message));

        _closeTimer.Tick += OnCloseTimerTick;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_NOACTIVATE = 0x08000000;
            var createParams = base.CreateParams;
            createParams.ExStyle |= WS_EX_NOACTIVATE;
            return createParams;
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        PositionNearTaskbar();
        _closeTimer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var bounds = ClientRectangle;
        bounds.Width -= 1;
        bounds.Height -= 1;

        using var pen = new Pen(Border);
        e.Graphics.DrawRectangle(pen, bounds);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _closeTimer.Tick -= OnCloseTimerTick;
            _closeTimer.Dispose();
            _iconImage.Dispose();
        }

        base.Dispose(disposing);
    }

    private Control CreateContent(string message)
    {
        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Dock = DockStyle.Top,
            MinimumSize = new Size(420, 118),
            Padding = new Padding(18, 18, 18, 18),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 66));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        layout.Controls.Add(new PictureBox
        {
            Dock = DockStyle.Fill,
            Image = _iconImage,
            Margin = new Padding(0, 0, 18, 0),
            SizeMode = PictureBoxSizeMode.Zoom,
        }, 0, 0);

        var textLayout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Dock = DockStyle.Top,
            RowCount = 2,
        };
        textLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        textLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        textLayout.Controls.Add(new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = PrimaryText,
            Margin = new Padding(0, 0, 0, 4),
            Text = "PowerPlanPilot",
            TextAlign = ContentAlignment.BottomLeft,
        }, 0, 0);

        textLayout.Controls.Add(new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = SecondaryText,
            MaximumSize = new Size(320, 0),
            Text = message,
            TextAlign = ContentAlignment.TopLeft,
        }, 0, 1);

        layout.Controls.Add(textLayout, 1, 0);
        return layout;
    }

    private void PositionNearTaskbar()
    {
        var workingArea = Screen.FromPoint(Cursor.Position).WorkingArea;
        var left = Math.Max(workingArea.Left + 18, workingArea.Right - Width - 18);
        var top = Math.Max(workingArea.Top + 18, workingArea.Bottom - Height - 18);
        Location = new Point(
            left,
            top);
    }

    private void OnCloseTimerTick(object? sender, EventArgs e)
    {
        _closeTimer.Stop();
        Close();
    }
}
