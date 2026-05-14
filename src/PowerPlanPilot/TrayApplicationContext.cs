namespace PowerPlanPilot;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly PowerPlanService _powerPlanService;
    private readonly ContextMenuStrip _menu = new();
    private readonly NotifyIcon _notifyIcon;

    public TrayApplicationContext(PowerPlanService powerPlanService)
    {
        _powerPlanService = powerPlanService;
        _menu.Opening += (_, _) => RebuildMenu();

        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = _menu,
            Icon = SystemIcons.Application,
            Text = "PowerPlanPilot",
            Visible = true,
        };

        _notifyIcon.MouseUp += OnTrayMouseUp;
        RebuildMenu();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notifyIcon.MouseUp -= OnTrayMouseUp;
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _menu.Dispose();
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
        _menu.Items.Clear();

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
                    Tag = plan,
                };

                item.Click += OnPowerPlanClick;
                _menu.Items.Add(item);
            }
        }

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

    private void AddUtilityItems()
    {
        var refreshItem = new ToolStripMenuItem("Refresh", null, (_, _) => RebuildMenu());
        _menu.Items.Add(refreshItem);

        var exitItem = new ToolStripMenuItem("Exit", null, (_, _) => ExitThread());
        _menu.Items.Add(exitItem);
    }

    private void AddDisabledItem(string text)
    {
        _menu.Items.Add(new ToolStripMenuItem(text) { Enabled = false });
    }

    private void ShowStatus(string message)
    {
        _notifyIcon.BalloonTipTitle = "PowerPlanPilot";
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.ShowBalloonTip(1200);
    }
}
