using System.Diagnostics;

namespace PowerPlanPilot;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private static readonly Padding MenuItemPadding = new(2, 3, 8, 3);
    private static readonly Padding HeaderPadding = new(2, 4, 8, 4);

    private readonly AutomationController _automationController;
    private readonly AutomationMenuBuilder _automationMenuBuilder;
    private readonly AutomationSettingsStore _settingsStore = new();
    private readonly PowerPlanService _powerPlanService;
    private readonly IProcessNameProvider _processNameProvider;
    private readonly ContextMenuStrip _menu = new();
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _trayIcon;
    private readonly Font _headerFont;
    private readonly string? _settingsWarning;

    public TrayApplicationContext(PowerPlanService powerPlanService)
        : this(powerPlanService, new SystemProcessService())
    {
    }

    private TrayApplicationContext(PowerPlanService powerPlanService, IProcessNameProvider processNameProvider)
    {
        _powerPlanService = powerPlanService;
        _processNameProvider = processNameProvider;

        var loadResult = _settingsStore.Load();
        _settingsWarning = loadResult.WarningMessage;
        _automationController = new AutomationController(_powerPlanService, loadResult.Settings);
        _automationController.StatusChanged += OnAutomationStatusChanged;
        _trayIcon = TrayIconFactory.CreateIcon();

        ConfigureMenu();
        _headerFont = new Font(_menu.Font, FontStyle.Bold);
        _automationMenuBuilder = new AutomationMenuBuilder(
            _menu,
            _automationController,
            _processNameProvider,
            AddHeaderItem,
            AddDisabledItem,
            CreateMenuItem,
            CreateCheckedMenuItem,
            UpdateAutomationSetting,
            PromptForInteger,
            PromptForDouble,
            _settingsWarning);
        _menu.Opening += (_, _) => RebuildMenu();

        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = _menu,
            Icon = _trayIcon,
            Text = "PowerPlanPilot",
            Visible = true,
        };

        _notifyIcon.MouseUp += OnTrayMouseUp;
        RebuildMenu();
        _automationController.Start();

        if (!string.IsNullOrWhiteSpace(_settingsWarning))
        {
            ShowStatus(_settingsWarning);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notifyIcon.MouseUp -= OnTrayMouseUp;
            _automationController.StatusChanged -= OnAutomationStatusChanged;
            _automationController.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _trayIcon.Dispose();
            _menu.Dispose();
            _headerFont.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnTrayMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        RebuildMenu();
        _menu.Show(Cursor.Position);
    }

    private void RebuildMenu()
    {
        ClearMenuItems();

        AddHeaderItem("Power plans");

        IReadOnlyList<PowerPlan> plans;
        try
        {
            plans = _powerPlanService.GetPowerPlans();
        }
        catch (Exception ex)
        {
            AddDisabledItem("Could not load Windows power plans");
            AddDisabledItem(ex.Message);
            AddSeparator();
            _automationMenuBuilder.AddAutomationItems([]);
            AddSeparator();
            AddUtilityItems();
            return;
        }

        if (plans.Count == 0)
        {
            AddDisabledItem("No Windows power plans found");
        }
        else
        {
            foreach (var plan in plans)
            {
                _menu.Items.Add(CreateCheckedMenuItem(plan.Name, plan.IsActive, OnPowerPlanClick, plan));
            }
        }

        AddSeparator();
        _automationMenuBuilder.AddAutomationItems(plans);
        AddSeparator();
        AddUtilityItems();
    }

    private void OnPowerPlanClick(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem { Tag: PowerPlan plan })
        {
            return;
        }

        try
        {
            _powerPlanService.ActivatePowerPlan(plan.Id);
            ShowStatus($"Switched to {plan.Name}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "PowerPlanPilot",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            RebuildMenu();
        }
    }

    private void AddUtilityItems()
    {
        AddHeaderItem("Tools");
        AddPaddedItem(_menu.Items, CreateMenuItem("Refresh", (_, _) => RebuildMenu()));
        AddPaddedItem(_menu.Items, CreateMenuItem("Windows power options", (_, _) => OpenWindowsPowerOptions()));
        AddPaddedItem(_menu.Items, CreateCheckedMenuItem("Start with Windows", AutostartManager.IsEnabled(), (_, _) => ToggleAutostart()));
        AddPaddedItem(_menu.Items, CreateMenuItem("Info", (_, _) => ShowInfo()));
        AddPaddedItem(_menu.Items, CreateMenuItem("Exit", (_, _) => ExitThread()));
    }

    private void OpenWindowsPowerOptions()
    {
        try
        {
            StartShellProcess("control.exe", "powercfg.cpl");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "PowerPlanPilot",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ToggleAutostart()
    {
        try
        {
            AutostartManager.SetEnabled(!AutostartManager.IsEnabled());
            ShowStatus(AutostartManager.IsEnabled() ? "Autostart enabled" : "Autostart disabled");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "PowerPlanPilot",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            RebuildMenu();
        }
    }

    private void ShowInfo()
    {
        using var aboutForm = new AboutForm();
        aboutForm.ShowDialog();
    }

    private static void StartShellProcess(string fileName, string arguments)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = true,
        });
    }

    private void AddHeaderItem(string text)
    {
        var header = new ToolStripLabel(text)
        {
            Enabled = false,
            Font = _headerFont,
            ForeColor = Color.FromArgb(83, 95, 110),
            Padding = HeaderPadding,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        _menu.Items.Add(header);
        AddSeparator();
    }

    private void AddDisabledItem(string text)
    {
        AddPaddedItem(_menu.Items, CreateMenuItem(text, enabled: false));
    }

    private static ToolStripMenuItem CreateMenuItem(string text, EventHandler? onClick = null, object? tag = null, bool enabled = true)
    {
        var item = new ToolStripMenuItem(text)
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            Enabled = enabled,
            Padding = MenuItemPadding,
            Tag = tag,
        };

        if (onClick is not null)
        {
            item.Click += onClick;
        }

        return item;
    }

    private static ToolStripMenuItem CreateCheckedMenuItem(string text, bool isChecked, EventHandler? onClick = null, object? tag = null)
    {
        var item = CreateMenuItem(text, onClick, tag);
        item.Checked = isChecked;
        item.CheckOnClick = false;
        return item;
    }

    private static void AddPaddedItem(ToolStripItemCollection items, ToolStripMenuItem item)
    {
        item.Padding = MenuItemPadding;
        items.Add(item);
    }

    private void AddSeparator() => _menu.Items.Add(new ToolStripSeparator());

    private void ClearMenuItems()
    {
        while (_menu.Items.Count > 0)
        {
            var item = _menu.Items[0];
            _menu.Items.RemoveAt(0);
            item.Dispose();
        }
    }

    private void ConfigureMenu()
    {
        _menu.BackColor = Color.White;
        _menu.ForeColor = Color.FromArgb(28, 35, 45);
        _menu.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        _menu.Padding = new Padding(2, 8, 2, 8);
        _menu.Renderer = new ModernMenuRenderer();
        _menu.ShowCheckMargin = true;
        _menu.ShowImageMargin = false;
    }

    private void UpdateAutomationSetting(Action<AutomationSettings> update)
    {
        update(_automationController.Settings);
        _settingsStore.Save(_automationController.Settings);
        _automationController.ApplySettings();
        RebuildMenu();
    }

    private void PromptForInteger(string title, string prompt, int currentValue, int minimum, int maximum, Action<int> apply)
    {
        if (NumberInputDialog.TryGetInteger(title, prompt, currentValue, minimum, maximum, out var value))
        {
            UpdateAutomationSetting(_ => apply(value));
        }
    }

    private void PromptForDouble(string title, string prompt, double currentValue, double minimum, double maximum, Action<double> apply)
    {
        if (NumberInputDialog.TryGetDouble(title, prompt, currentValue, minimum, maximum, out var value))
        {
            UpdateAutomationSetting(_ => apply(value));
        }
    }

    private void OnAutomationStatusChanged(object? sender, string status)
    {
        _notifyIcon.Text = status.Length > 63 ? status[..63] : status;

        if (status.StartsWith("Switched to ", StringComparison.Ordinal))
        {
            ShowStatus(status);
            RebuildMenu();
        }
    }

    private void ShowStatus(string message)
    {
        _notifyIcon.BalloonTipTitle = "PowerPlanPilot";
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.ShowBalloonTip(1200);
    }
}
