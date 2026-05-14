namespace PowerPlanPilot;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly PowerPlanService _powerPlanService;
    private readonly ContextMenuStrip _menu = new();
    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _trayIcon;
    private readonly Font _headerFont;

    public TrayApplicationContext(PowerPlanService powerPlanService)
    {
        _powerPlanService = powerPlanService;
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
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notifyIcon.MouseUp -= OnTrayMouseUp;
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
        var refreshItem = new ToolStripMenuItem("Refresh", null, (_, _) => RebuildMenu())
        {
            Padding = new Padding(2, 3, 8, 3),
        };
        _menu.Items.Add(refreshItem);

        var exitItem = new ToolStripMenuItem("Exit", null, (_, _) => ExitThread())
        {
            Padding = new Padding(2, 3, 8, 3),
        };
        _menu.Items.Add(exitItem);
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

    private void ShowStatus(string message)
    {
        _notifyIcon.BalloonTipTitle = "PowerPlanPilot";
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.ShowBalloonTip(1200);
    }
}
