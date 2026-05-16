namespace PowerPlanPilot;

internal sealed class NumberInputDialog : Form
{
    private readonly NumericUpDown _input = new();

    private NumberInputDialog(
        string title,
        string prompt,
        decimal currentValue,
        decimal minimum,
        decimal maximum,
        int decimalPlaces,
        decimal increment)
    {
        Text = title;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Font = SystemFonts.MessageBoxFont;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;

        var promptLabel = new Label
        {
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12),
            MaximumSize = new Size(420, 0),
            Text = prompt,
        };

        _input.DecimalPlaces = decimalPlaces;
        _input.Increment = increment;
        _input.Anchor = AnchorStyles.Left;
        _input.Margin = new Padding(0, 0, 0, 18);
        _input.Maximum = maximum;
        _input.Minimum = minimum;
        _input.ThousandsSeparator = false;
        _input.Value = Math.Clamp(currentValue, minimum, maximum);
        _input.Width = 180;

        var okButton = new Button
        {
            AutoSize = true,
            DialogResult = DialogResult.OK,
            FlatStyle = FlatStyle.System,
            MinimumSize = new Size(88, 32),
            Text = "OK",
        };

        var cancelButton = new Button
        {
            AutoSize = true,
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.System,
            Margin = new Padding(8, 0, 0, 0),
            MinimumSize = new Size(88, 32),
            Text = "Cancel",
        };

        var buttonPanel = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Right,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = Padding.Empty,
            WrapContents = false,
        };
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(okButton);

        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            MinimumSize = new Size(420, 0),
            Padding = new Padding(14),
            RowCount = 3,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(promptLabel, 0, 0);
        layout.Controls.Add(_input, 0, 1);
        layout.Controls.Add(buttonPanel, 0, 2);

        AcceptButton = okButton;
        CancelButton = cancelButton;
        Controls.Add(layout);
    }

    private decimal Value => _input.Value;

    public static bool TryGetInteger(
        string title,
        string prompt,
        int currentValue,
        int minimum,
        int maximum,
        out int value)
    {
        using var dialog = new NumberInputDialog(title, prompt, currentValue, minimum, maximum, 0, 1);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            value = decimal.ToInt32(dialog.Value);
            return true;
        }

        value = currentValue;
        return false;
    }

    public static bool TryGetDouble(
        string title,
        string prompt,
        double currentValue,
        double minimum,
        double maximum,
        out double value)
    {
        using var dialog = new NumberInputDialog(
            title,
            prompt,
            Convert.ToDecimal(currentValue),
            Convert.ToDecimal(minimum),
            Convert.ToDecimal(maximum),
            1,
            0.1M);

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            value = decimal.ToDouble(dialog.Value);
            return true;
        }

        value = currentValue;
        return false;
    }
}
