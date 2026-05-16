using System.Diagnostics;
using Microsoft.VisualBasic;

namespace PowerPlanPilot;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly AutomationController _automationController;
    private readonly AutomationSettingsStore _settingsStore = new();
    private readonly PowerPlanService _powerPlanService;
    private readonly ContextMenuStrip _menu = new();
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _trayIcon;
    private readonly Font _headerFont;

    public TrayApplicationContext(PowerPlanService powerPlanService)
    {
        _powerPlanService = powerPlanService;
        _automationController = new AutomationController(_powerPlanService, _settingsStore.Load());
        _automationController.StatusChanged += OnAutomationStatusChanged;
        _trayIcon = TrayIconFactory.CreateIcon();

        ConfigureMenu();
        _headerFont = new Font(_menu.Font, FontStyle.Bold);
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
            _menu.Items.Add(new ToolStripSeparator());
            AddAutomationItems([]);
            _menu.Items.Add(new ToolStripSeparator());
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
                var item = new ToolStripMenuItem(plan.Name)
                {
                    Checked = plan.IsActive,
                    CheckOnClick = false,
                    DisplayStyle = ToolStripItemDisplayStyle.Text,
                    Padding = new Padding(2, 3, 8, 3),
                    Tag = plan,
                };

                item.Click += OnPowerPlanClick;
                _menu.Items.Add(item);
            }
        }

        _menu.Items.Add(new ToolStripSeparator());
        AddAutomationItems(plans);
        _menu.Items.Add(new ToolStripSeparator());
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

    private void AddAutomationItems(IReadOnlyList<PowerPlan> plans)
    {
        AddHeaderItem("Automation");

        var settings = _automationController.Settings;
        var enabledItem = new ToolStripMenuItem("Enable automation", null, (_, _) => UpdateAutomationSetting(s => s.IsEnabled = !s.IsEnabled))
        {
            Checked = settings.IsEnabled,
            Padding = new Padding(2, 3, 8, 3),
        };
        _menu.Items.Add(enabledItem);

        AddDisabledItem(_automationController.StatusText);

        var targetMenu = new ToolStripMenuItem("Scale-down target plan")
        {
            Padding = new Padding(2, 3, 8, 3),
        };

        if (plans.Count == 0)
        {
            targetMenu.DropDownItems.Add(new ToolStripMenuItem("No plans available") { Enabled = false });
        }
        else
        {
            foreach (var plan in plans)
            {
                var item = new ToolStripMenuItem(plan.Name, null, (_, _) => UpdateAutomationSetting(s => s.TargetPowerPlanId = plan.Id))
                {
                    Checked = settings.TargetPowerPlanId == plan.Id,
                };
                targetMenu.DropDownItems.Add(item);
            }
        }
        _menu.Items.Add(targetMenu);

        var modeMenu = new ToolStripMenuItem("Switch condition")
        {
            Padding = new Padding(2, 3, 8, 3),
        };
        modeMenu.DropDownItems.Add(new ToolStripMenuItem("Idle time", null, (_, _) => UpdateAutomationSetting(s => s.Mode = AutomationMode.IdleTime))
        {
            Checked = settings.Mode == AutomationMode.IdleTime,
        });
        modeMenu.DropDownItems.Add(new ToolStripMenuItem("Process CPU usage", null, (_, _) => UpdateAutomationSetting(s => s.Mode = AutomationMode.ProcessCpu))
        {
            Checked = settings.Mode == AutomationMode.ProcessCpu,
        });
        _menu.Items.Add(modeMenu);

        var idleItem = new ToolStripMenuItem($"Idle threshold: {settings.IdleMinutes} minutes", null, (_, _) => PromptForInteger(
            "Idle threshold",
            "Switch to the scale-down plan after this many idle minutes:",
            settings.IdleMinutes,
            1,
            1440,
            value => settings.IdleMinutes = value))
        {
            Padding = new Padding(2, 3, 8, 3),
        };
        _menu.Items.Add(idleItem);

        AddProcessItems(settings);
    }

    private void AddProcessItems(AutomationSettings settings)
    {
        var processMenu = new ToolStripMenuItem($"Process: {settings.ProcessName ?? "not selected"}")
        {
            Padding = new Padding(2, 3, 8, 3),
        };

        var processNames = GetOpenProcessNames();
        if (processNames.Count == 0)
        {
            processMenu.DropDownItems.Add(new ToolStripMenuItem("No processes available") { Enabled = false });
        }
        else
        {
            foreach (var processName in processNames)
            {
                processMenu.DropDownItems.Add(new ToolStripMenuItem(processName, null, (_, _) => UpdateAutomationSetting(s => s.ProcessName = processName))
                {
                    Checked = string.Equals(settings.ProcessName, processName, StringComparison.OrdinalIgnoreCase),
                });
            }
        }

        _menu.Items.Add(processMenu);

        _menu.Items.Add(new ToolStripMenuItem($"CPU threshold: {settings.ProcessCpuThresholdPercent:F1}%", null, (_, _) => PromptForDouble(
            "Process CPU threshold",
            "Switch when the selected process stays under this CPU percentage:",
            settings.ProcessCpuThresholdPercent,
            0,
            100,
            value => settings.ProcessCpuThresholdPercent = value))
        {
            Padding = new Padding(2, 3, 8, 3),
        });

        _menu.Items.Add(new ToolStripMenuItem($"Low-usage duration: {settings.ProcessLowUsageMinutes} minutes", null, (_, _) => PromptForInteger(
            "Low-usage duration",
            "Switch after the selected process stays below the CPU threshold for this many minutes:",
            settings.ProcessLowUsageMinutes,
            1,
            1440,
            value => settings.ProcessLowUsageMinutes = value))
        {
            Padding = new Padding(2, 3, 8, 3),
        });
    }

    private void AddUtilityItems()
    {
        AddHeaderItem("Tools");

        var refreshItem = new ToolStripMenuItem("Refresh", null, (_, _) => RebuildMenu())
        {
            Padding = new Padding(2, 3, 8, 3),
        };
        _menu.Items.Add(refreshItem);

        var powerOptionsItem = new ToolStripMenuItem("Windows power options", null, (_, _) => OpenWindowsPowerOptions())
        {
            Padding = new Padding(2, 3, 8, 3),
        };
        _menu.Items.Add(powerOptionsItem);

        var autostartItem = new ToolStripMenuItem("Start with Windows", null, (_, _) => ToggleAutostart())
        {
            Checked = AutostartManager.IsEnabled(),
            Padding = new Padding(2, 3, 8, 3),
        };
        _menu.Items.Add(autostartItem);

        var infoItem = new ToolStripMenuItem("Info", null, (_, _) => ShowInfo())
        {
            Padding = new Padding(2, 3, 8, 3),
        };
        _menu.Items.Add(infoItem);

        var exitItem = new ToolStripMenuItem("Exit", null, (_, _) => ExitThread())
        {
            Padding = new Padding(2, 3, 8, 3),
        };
        _menu.Items.Add(exitItem);
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
            Padding = new Padding(2, 4, 8, 4),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        _menu.Items.Add(header);
        _menu.Items.Add(new ToolStripSeparator());
    }

    private void AddDisabledItem(string text)
    {
        _menu.Items.Add(new ToolStripMenuItem(text)
        {
            Enabled = false,
            Padding = new Padding(2, 3, 8, 3),
        });
    }

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
        var input = Interaction.InputBox(prompt, title, currentValue.ToString());
        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        if (!int.TryParse(input, out var value) || value < minimum || value > maximum)
        {
            MessageBox.Show($"Enter a whole number from {minimum} to {maximum}.", "PowerPlanPilot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        UpdateAutomationSetting(_ => apply(value));
    }

    private void PromptForDouble(string title, string prompt, double currentValue, double minimum, double maximum, Action<double> apply)
    {
        var input = Interaction.InputBox(prompt, title, currentValue.ToString("F1"));
        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        if (!double.TryParse(input, out var value) || value < minimum || value > maximum)
        {
            MessageBox.Show($"Enter a number from {minimum:F0} to {maximum:F0}.", "PowerPlanPilot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        UpdateAutomationSetting(_ => apply(value));
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

    private static IReadOnlyList<string> GetOpenProcessNames()
    {
        return Process.GetProcesses()
            .Select(process =>
            {
                using (process)
                {
                    try
                    {
                        return process.ProcessName + ".exe";
                    }
                    catch (InvalidOperationException)
                    {
                        return null;
                    }
                }
            })
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray()!;
    }
}
