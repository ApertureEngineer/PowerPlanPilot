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
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(420, 140);

        var promptLabel = new Label
        {
            AutoSize = false,
            Location = new Point(12, 12),
            Size = new Size(396, 42),
            Text = prompt,
        };

        _input.DecimalPlaces = decimalPlaces;
        _input.Increment = increment;
        _input.Location = new Point(15, 60);
        _input.Maximum = maximum;
        _input.Minimum = minimum;
        _input.Size = new Size(180, 27);
        _input.ThousandsSeparator = false;
        _input.Value = Math.Clamp(currentValue, minimum, maximum);

        var okButton = new Button
        {
            DialogResult = DialogResult.OK,
            Location = new Point(240, 100),
            Size = new Size(80, 28),
            Text = "OK",
        };

        var cancelButton = new Button
        {
            DialogResult = DialogResult.Cancel,
            Location = new Point(328, 100),
            Size = new Size(80, 28),
            Text = "Cancel",
        };

        AcceptButton = okButton;
        CancelButton = cancelButton;
        Controls.AddRange([promptLabel, _input, okButton, cancelButton]);
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
