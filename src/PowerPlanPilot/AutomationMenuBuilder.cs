namespace PowerPlanPilot;

internal sealed class AutomationMenuBuilder
{
    private readonly ContextMenuStrip _menu;
    private readonly AutomationController _automationController;
    private readonly IProcessNameProvider _processNameProvider;
    private readonly Action<string> _addHeaderItem;
    private readonly Action<string> _addDisabledItem;
    private readonly CreateMenuItemDelegate _createMenuItem;
    private readonly CreateCheckedMenuItemDelegate _createCheckedMenuItem;
    private readonly Action<Action<AutomationSettings>> _updateAutomationSetting;
    private readonly PromptForIntegerDelegate _promptForInteger;
    private readonly PromptForDoubleDelegate _promptForDouble;
    private readonly string? _settingsWarning;

    public AutomationMenuBuilder(
        ContextMenuStrip menu,
        AutomationController automationController,
        IProcessNameProvider processNameProvider,
        Action<string> addHeaderItem,
        Action<string> addDisabledItem,
        CreateMenuItemDelegate createMenuItem,
        CreateCheckedMenuItemDelegate createCheckedMenuItem,
        Action<Action<AutomationSettings>> updateAutomationSetting,
        PromptForIntegerDelegate promptForInteger,
        PromptForDoubleDelegate promptForDouble,
        string? settingsWarning)
    {
        _menu = menu;
        _automationController = automationController;
        _processNameProvider = processNameProvider;
        _addHeaderItem = addHeaderItem;
        _addDisabledItem = addDisabledItem;
        _createMenuItem = createMenuItem;
        _createCheckedMenuItem = createCheckedMenuItem;
        _updateAutomationSetting = updateAutomationSetting;
        _promptForInteger = promptForInteger;
        _promptForDouble = promptForDouble;
        _settingsWarning = settingsWarning;
    }

    public delegate ToolStripMenuItem CreateMenuItemDelegate(
        string text,
        EventHandler? onClick = null,
        object? tag = null,
        bool enabled = true);

    public delegate ToolStripMenuItem CreateCheckedMenuItemDelegate(
        string text,
        bool isChecked,
        EventHandler? onClick = null,
        object? tag = null);

    public delegate void PromptForIntegerDelegate(
        string title,
        string prompt,
        int currentValue,
        int minimum,
        int maximum,
        Action<int> apply);

    public delegate void PromptForDoubleDelegate(
        string title,
        string prompt,
        double currentValue,
        double minimum,
        double maximum,
        Action<double> apply);

    public void AddAutomationItems(IReadOnlyList<PowerPlan> plans)
    {
        _addHeaderItem("Automation");

        var settings = _automationController.Settings;
        _menu.Items.Add(_createCheckedMenuItem(
            "Enable automation",
            settings.IsEnabled,
            (_, _) => _updateAutomationSetting(s => s.IsEnabled = !s.IsEnabled)));

        _addDisabledItem(_automationController.StatusText);
        if (!string.IsNullOrWhiteSpace(_settingsWarning))
        {
            _addDisabledItem(_settingsWarning);
        }

        AddIdleItems(plans, settings);
        AddProcessItems(plans, settings);
        AddPowerSourceMenus(plans, settings);
    }

    private void AddIdleItems(IReadOnlyList<PowerPlan> plans, AutomationSettings settings)
    {
        var idleMenu = _createMenuItem("Idle time settings");
        idleMenu.DropDownItems.Add(_createCheckedMenuItem(
            "Use idle time",
            settings.Mode == AutomationMode.IdleTime,
            (_, _) => _updateAutomationSetting(s => s.Mode = AutomationMode.IdleTime)));
        idleMenu.DropDownItems.Add(new ToolStripSeparator());
        idleMenu.DropDownItems.Add(CreatePlanMenu(
            $"Scale-down plan: {GetPlanName(plans, settings.TargetPowerPlanId)}",
            plans,
            settings.TargetPowerPlanId,
            (s, planId) => s.TargetPowerPlanId = planId));
        idleMenu.DropDownItems.Add(_createMenuItem(
            $"Idle threshold: {settings.IdleMinutes} minutes",
            (_, _) => _promptForInteger(
                "Idle threshold",
                "Switch to the scale-down plan after this many idle minutes:",
                settings.IdleMinutes,
                1,
                1440,
                value => settings.IdleMinutes = value)));
        _menu.Items.Add(idleMenu);
    }

    private void AddPowerSourceMenus(IReadOnlyList<PowerPlan> plans, AutomationSettings settings)
    {
        var sourceMenu = _createMenuItem("Power source settings");
        sourceMenu.DropDownItems.Add(CreatePowerSourceMenu(
            $"On AC: {GetPlanName(plans, settings.AcPowerPlanId)}",
            "Switch automatically on AC",
            settings.SwitchOnAcPower,
            settings.AcPowerPlanId,
            plans,
            s => s.SwitchOnAcPower = !s.SwitchOnAcPower,
            (s, planId) => s.AcPowerPlanId = planId));

        sourceMenu.DropDownItems.Add(CreatePowerSourceMenu(
            $"On battery: {GetPlanName(plans, settings.BatteryPowerPlanId)}",
            "Switch automatically on battery",
            settings.SwitchOnBattery,
            settings.BatteryPowerPlanId,
            plans,
            s => s.SwitchOnBattery = !s.SwitchOnBattery,
            (s, planId) => s.BatteryPowerPlanId = planId));

        _menu.Items.Add(sourceMenu);
    }

    private ToolStripMenuItem CreatePowerSourceMenu(
        string text,
        string toggleText,
        bool isEnabled,
        Guid? selectedPlanId,
        IReadOnlyList<PowerPlan> plans,
        Action<AutomationSettings> toggle,
        Action<AutomationSettings, Guid?> setPlanId)
    {
        var menu = _createMenuItem(text);
        menu.DropDownItems.Add(_createCheckedMenuItem(
            toggleText,
            isEnabled,
            (_, _) => _updateAutomationSetting(toggle)));
        menu.DropDownItems.Add(new ToolStripSeparator());

        if (plans.Count == 0)
        {
            menu.DropDownItems.Add(_createMenuItem("No plans available", enabled: false));
            return menu;
        }

        menu.DropDownItems.Add(_createCheckedMenuItem(
            "No plan selected",
            selectedPlanId is null,
            (_, _) => _updateAutomationSetting(s => setPlanId(s, null))));

        foreach (var plan in plans)
        {
            menu.DropDownItems.Add(_createCheckedMenuItem(
                plan.Name,
                selectedPlanId == plan.Id,
                (_, _) => _updateAutomationSetting(s => setPlanId(s, plan.Id))));
        }

        return menu;
    }

    private void AddProcessItems(IReadOnlyList<PowerPlan> plans, AutomationSettings settings)
    {
        var processSettingsMenu = _createMenuItem("Process CPU settings");
        processSettingsMenu.DropDownItems.Add(_createCheckedMenuItem(
            "Use process CPU",
            settings.Mode == AutomationMode.ProcessCpu,
            (_, _) => _updateAutomationSetting(s => s.Mode = AutomationMode.ProcessCpu)));
        processSettingsMenu.DropDownItems.Add(new ToolStripSeparator());

        var processMenu = _createMenuItem($"Process: {settings.ProcessName ?? "not selected"}");

        var processNames = _processNameProvider.GetOpenProcessNames();
        if (processNames.Count == 0)
        {
            processMenu.DropDownItems.Add(_createMenuItem("No processes available", enabled: false));
        }
        else
        {
            foreach (var processName in processNames)
            {
                processMenu.DropDownItems.Add(_createCheckedMenuItem(
                    processName,
                    string.Equals(settings.ProcessName, processName, StringComparison.OrdinalIgnoreCase),
                    (_, _) => _updateAutomationSetting(s => s.ProcessName = processName)));
            }
        }

        processSettingsMenu.DropDownItems.Add(processMenu);
        processSettingsMenu.DropDownItems.Add(new ToolStripSeparator());

        processSettingsMenu.DropDownItems.Add(CreatePlanMenu(
            $"Scale-down plan: {GetPlanName(plans, settings.TargetPowerPlanId)}",
            plans,
            settings.TargetPowerPlanId,
            (s, planId) => s.TargetPowerPlanId = planId));

        processSettingsMenu.DropDownItems.Add(_createMenuItem(
            $"Low CPU threshold: {settings.ProcessCpuThresholdPercent:F1}%",
            (_, _) => _promptForDouble(
                "Low CPU threshold",
                "Switch when the selected process stays under this CPU percentage:",
                settings.ProcessCpuThresholdPercent,
                0,
                100,
                value => settings.ProcessCpuThresholdPercent = value)));

        processSettingsMenu.DropDownItems.Add(_createMenuItem(
            $"Low-usage duration: {settings.ProcessLowUsageMinutes} minutes",
            (_, _) => _promptForInteger(
                "Low-usage duration",
                "Switch after the selected process stays below the CPU threshold for this many minutes:",
                settings.ProcessLowUsageMinutes,
                1,
                1440,
                value => settings.ProcessLowUsageMinutes = value)));

        processSettingsMenu.DropDownItems.Add(new ToolStripSeparator());
        processSettingsMenu.DropDownItems.Add(CreatePlanMenu(
            $"Scale-up plan: {GetPlanName(plans, settings.ScaleUpPowerPlanId)}",
            plans,
            settings.ScaleUpPowerPlanId,
            (s, planId) => s.ScaleUpPowerPlanId = planId));

        processSettingsMenu.DropDownItems.Add(_createMenuItem(
            $"High CPU threshold: {settings.ProcessHighCpuThresholdPercent:F1}%",
            (_, _) => _promptForDouble(
                "High CPU threshold",
                "Switch to the scale-up plan after the selected process stays over this CPU percentage:",
                settings.ProcessHighCpuThresholdPercent,
                0,
                100,
                value => settings.ProcessHighCpuThresholdPercent = value)));

        processSettingsMenu.DropDownItems.Add(_createMenuItem(
            $"High-usage duration: {settings.ProcessHighUsageMinutes} minutes",
            (_, _) => _promptForInteger(
                "High-usage duration",
                "Switch to the scale-up plan after this many high-usage minutes:",
                settings.ProcessHighUsageMinutes,
                1,
                1440,
                value => settings.ProcessHighUsageMinutes = value)));

        _menu.Items.Add(processSettingsMenu);
    }

    private ToolStripMenuItem CreatePlanMenu(
        string text,
        IReadOnlyList<PowerPlan> plans,
        Guid? selectedPlanId,
        Action<AutomationSettings, Guid?> setPlanId)
    {
        var menu = _createMenuItem(text);

        if (plans.Count == 0)
        {
            menu.DropDownItems.Add(_createMenuItem("No plans available", enabled: false));
            return menu;
        }

        menu.DropDownItems.Add(_createCheckedMenuItem(
            "No plan selected",
            selectedPlanId is null,
            (_, _) => _updateAutomationSetting(s => setPlanId(s, null))));

        foreach (var plan in plans)
        {
            menu.DropDownItems.Add(_createCheckedMenuItem(
                plan.Name,
                selectedPlanId == plan.Id,
                (_, _) => _updateAutomationSetting(s => setPlanId(s, plan.Id))));
        }

        return menu;
    }

    private static string GetPlanName(IReadOnlyList<PowerPlan> plans, Guid? planId)
    {
        if (planId is null)
        {
            return "not selected";
        }

        return plans.FirstOrDefault(plan => plan.Id == planId)?.Name ?? "missing plan";
    }
}
